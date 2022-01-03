using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;
using Avira.VPN.Core.Win;
using Avira.Win.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Notifier
{
    [ComVisible(true)]
    public class NotificationWindow : Form, IService
    {
        private readonly PipeCommunicatorServer server;

        private readonly ReflectionService reflectionService;

        private readonly Router router;

        private readonly ConcurrentDictionary<string, Action<Avira.Win.Messaging.Message>> subscriptions =
            new ConcurrentDictionary<string, Action<Avira.Win.Messaging.Message>>();

        private readonly Timer closeTimer = new Timer();

        private readonly Timer timeoutTimer = new Timer();

        private bool mouseIsDown;

        private Point firstPoint;

        private Point mousePoint;

        private Notification.Command closeCommand = new Notification.Command();

        private bool hidden = true;

        private Notification currentNotification;

        private IContainer components;

        private WebBrowser webBrowser1;

        public NotificationWindow()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,
                PipeAccessRights.FullControl, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
            server = new PipeCommunicatorServer(ProductSettings.VpnNotifierPipeName, pipeSecurity);
            try
            {
                InitializeComponent();
            }
            catch (Exception exception)
            {
                Log.Error(exception, "InitializeComponent failed for notifier.");
            }

            string urlString = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates\\Template1.html");
            webBrowser1.ObjectForScripting = this;
            webBrowser1.Navigate(urlString);
            reflectionService = new ReflectionService(this);
            router = new Router(new Multiplexer(server));
            router.AddAllRoutes(reflectionService);
            router.AddRoute("action", this);
            server.Start();
            base.Visible = false;
            closeTimer.Interval = 30000;
            closeTimer.Tick += CloseTimer_Tick;
            closeTimer.Start();
            timeoutTimer.Tick += TimeoutTimer_Tick;
        }

        private void TimeoutTimer_Tick(object sender, EventArgs e)
        {
            Invoke(new MethodInvoker(CloseOnTimeout));
        }

        private void CloseOnTimeout()
        {
            PerformAction("Timeout");
        }

        private void CloseCurrentNotification(string closeId)
        {
            PerformAction(closeId, SetDontShowActionParams());
        }

        private string SetDontShowActionParams()
        {
            string value = webBrowser1.Document.GetElementById("checkDontShowAgain")?.GetAttribute("checked");
            if (!string.IsNullOrEmpty(value))
            {
                return JsonConvert.SerializeObject(new JObject { ["dont_show_again"] = (JToken)bool.Parse(value) });
            }

            return null;
        }

        private void CloseWindow()
        {
            if (base.Visible)
            {
                timeoutTimer.Stop();
                closeTimer.Start();
                Hide();
                currentNotification = null;
            }
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            Invoke(new MethodInvoker(base.Close));
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs completedEvent)
        {
            HtmlElement elementById = webBrowser1.Document.GetElementById("close");
            if (elementById != null)
            {
                elementById.Click += Close_Click;
            }

            HtmlElement elementById2 = webBrowser1.Document.GetElementById("close2");
            if (elementById2 != null)
            {
                elementById2.Click += Close_Click;
                elementById2.InnerText = currentNotification.Close2 ?? elementById2.InnerText;
            }

            if (currentNotification == null)
            {
                return;
            }

            string iconFileName = GetIconFileName(currentNotification);
            if (!string.IsNullOrEmpty(iconFileName))
            {
                HtmlElement elementById3 = webBrowser1.Document.GetElementById("icon");
                if (elementById3 != null)
                {
                    elementById3.SetAttribute("src", iconFileName);
                }
            }

            if (currentNotification.IsMovable)
            {
                webBrowser1.Document.Body.MouseDown += HtmlDoc_MouseDown;
                webBrowser1.Document.Body.MouseMove += HtmlDoc_MouseMove;
                webBrowser1.Document.Body.MouseUp += HtmlDoc_MouseUp;
            }

            if (!string.IsNullOrEmpty(currentNotification.Message))
            {
                SetElementText("message", currentNotification.Message);
            }

            if (!string.IsNullOrEmpty(currentNotification.Title))
            {
                SetElementText("title", currentNotification.Title);
            }

            if (!string.IsNullOrEmpty(currentNotification.Title2))
            {
                SetElementText("title2", currentNotification.Title2);
            }

            if (!string.IsNullOrEmpty(currentNotification.Question))
            {
                SetElementText("question", currentNotification.Question);
            }

            if (!string.IsNullOrEmpty(currentNotification.Hint))
            {
                SetElementText("hint", currentNotification.Hint);
            }

            if (!string.IsNullOrEmpty(currentNotification.Image))
            {
                SetImageName("image", currentNotification.Image);
            }

            if (currentNotification.Ftu != null)
            {
                string text = JsonConvert.SerializeObject(currentNotification.Ftu);
                webBrowser1.Document.InvokeScript("initPages",
                    new object[2] { text, currentNotification.TrialDisabled });
            }

            ConnectAction("action1", currentNotification.Action1, PerformAction1);
            ConnectAction("action2", currentNotification.Action2, PerformAction2);
            Show();
            UpdatePosition();
        }

        private void SetElementText(string id, string text)
        {
            HtmlElement elementById = webBrowser1.Document.GetElementById(id);
            if (elementById != null)
            {
                elementById.InnerHtml = text;
            }
        }

        private void SetImageName(string id, string name)
        {
            HtmlElement elementById = webBrowser1.Document.GetElementById(id);
            if (elementById != null)
            {
                name = "images/" + name;
                elementById.SetAttribute("src", name);
            }
        }

        private void UpdatePosition()
        {
            Rectangle clientRectangle = webBrowser1.Document.Body.ClientRectangle;
            Size size = new Size(2, 2);
            base.Size = clientRectangle.Size + size;
            if (currentNotification.Position == Notification.PositionType.CenterScreen)
            {
                CenterToScreen();
            }
            else
            {
                base.Location = new Point(Screen.PrimaryScreen.Bounds.Width - base.Size.Width - 50,
                    Screen.PrimaryScreen.Bounds.Height - base.Size.Height - 70);
            }
        }

        private void ConnectAction(string id, Notification.Command command, Action<HtmlElement> handler)
        {
            HtmlElement elem = webBrowser1.Document.GetElementById(id);
            if (!(elem == null))
            {
                elem.InnerText = command?.Text ?? elem.InnerText;
                elem.Click += delegate { handler(elem); };
            }
        }

        private void PerformAction1(HtmlElement element)
        {
            ExecuteSelectedAction(currentNotification?.Action1?.Id, element);
        }

        private void PerformAction2(HtmlElement element)
        {
            ExecuteSelectedAction(currentNotification?.Action2?.Id, element);
        }

        private void ExecuteSelectedAction(string notificationActionId, HtmlElement element)
        {
            string obj = element?.GetAttribute("action");
            string text2 = element?.GetAttribute("action-param");
            string text3 = EmptyToNull(notificationActionId);
            string text4 = EmptyToNull(obj) ?? text3;
            if (currentNotification != null && text4 != null)
            {
                PerformAction(text4, (!string.IsNullOrEmpty(text2)) ? text2 : SetDontShowActionParams());
            }

            static string EmptyToNull(string text)
            {
                if (!(text == string.Empty))
                {
                    return text;
                }

                return null;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (hidden)
            {
                value = false;
                if (!base.IsHandleCreated)
                {
                    CreateHandle();
                }
            }

            base.SetVisibleCore(value);
        }

        [Routing("notification/show")]
        public void ShowNotification(Notification notification)
        {
            Log.Debug("Got template " + notification.TemplateName);
            if (!notification.OnlyIfNoForegroundUiWindow || !IsMainUiForeground())
            {
                Invoke((MethodInvoker)delegate { Notify(notification); });
            }
        }

        private void Notify(Notification notification)
        {
            if (currentNotification != null)
            {
                if (notification.Priority <= currentNotification.Priority)
                {
                    Log.Debug("Ignored received notification. An existing notification is in progress.");
                    return;
                }

                Log.Debug("Closing existing notification to display higher prioirty notification.");
                CloseCurrentNotification("LowerPriority");
            }

            currentNotification = notification;
            Log.Debug("Notifying the user: " + notification.Message);
            closeTimer.Stop();
            hidden = false;
            if (notification.Timeout > 0)
            {
                timeoutTimer.Interval = notification.Timeout;
                timeoutTimer.Start();
            }

            string text = notification.TemplateName;
            if (notification.Template != Notification.TemplateType.CustomTemplate)
            {
                text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GetTemplateFilename(notification));
            }

            Log.Debug("Loading " + text + " template");
            webBrowser1.Navigate(text);
        }

        private static bool IsMainUiForeground()
        {
            GlobalWindow globalWindow = new GlobalWindow();
            if (globalWindow.FindInstance(Process.GetProcessesByName("Avira.WebAppHost")) != null)
            {
                return globalWindow.IsForeground;
            }

            return false;
        }

        private string GetIconFileName(Notification notification)
        {
            return notification.Icon switch
            {
                Notification.IconType.Default => string.Empty,
                Notification.IconType.Alert => "images/alert.png",
                Notification.IconType.Check => "images/alert.png",
                Notification.IconType.Feedback => "images/feedback.png",
                _ => string.Empty,
            };
        }

        public void Exit()
        {
            CloseCurrentNotification("Exit");
        }

        private string GetTemplateFilename(Notification notification)
        {
            if (!string.IsNullOrEmpty(notification.TemplateName))
            {
                return "Templates/" + notification.TemplateName + ".html";
            }

            switch (notification.Template)
            {
                case Notification.TemplateType.Auto:
                    if (notification.Action2 != null)
                    {
                        return "Templates/Template2.html";
                    }

                    if (notification.Action1 != null)
                    {
                        return "Templates/Template1.html";
                    }

                    return "Templates/Template0.html";
                case Notification.TemplateType.Template0:
                    return "Templates/Template0.html";
                case Notification.TemplateType.Template1:
                    return "Templates/Template1.html";
                case Notification.TemplateType.Template2:
                    return "Templates/Template2.html";
                default:
                    return "Templates/Template0.html";
            }
        }

        public void ExecuteAction(string actionId)
        {
            PerformAction(actionId);
        }

        private void PerformAction(string actionId, string actionParam = null)
        {
            string text = "action";
            if (subscriptions.TryGetValue(text, out var value) && value != null)
            {
                Avira.Win.Messaging.Message message = Avira.Win.Messaging.Message.CreateNotification(text);
                JObject jObject = (JObject)(message.Params = new JObject
                {
                    ["RequestId"] = (JToken)currentNotification.UniqueId,
                    ["ActionId"] = (JToken)actionId,
                    ["ActionParam"] = (JToken)actionParam
                });
                value(message);
            }

            CloseWindow();
        }

        private void Close_Click(object sender, HtmlElementEventArgs e)
        {
            CloseCurrentNotification("Cancel");
        }

        private void NotificationWindow_Load(object sender, EventArgs e)
        {
        }

        public void Request(Avira.Win.Messaging.Message message, Action<Avira.Win.Messaging.Message> onResponse,
            Action<Avira.Win.Messaging.Message> onError)
        {
        }

        public void Subscribe(string messageCommand, Action<Avira.Win.Messaging.Message> onMessage)
        {
            Log.Debug("Notifier got subscription " + messageCommand);
            subscriptions[messageCommand] = onMessage;
        }

        public void Unsubscribe(string messageCommand, Action<Avira.Win.Messaging.Message> onMessage)
        {
            if (subscriptions.ContainsKey(messageCommand))
            {
                subscriptions[messageCommand] = null;
                subscriptions.TryRemove(messageCommand, out var _);
            }
        }

        private void NotificationWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Debug("Notification window closed");
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
                Log.Debug("mousemove: " + e.MousePosition);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            webBrowser1 = new System.Windows.Forms.WebBrowser();
            SuspendLayout();
            webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            webBrowser1.IsWebBrowserContextMenuEnabled = false;
            webBrowser1.Location = new System.Drawing.Point(0, 0);
            webBrowser1.Margin = new System.Windows.Forms.Padding(0);
            webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            webBrowser1.Name = "webBrowser1";
            webBrowser1.ScrollBarsEnabled = false;
            webBrowser1.Size = new System.Drawing.Size(563, 87);
            webBrowser1.TabIndex = 0;
            webBrowser1.WebBrowserShortcutsEnabled = false;
            webBrowser1.DocumentCompleted +=
                new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);
            base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new System.Drawing.Size(563, 87);
            base.Controls.Add(webBrowser1);
            base.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            base.Name = "NotificationWindow";
            base.ShowIcon = false;
            base.ShowInTaskbar = false;
            Text = "n";
            base.TopMost = true;
            base.FormClosing += new System.Windows.Forms.FormClosingEventHandler(NotificationWindow_FormClosing);
            base.Load += new System.EventHandler(NotificationWindow_Load);
            ResumeLayout(false);
        }
    }
}