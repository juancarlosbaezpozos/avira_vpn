using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;
using Avira.Common.Core;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.VpnService;
using Avira.WebAppHost.Properties;
using Avira.Win.Messaging;
using Microsoft.Win32;
using mshtml;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.WebAppHost
{
    public class VpnGuiForm : Form, IService
    {
        private readonly ScriptingObject scriptingObject;

        private readonly SharedStartEvent serviceStardedEvent = new SharedStartEvent();

        private const int KEY_PLUS = 187;

        private const int KEY_MINUS = 189;

        private bool mouseIsDown;

        private Point firstPoint;

        private Point mousePoint;

        private HtmlDocument htmlDocument;

        private IService vpn;

        private ConnectionState suspendedConnectionStatus;

        private HostRouter hostRouter;

        private Router messagRouter;

        private MessageConnector messageConnector;

        private ContextMenuService contextMenuService;

        private bool startMinimized;

        private ReflectionService thisService;

        private string lastOsNotificationId;

        private SystemSettings systemSettings;

        private bool showSettingsView;

        private IContainer components;

        private WebBrowser webBrowserControl;

        private ContextMenuStrip contextMenu;

        public NotifyIcon notifyIcon;

        public ConnectionState CurrentConnectionStatus { get; private set; }

        private bool IsDisconnected => CurrentConnectionStatus == ConnectionState.Disconnected;

        public IService Vpn
        {
            get
            {
                if (vpn == null)
                {
                    ConnectToVpnService();
                }

                return vpn;
            }
        }

        [Routing("settings")]
        public JObject UiSettings
        {
            get { return JObject.Parse(ProductSettings.UiSettings); }
            set { ProductSettings.UiSettings = value.ToString(); }
        }

        [Routing("uiLanguage")] public string UiLanguage => ProductSettings.ProductLanguage;

        public VpnGuiForm(bool startMinimized, bool showSettingsView = false)
        {
            InitializeComponent();
            Serilog.Log.Information("VPN Gui started ...");
            base.ShowInTaskbar = false;
            this.startMinimized = startMinimized;
            this.showSettingsView = showSettingsView;
            string text = FileSystem.MakeFullPath("App\\index.html");
            if (!new PathWhiteList().IsWhiteListed(text))
            {
                throw new Exception("[error] can't load index.html because it is not whitelisted");
            }

            text = "file://" + text;
            text.Replace("\\", "/");
            DiContainer.SetActivator<IAppSettings>(() => new AppSettings());
            DiContainer.SetInstance<ISettings>(new WinSettings(Settings.Default));
            DiContainer.SetInstance<ISettings>(new WinSettings(Settings.Default));
            DiContainer.SetInstance<IProductSettings>(new ProductSettingsBridge());
            systemSettings = new SystemSettings();
            systemSettings.SystemSettingsChanged += delegate(object obj, SystemSettingsData args)
            {
                NotifyOsThemeChanged(args.Theme);
            };
            thisService = new ReflectionService(this);
            contextMenuService = new ContextMenuService(contextMenu);
            contextMenuService.ItemClicked += delegate(object sender, EventArgs<string> s)
            {
                Avira.Win.Messaging.Message message = Avira.Win.Messaging.Message.CreateNotification(s.Value);
                scriptingObject.OnMessage(Envelope.Pack(message, "webHost"));
            };
            hostRouter = new HostRouter();
            messageConnector = new MessageConnector();
            hostRouter.AddConnection("VPN", () => PipeCommunicatorClient.Connect(ProductSettings.VpnPipeName, 500));
            hostRouter.AddConnection("WebAppHost", () => messageConnector.Source);
            messagRouter = new Router(messageConnector.Destination);
            messagRouter.AddAllRoutes(thisService);
            messagRouter.AddRoute("hide", this);
            messagRouter.AddRoute("closeWindow", this);
            messagRouter.AddRoute("showNotification", this);
            messagRouter.AddRoute("menu/add", this);
            messagRouter.AddRoute("menu/set", this);
            messagRouter.AddRoute("menu/itemClicked", this);
            messagRouter.AddRoute("systray/icon/set", this);
            messagRouter.AddRoute("systray/tooltip/set", this);
            messagRouter.AddRoute("userSettings/set", this);
            messagRouter.AddRoute("userSettings/get", this);
            messagRouter.AddRoute("openUrlInDefaultBrowser", this);
            messagRouter.AddRoute("systemSettings", this);
            messagRouter.AddRoute("startSettings", this);
            scriptingObject = new ScriptingObject
            {
                Messenger = hostRouter
            };
            webBrowserControl.ObjectForScripting = scriptingObject;
            CurrentConnectionStatus = ConnectionState.Unknown;
            ConnectToVpnService();
            UpdateTrackingSettings();
            WifiAutoconnect();
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            notifyIcon.BalloonTipClicked += delegate(object sender, EventArgs args)
            {
                string text2 = (sender as NotifyIcon)?.Tag as string;
                if (!(text2 == "register"))
                {
                    if (text2 == "learn")
                    {
                        SendLearnMoreRequestToService();
                    }
                }
                else
                {
                    SendRegisterRequestToService();
                }

                Tracker.TrackEvent(Tracker.Events.OsNotificationClicked,
                    new Dictionary<string, string> { { "Notification Id", lastOsNotificationId } });
            };
            notifyIcon.Icon = Resources.Disconnected;
            webBrowserControl.ScriptErrorsSuppressed = false;
            webBrowserControl.DocumentCompleted += WebBrowserControlOnDocumentCompleted;
            webBrowserControl.Navigate(text);
            InitSharedEvent();
        }

        private static void CheckRelativeReferences(HtmlElement parent, string tag, string attribute)
        {
            if ((from HtmlElement script in parent.GetElementsByTagName(tag)
                    select script.GetAttribute(attribute)
                    into scriptPath
                    select new Uri(scriptPath, UriKind.Relative)).Any((Uri uri) => uri.IsAbsoluteUri))
            {
                throw new Exception("[error] all files must be local");
            }
        }

        private static void OnResponse(Avira.Win.Messaging.Message response)
        {
            if (response.Error != null && response.Error.Type != JTokenType.Null)
            {
                Serilog.Log.Error("Request failed. Method {0} Code {1} Message {2}", response.Method,
                    response.Error["code"], response.Error["message"]);
            }
        }

        private SystemSettingsData GetSystemSettings()
        {
            return new SystemSettingsData
            {
                Theme = systemSettings.ThemeAppsUse
            };
        }

        private static void ShowWebPageInDefaultBrowser(Avira.Win.Messaging.Message message)
        {
            string text = message?.Params?["url"].ToString();
            if (text != null && (text.StartsWith("http://") || text.StartsWith("https://")))
            {
                Process.Start(text);
            }
        }

        private static bool IsServiceInstalled()
        {
            try
            {
                if (ServiceController.GetServices().Any((ServiceController service) =>
                        service.ServiceName == ProductSettings.ServiceName))
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "Failed to query installed services.");
            }

            return false;
        }

        private static bool IsServiceRunning()
        {
            using ServiceController serviceController = new ServiceController
            {
                ServiceName = ProductSettings.ServiceName
            };
            return serviceController.Status == ServiceControllerStatus.Running;
        }

        private void InitSharedEvent()
        {
            try
            {
                serviceStardedEvent.Create();
                ThreadPool.RegisterWaitForSingleObject(serviceStardedEvent.Handle, delegate { ReconnectToService(); },
                    null, -1, executeOnlyOnce: false);
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Failed to init the shared event.");
            }
        }

        private void UpdateTrackingSettings()
        {
            Avira.Win.Messaging.Message message = Avira.Win.Messaging.Message.CreateRequest("appSettings/get");
            vpn?.Request(message, delegate(Avira.Win.Messaging.Message m)
            {
                try
                {
                    if (m.Result?["appImprovement"] != null)
                    {
                        ProductSettings.ProductImprovementUserSetting = (bool)m.Result["appImprovement"];
                    }
                }
                catch (Exception exception)
                {
                    Serilog.Log.Error(exception, "Failed to update appImprovement settings.");
                }
            }, null);
        }

        private void WifiAutoconnect()
        {
            Avira.Win.Messaging.Message message = Avira.Win.Messaging.Message.CreateRequest("wifiAutoconnect");
            vpn?.Request(message, delegate { },
                delegate(Avira.Win.Messaging.Message err)
                {
                    Serilog.Log.Error("wifiAutoconnect error: " + err.ToString());
                });
        }

        protected override void SetVisibleCore(bool value)
        {
            if (startMinimized)
            {
                value = false;
                if (!base.IsHandleCreated)
                {
                    CreateHandle();
                }
            }

            base.SetVisibleCore(value);
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs powerModeChangedEventArgs)
        {
            if (powerModeChangedEventArgs.Mode == PowerModes.Resume)
            {
                vpn.Request(Avira.Win.Messaging.Message.CreateRequest("status"), OnResponse, null);
                if (suspendedConnectionStatus == ConnectionState.Connected && IsDisconnected)
                {
                    PowerModeResumeReconnect();
                }
            }
            else if (powerModeChangedEventArgs.Mode == PowerModes.Suspend)
            {
                suspendedConnectionStatus = CurrentConnectionStatus;
            }
        }

        private void ConnectToVpnService()
        {
            if (TryConnectToVpnService() || startMinimized)
            {
                return;
            }

            if (!IsServiceInstalled())
            {
                Serilog.Log.Error("The product is corrupted. Showing reinstall message box.");
                MessageBox.Show("The product is corrupted. Please reinstall.", ProductSettings.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            else if (!IsServiceRunning())
            {
                Serilog.Log.Debug("The VPN Service is not running. Trying to start it...");
                if (StartService())
                {
                    TryConnectToVpnService();
                }
            }
        }

        private bool StartService()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
                Verb = "runas",
                Arguments = "/service"
            };
            try
            {
                Process process = Process.Start(startInfo);
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Failed to start service from WebAppHost.");
            }

            return false;
        }

        private bool TryConnectToVpnService()
        {
            bool result = false;
            try
            {
                Serilog.Log.Debug("VpnGui: connecting to service ...");
                ServiceInterfaceFactory serviceInterfaceFactory =
                    new ServiceInterfaceFactory(new ServiceLocator(), new PipeChannelFactory());
                vpn = serviceInterfaceFactory.CreateServiceInterface("VPN");
                SubscribeToService();
                result = true;
                return result;
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Error on creating communication chanel with service.");
                return result;
            }
        }

        private void SubscribeToService()
        {
            if (vpn != null)
            {
                vpn.Subscribe("status", ChangeStatus);
                vpn.Subscribe("queryForUpdate", delegate { OnQueryForUpdate(); });
                vpn.Subscribe("prepareForUpdate", delegate { Invoke(new MethodInvoker(base.Close)); });
                vpn.Subscribe("showNotification",
                    delegate(Avira.Win.Messaging.Message m) { ShowWindowsNotification(m); });
            }
        }

        private void ShowWindowsNotification(Avira.Win.Messaging.Message msg)
        {
            JObject jObject = msg.Params as JObject;
            int timeout = ((jObject["timeout"] == null) ? 30000 : ((int)jObject["timeout"]));
            ToolTipIcon toolTipIcon = ((jObject["type"] == null)
                ? ToolTipIcon.Warning
                : GetTooltipIcon(jObject["type"]!.ToString()));
            string action = ((jObject["action"] == null) ? "" : jObject["action"]!.ToString());
            string notificationId = ((jObject["notificationId"] == null)
                ? string.Empty
                : jObject["notificationId"]!.ToString());
            ShowBaloon(jObject["message"]!.ToString(), toolTipIcon, timeout, action, notificationId);
        }

        private ToolTipIcon GetTooltipIcon(string type)
        {
            return type switch
            {
                "info" => ToolTipIcon.Info,
                "error" => ToolTipIcon.Error,
                "warning" => ToolTipIcon.Warning,
                _ => ToolTipIcon.None,
            };
        }

        private void OnQueryForUpdate()
        {
            if (base.Visible)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    vpn.Request(Avira.Win.Messaging.Message.CreateRequest("updater/defereUpdate"), null, null);
                });
            }
        }

        private void ChangeStatus(Avira.Win.Messaging.Message message)
        {
            try
            {
                JToken jToken = MessageSerializer.ToJObject(message)["params"]!["status"];
                CurrentConnectionStatus = ConvertToConnectionState(jToken.ToString());
            }
            catch (Exception exception)
            {
                Serilog.Log.Debug(exception, $"Error on getting current status from message : {message}.");
                throw;
            }
        }

        private ConnectionState ConvertToConnectionState(string state)
        {
            return state.ToLower() switch
            {
                "connected" => ConnectionState.Connected,
                "connecting" => ConnectionState.Connecting,
                "disconnected" => ConnectionState.Disconnected,
                "disconnecting" => ConnectionState.Disconnecting,
                _ => ConnectionState.Unknown,
            };
        }

        private void WebBrowserControlOnDocumentCompleted(object sender,
            WebBrowserDocumentCompletedEventArgs webBrowserDocumentCompletedEventArgs)
        {
            if (webBrowserControl.ReadyState == WebBrowserReadyState.Complete)
            {
                SetHtmlControlEvents();
                try
                {
                    VerifyReferencePathes();
                    new GlobalWindow().Set(base.Handle);
                }
                catch (Exception)
                {
                    Hide();
                    MessageBox.Show("The product is corrupted. Please reinstall.", ProductSettings.ProductName,
                        MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    Close();
                }
            }
        }

        private void VerifyReferencePathes()
        {
            HtmlElement obj = webBrowserControl.Document?.GetElementsByTagName("head")[0];
            if (obj == null)
            {
                throw new Exception("[error] incorect document loaded");
            }

            CheckRelativeReferences(obj, "script", "src");
            CheckRelativeReferences(obj, "link", "href");
        }

        private void SetHtmlControlEvents()
        {
            if (webBrowserControl.Document == null || htmlDocument != null)
            {
                return;
            }

            htmlDocument = webBrowserControl.Document;
            htmlDocument.ContextMenuShowing += HtmlDoc_ContextMenuShowing;
            HtmlElement elementById = htmlDocument.GetElementById("header");
            if (elementById != null)
            {
                elementById.MouseDown += HtmlDoc_MouseDown;
                elementById.MouseMove += HtmlDoc_MouseMove;
                elementById.MouseUp += HtmlDoc_MouseUp;
            }

            HTMLDocumentEvents2_Event hTMLDocumentEvents2_Event =
                (HTMLDocumentEvents2_Event)webBrowserControl.Document.DomDocument;
            if (hTMLDocumentEvents2_Event != null)
            {
                new ComAwareEventInfo(typeof(HTMLDocumentEvents2_Event), "onmousewheel").AddEventHandler(
                    hTMLDocumentEvents2_Event,
                    new HTMLDocumentEvents2_onmousewheelEventHandler(DocumentEvent_OnMouseWheel));
                new ComAwareEventInfo(typeof(HTMLDocumentEvents2_Event), "onkeydown").AddEventHandler(
                    hTMLDocumentEvents2_Event,
                    (HTMLDocumentEvents2_onkeydownEventHandler)delegate(IHTMLEventObj e) { CancelZoomInOut(e); });
                new ComAwareEventInfo(typeof(HTMLDocumentEvents2_Event), "onkeyup").AddEventHandler(
                    hTMLDocumentEvents2_Event,
                    (HTMLDocumentEvents2_onkeyupEventHandler)delegate(IHTMLEventObj e) { CancelZoomInOut(e); });
            }
        }

        private static void CancelZoomInOut(IHTMLEventObj e)
        {
            if (e.ctrlKey && (e.keyCode == 187 || e.keyCode == 189))
            {
                e.cancelBubble = true;
                e.returnValue = false;
            }
        }

        private bool DocumentEvent_OnMouseWheel(IHTMLEventObj evtobj)
        {
            //if (evtobj.ctrlKey)
            //{
            //    evtobj.cancelBubble = true;
            //    evtobj.returnValue = false;
            //    return false;
            //}
            return true;
        }

        private void OnHide()
        {
            Hide();
            vpn?.Request(Avira.Win.Messaging.Message.CreateRequest("closeClicked"), null, null);
        }

        private void SendRegisterRequestToService()
        {
            vpn?.Request(Avira.Win.Messaging.Message.CreateRequest("registerUser"), OnResponse, null);
        }

        private void SendLearnMoreRequestToService()
        {
            vpn?.Request(Avira.Win.Messaging.Message.CreateRequest("learnMore"), OnResponse, null);
        }

        private void HtmlDoc_ContextMenuShowing(object sender, HtmlElementEventArgs e)
        {
            e.ReturnValue = false;
        }

        private void HtmlDoc_MouseDown(object sender, HtmlElementEventArgs e)
        {
            firstPoint = e.MousePosition;
            mouseIsDown = true;
        }

        private void HtmlDoc_MouseUp(object sender, HtmlElementEventArgs e)
        {
            mouseIsDown = false;
        }

        private void HtmlDoc_MouseMove(object sender, HtmlElementEventArgs e)
        {
            if (e.MousePosition == mousePoint)
            {
                return;
            }

            mousePoint = e.MousePosition;
            if (e.MouseButtonsPressed != MouseButtons.Left)
            {
                mouseIsDown = false;
                return;
            }

            if (e.MousePosition.X < 0)
            {
                Serilog.Log.Debug("mousemove: " + e.MousePosition);
            }

            if (mouseIsDown)
            {
                int num = firstPoint.X - e.MousePosition.X;
                int num2 = firstPoint.Y - e.MousePosition.Y;
                int num3 = base.Location.X - num;
                int num4 = base.Location.Y - num2;
                base.Location = new Point(num3, num4);
            }
        }

        private void ShowBaloon(string message, ToolTipIcon toolTipIcon = ToolTipIcon.Warning, int timeout = 30000,
            string action = "register", string notificationId = "")
        {
            lastOsNotificationId = notificationId;
            notifyIcon.BalloonTipTitle = ProductSettings.ProductName;
            notifyIcon.BalloonTipText = message;
            notifyIcon.BalloonTipIcon = toolTipIcon;
            notifyIcon.Tag = action;
            notifyIcon.ShowBalloonTip(timeout);
            Tracker.TrackEvent(Tracker.Events.OsNotificationShown,
                new Dictionary<string, string> { { "Notification Id", lastOsNotificationId } });
        }

        private void PowerModeResumeReconnect()
        {
            Avira.Win.Messaging.Message message =
                Avira.Win.Messaging.Message.CreateNotification("powerModeResumeConnect");
            scriptingObject.OnMessage(Envelope.Pack(message, "webHost"));
        }

        private void ReconnectToService()
        {
            Serilog.Log.Information("Reconnection to the service.");
            Avira.Win.Messaging.Message message =
                Avira.Win.Messaging.Message.CreateNotification("connectionReestablished");
            scriptingObject.OnMessage(Envelope.Pack(message, "webHost"));
            vpn = null;
            TryConnectToVpnService();
        }

        private void Unsubscribe()
        {
            try
            {
                vpn?.Unsubscribe("status", ChangeStatus);
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Failed to unsubscribe from status changes.");
            }
        }

        private void CloseVpnConnection()
        {
            Unsubscribe();
            scriptingObject.Close();
        }

        private void VpnGuiForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ProductSettings.WindowLocation = base.Location;
            if (!showSettingsView)
            {
                CloseVpnConnection();
            }
        }

        private void VpnGuiForm_Load(object sender, EventArgs e)
        {
            base.Location = ProductSettings.WindowLocation;
            if (!(base.Location != new Point(-1, -1)) || base.Width != 392 || base.Height != 550 ||
                !IsOnScreen(base.Location))
            {
                Screen primaryScreen = Screen.PrimaryScreen;
                base.Width = 392;
                base.Height = 550;
                base.Location = new Point(primaryScreen.WorkingArea.Right - base.Width,
                    primaryScreen.WorkingArea.Bottom - base.Height);
            }
        }

        public bool IsOnScreen(Point topLeft)
        {
            Point checkPoint = topLeft + new Size(50, 50);
            return Screen.AllScreens.Any((Screen screen) => screen.WorkingArea.Contains(checkPoint));
        }

        private void VpnGuiForm_SizeChanged(object sender, EventArgs e)
        {
            if (base.WindowState == FormWindowState.Maximized)
            {
                base.WindowState = FormWindowState.Normal;
            }
        }

        protected Icon GetIcon(string iconId)
        {
            switch (iconId)
            {
                case "connected":
                    return Resources.Connected;
                case "disconnected":
                    return Resources.Disconnected;
                case "connecting":
                case "disconnecting":
                    return Resources.Connecting;
                default:
                    return Resources.Disconnected;
            }
        }

        public void Request(Avira.Win.Messaging.Message message, Action<Avira.Win.Messaging.Message> onResponse,
            Action<Avira.Win.Messaging.Message> onError)
        {
            Avira.Win.Messaging.Message message2 = null;
            switch (message.Method)
            {
                case "showNotification":
                    ShowWindowsNotification(message);
                    break;
                case "menu/add":
                    contextMenuService.Add((JObject)message.Params);
                    break;
                case "menu/set":
                    contextMenuService.Set((JObject)message.Params);
                    break;
                case "systray/icon/set":
                    notifyIcon.Icon = GetIcon(message.Params.ToString());
                    break;
                case "systray/tooltip/set":
                    notifyIcon.Text = message.Params.ToString();
                    break;
                case "closeWindow":
                    Invoke(new MethodInvoker(base.Close));
                    break;
                case "hide":
                    if (showSettingsView)
                    {
                        Invoke(new MethodInvoker(base.Close));
                    }
                    else
                    {
                        Invoke(new MethodInvoker(OnHide));
                    }

                    break;
                case "userSettings/get":
                case "userSettings/set":
                    message2 = new UserSettings().HandleRequest(message);
                    break;
                case "openUrlInDefaultBrowser":
                    ShowWebPageInDefaultBrowser(message);
                    break;
                case "systemSettings":
                    message2 = Avira.Win.Messaging.Message.CreateResponse(message, GetSystemSettings());
                    break;
                case "startSettings":
                    message2 = Avira.Win.Messaging.Message.CreateResponse(message, showSettingsView);
                    break;
            }

            if (message2 == null)
            {
                message2 = Avira.Win.Messaging.Message.CreateResponse(message, "OK");
            }

            onResponse(message2);
        }

        public void Subscribe(string messageCommand, Action<Avira.Win.Messaging.Message> onMessage)
        {
        }

        public void Unsubscribe(string messageCommand, Action<Avira.Win.Messaging.Message> onMessage)
        {
        }

        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!base.Visible)
                {
                    startMinimized = false;
                    Tracker.TrackEvent(Tracker.Events.GuiOpenedTrigger,
                        new Dictionary<string, string> { { "Trigger Source", "Systray Icon" } });
                    Show();
                    Activate();
                }
                else
                {
                    Hide();
                }
            }
        }

        private void VpnGuiForm_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        private void NotifyUiVisibilityChanged()
        {
            string text = (base.Visible ? "visible" : "hidden");
            Serilog.Log.Debug("UI visibility : " + text + ".");
            Avira.Win.Messaging.Message message = Avira.Win.Messaging.Message.CreateRequest("uiVisibilityChanged");
            message.Params = Avira.Win.Messaging.Message.ToJObject(base.Visible);
            vpn?.Request(message, OnResponse, null);
        }

        private void VpnGuiForm_VisibleChanged(object sender, EventArgs e)
        {
            NotifyUiVisibilityChanged();
            if (base.Visible && !startMinimized)
            {
                ProductSettings.LastGuiOpened = DateTime.Now;
                Tracker.TrackEvent(Tracker.Events.GuiOpened);
            }
        }

        private void NotifyOsThemeChanged(string theme)
        {
            Avira.Win.Messaging.Message message = Avira.Win.Messaging.Message.CreateRequest("osThemeChanged");
            message.Params = Avira.Win.Messaging.Message.ToJObject(theme);
            vpn?.Request(message, OnResponse, null);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                if (hostRouter != null)
                {
                    hostRouter.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources =
                new System.ComponentModel.ComponentResourceManager(typeof(Avira.WebAppHost.VpnGuiForm));
            webBrowserControl = new System.Windows.Forms.WebBrowser();
            notifyIcon = new System.Windows.Forms.NotifyIcon(components);
            contextMenu = new System.Windows.Forms.ContextMenuStrip(components);
            SuspendLayout();
            webBrowserControl.AllowWebBrowserDrop = false;
            webBrowserControl.Dock = System.Windows.Forms.DockStyle.Fill;
            webBrowserControl.IsWebBrowserContextMenuEnabled = false;
            webBrowserControl.Location = new System.Drawing.Point(0, 0);
            webBrowserControl.MinimumSize = new System.Drawing.Size(20, 20);
            webBrowserControl.Name = "webBrowserControl";
            webBrowserControl.ScrollBarsEnabled = false;
            webBrowserControl.Size = new System.Drawing.Size(392, 550);
            webBrowserControl.TabIndex = 0;
            webBrowserControl.WebBrowserShortcutsEnabled = false;
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Icon = (System.Drawing.Icon)resources.GetObject("notifyIcon.Icon");
            notifyIcon.Text = Avira.VPN.Core.Win.ProductSettings.ProductName;
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(NotifyIcon_MouseClick);
            contextMenu.Name = "contextMenu";
            contextMenu.Size = new System.Drawing.Size(61, 4);
            base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new System.Drawing.Size(392, 550);
            base.ControlBox = false;
            base.Controls.Add(webBrowserControl);
            ForeColor = System.Drawing.SystemColors.ControlDark;
            base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            base.Icon = Avira.WebAppHost.Properties.Resources.appIcon;
            base.Name = "VpnGuiForm";
            Text = Avira.VPN.Core.Win.ProductSettings.ProductName;
            base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(VpnGuiForm_FormClosing);
            base.FormClosed += new System.Windows.Forms.FormClosedEventHandler(VpnGuiForm_FormClosed);
            base.Load += new System.EventHandler(VpnGuiForm_Load);
            base.SizeChanged += new System.EventHandler(VpnGuiForm_SizeChanged);
            base.VisibleChanged += new System.EventHandler(VpnGuiForm_VisibleChanged);
            ResumeLayout(false);
        }
    }
}