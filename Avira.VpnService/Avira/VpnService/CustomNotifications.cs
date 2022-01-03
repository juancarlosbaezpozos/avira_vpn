using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VpnService
{
    public class CustomNotifications
    {
        private readonly IHttpClient httpClient;

        private readonly IStorage storage;

        private readonly IStorageSecurity storageSecurity;

        private readonly IFileFactory fileFactory;

        private readonly string customNotificationsDir;

        private readonly SemaphoreLock updateNotificationsLock = new SemaphoreLock();

        public CustomNotifications(IHttpClient httpClient, IStorage storage, IStorageSecurity storageSecurity,
            IFileFactory fileFactory)
        {
            this.httpClient = httpClient;
            this.storage = storage;
            this.storageSecurity = storageSecurity;
            this.fileFactory = fileFactory;
            customNotificationsDir = Path.Combine(ProductSettings.SettingsFilePath, "custom_notification");
        }

        public CustomNotifications(IStorage storage)
            : this(new HttpClient(), storage, new StorageSecurity(), new FileFactory())
        {
        }

        public async Task Update(List<RemoteFeatureData> remoteFeatures)
        {
            using (await updateNotificationsLock.AquireLockAsync())
            {
                List<RemoteFeatureData> notifications =
                    remoteFeatures.FindAll((RemoteFeatureData f) => f.Id == "custom_notification");
                await DownloadNewNotifications(notifications);
            }
        }

        public async Task<string> GetCustomNotificationPath(string id, List<RemoteFeatureData> remoteFeatures)
        {
            try
            {
                RemoteFeatureData remoteFeatureData = remoteFeatures
                    ?.FindAll((RemoteFeatureData f) => f.Id == "custom_notification")?.Find((RemoteFeatureData f) =>
                        f.Params?.ToObject<CustomNotificationsData>().Id ==
                        NotificationIdToRemoteCustomNoficationId(id));
                if (remoteFeatureData == null || !remoteFeatureData.IsActive)
                {
                    return string.Empty;
                }

                string productLanguage = ProductSettings.ProductLanguage;
                CustomNotificationsData customNotificationsData =
                    remoteFeatureData.Params.ToObject<CustomNotificationsData>();
                string notificationPath = Path.Combine(GetNotificationDownloadDir(customNotificationsData),
                    customNotificationsData.Id + "." + productLanguage + ".html");
                return (await IsNotificationValid(customNotificationsData, notificationPath))
                    ? notificationPath
                    : string.Empty;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to get custom notification for notification id: " + id);
                return string.Empty;
            }
        }

        private string NotificationIdToRemoteCustomNoficationId(string internalNotificationId)
        {
            return internalNotificationId switch
            {
                "Inactivity" => "inactivity_notification",
                "GiveGeneralFeedback" => "general_feedback_notification",
                "GiveProductFeedback" => "product_feedback_notification",
                "UnkownWifi" => "unknown_wifi_notification",
                "UnsecureWifi" => "unsecure_wifi_notification",
                "TrafficLimitReached" => "traffic_limit_notification",
                "Traffic50PercentReached" => "traffic_50_percent_notification",
                "Traffic80PercentReached" => "traffic_80_percent_notification",
                "Traffic90PercentReached" => "traffic_90_percent_notification",
                "KillSwitch" => "kill_switch_notification",
                "Update" => "update_notification",
                "Upgrade" => "upgrade_notification",
                _ => string.Empty,
            };
        }

        private async Task<bool> IsNotificationValid(CustomNotificationsData notification, string notificationPath)
        {
            string text = storage.Get(GetSettingsKey(notification));
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (new Version(text) != new Version(notification.Version))
            {
                return false;
            }

            return await fileFactory.CreateFile(notificationPath).Exists();
        }

        private async Task DownloadNewNotifications(List<RemoteFeatureData> notifications)
        {
            foreach (RemoteFeatureData notification in notifications)
            {
                CustomNotificationsData notificationData = notification.Params.ToObject<CustomNotificationsData>();
                bool flag = notification.IsActive;
                if (flag)
                {
                    flag = !(await IsNotificationDownloaded(notificationData));
                }

                if (flag)
                {
                    await DownloadNotificationAndUnzip(notificationData);
                }
            }
        }

        private async Task DownloadNotificationAndUnzip(CustomNotificationsData notification)
        {
            TaskCompletionSource<AsyncCompletedEventArgs> downloadTaskCompletionSource =
                new TaskCompletionSource<AsyncCompletedEventArgs>();
            string destinationPath = await CreateSecureDownloadLocation(notification);
            httpClient.DownloadFileAsync(notification.BundleUrl, destinationPath,
                delegate(object sender, AsyncCompletedEventArgs e) { downloadTaskCompletionSource.SetResult(e); });
            AsyncCompletedEventArgs asyncCompletedEventArgs = await downloadTaskCompletionSource.Task;
            if (asyncCompletedEventArgs.Cancelled || asyncCompletedEventArgs.Error != null)
            {
                Log.Error($"Failed to download custom notification {asyncCompletedEventArgs}");
                return;
            }

            await UnzipCustomNotification(notification, destinationPath);
            storage.Set(GetSettingsKey(notification), notification.Version);
        }

        private Task UnzipCustomNotification(CustomNotificationsData notification, string notificationZip)
        {
            return Task.Run(delegate
            {
                fileFactory.CreateFile(notificationZip).UnzipToDirectory(GetNotificationDownloadDir(notification));
            });
        }

        private Task<string> CreateSecureDownloadLocation(CustomNotificationsData notification)
        {
            return Task.Run(async delegate
            {
                Uri downloadUri = new Uri(notification.BundleUrl);
                if (!downloadUri.Segments.Last().Contains("zip"))
                {
                    throw new Exception("CustomNotification URL doesn't point to a zip file.");
                }

                string downloadDir = GetNotificationDownloadDir(notification);
                IFile dir = fileFactory.CreateFile(downloadDir);
                if (await dir.Exists())
                {
                    await dir.Delete();
                }

                await dir.Create(pathIsDirectory: true);
                return Path.Combine(downloadDir, Path.GetFileName(downloadUri.LocalPath));
            });
        }

        private string GetNotificationDownloadDir(CustomNotificationsData notification)
        {
            return Path.Combine(customNotificationsDir, notification.Id + "\\" + notification.Version);
        }

        private string GetSettingsKey(CustomNotificationsData notification)
        {
            return "Notification.Id." + notification.Id + ".Version";
        }

        private async Task<bool> IsNotificationDownloaded(CustomNotificationsData notification)
        {
            string text = storage.Get(GetSettingsKey(notification));
            IFile file = fileFactory.CreateFile(GetNotificationDownloadDir(notification));
            bool flag = !string.IsNullOrEmpty(text) && new Version(text) == new Version(notification.Version);
            if (flag)
            {
                flag = await file.Exists();
            }

            return flag;
        }
    }
}