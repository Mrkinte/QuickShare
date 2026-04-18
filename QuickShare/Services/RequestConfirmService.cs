using QuickShare.Models;
using QuickShare.ViewModels.Pages;
using QuickShare.Views.Pages;
using System.Collections.ObjectModel;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;
using Wpf.Ui;

namespace QuickShare.Services
{
    public partial class RequestConfirmService(
        AppConfigService appConfigService,
        INavigationService navigationService,
        TextMessageViewModel textMessageViewModel) : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<RequestModel> _guestRequests = new();

        [RelayCommand]
        private void OnAgreeRequest(RequestModel model)
        {
            model.Status = 1;
            model.Visibility = Visibility.Collapsed;

            if (model.Type == 1)
            {
                textMessageViewModel.Sender = model.Name;
                textMessageViewModel.Message = model.Description;
                _ = navigationService.NavigateWithHierarchy(typeof(TextMessagePage));
            }
        }

        [RelayCommand]
        private void OnRefuseRequest(RequestModel model)
        {
            model.Status = 2;
            model.Visibility = Visibility.Collapsed;
        }

        public void SetRequestsControl(ItemsControl control)
        {
            control.ItemsSource = GuestRequests;
            control.DataContext = this;
        }

        public string CreateUploadRequest(string guestName, string firstFileName, int fileCount, string fileTotalSize)
        {
            string uuid = Guid.NewGuid().ToString();
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                var model = new RequestModel
                {
                    Name = guestName,
                    Uuid = uuid,
                    Type = 0,
                    RequestCounts = fileCount,
                    Title = $"来自 {guestName} 的上传请求，是否同意？",
                    Description = $"{firstFileName}\n等共计{fileCount}个文件，大小{fileTotalSize}。",
                    AgreeButtonText = "同意",
                    RefuseButtonText = $"拒绝（{appConfigService.TransmitConfig.RequestTimeout}s）"
                };
                StartRequestTimer(model);
                GuestRequests.Add(model);
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.ShowInTaskbar = true;
            }));
            PlayNotificationSound();
            return uuid;
        }

        public void CreateMessageRequest(string guestName, string message)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                var model = new RequestModel
                {
                    Name = guestName,
                    Type = 1,
                    Title = $"{guestName} 给你发送了一条文本信息。",
                    Description = message,
                    AgreeButtonText = "查看",
                    RefuseButtonText = $"关闭（{appConfigService.TransmitConfig.RequestTimeout}s）"
                };
                StartRequestTimer(model);
                GuestRequests.Add(model);
                Application.Current.MainWindow.Show();
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                Application.Current.MainWindow.ShowInTaskbar = true;
            }));
            PlayNotificationSound();
        }

        public int GetRequestResult(string uuid)
        {
            var model = GuestRequests.FirstOrDefault(r => r.Uuid == uuid);
            if (model == null) { return 2; }
            return model.Status;
        }

        public bool VerifyRequestUuid(string uuid)
        {
            var model = GuestRequests.FirstOrDefault(r => r.Uuid == uuid);
            if (model == null) { return false; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                model.RequestCounts--;
                if (model.RequestCounts <= 0)
                {
                    GuestRequests.Remove(model);
                }
            });
            if (model.Status == 1) { return true; }
            return false;
        }

        public RequestModel? GetRequestModel(string uuid)
        {
            return GuestRequests.FirstOrDefault(r => r.Uuid == uuid);
        }

        private void StartRequestTimer(RequestModel model)
        {
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tag = model;
            timer.Tick += RequestTimerTick;
            timer.Start();
        }

        private void RequestTimerTick(object? sender, EventArgs e)
        {
            var timer = sender as DispatcherTimer;
            if (timer == null) { return; }
            var model = timer.Tag as RequestModel;
            if (model == null) { return; }

            model.TotalSeconds++;
            int remainingSeconds = appConfigService.TransmitConfig.RequestTimeout - model.TotalSeconds;
            // 超过设置的响应时间1分钟后自动清除请求，防止内存泄漏。
            if (model.TotalSeconds > appConfigService.TransmitConfig.RequestTimeout + 60)
            {
                GuestRequests.Remove(model);
                model = null;
                timer.Stop();
                timer.Tick -= RequestTimerTick;
                timer.Tag = null;
                timer = null;
            }
            else
            {
                if (remainingSeconds <= 0)
                {
                    model.Status = 2;
                    model.Visibility = Visibility.Collapsed;
                    model.RefuseButtonText = model.Type == 0 ? "已拒绝" : "关闭";
                }
                else
                {
                    model.RefuseButtonText = model.Type == 0 ? $"拒绝（{remainingSeconds}s）" : model.RefuseButtonText = $"关闭（{remainingSeconds}s）";
                }
            }
        }

        private void PlayNotificationSound()
        {
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("QuickShare.Assets.Sounds.Notification.wav")!)
            {
                if (resourceStream != null && appConfigService.UserConfig.EnableNotificationSound)
                {
                    using (SoundPlayer player = new SoundPlayer(resourceStream))
                    {
                        player.Play();
                    }
                }
            }
        }
    }
}
