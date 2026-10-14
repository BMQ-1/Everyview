using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EverythingModern
{
    public partial class SplashScreenWindow : Window
    {
        public bool IsForceOpened { get; private set; }
        private bool _isClosing;

        public SplashScreenWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch { }
            }
        }

        public event Action? SkipRequested;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsForceOpened = true;
                SkipRequested?.Invoke();
                FadeOutAndClose();
            }
            base.OnKeyDown(e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsForceOpened = true;
            SkipRequested?.Invoke();
            FadeOutAndClose();
        }

        private void OpenNowButton_Click(object sender, RoutedEventArgs e)
        {
            IsForceOpened = true;
            SkipRequested?.Invoke();
            FadeOutAndClose();
        }

        public void FadeOutAndClose()
        {
            if (_isClosing) return;
            _isClosing = true;

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(FadeOutAndClose);
                return;
            }

            var fadeAnim = new DoubleAnimation
            {
                From = Opacity,
                To = 0.0,
                Duration = new Duration(TimeSpan.FromMilliseconds(160)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var scaleAnim = new DoubleAnimation
            {
                From = WindowScale.ScaleX,
                To = 0.96,
                Duration = new Duration(TimeSpan.FromMilliseconds(160)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            fadeAnim.Completed += (s, e) =>
            {
                try { Close(); } catch { }
            };

            WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            BeginAnimation(OpacityProperty, fadeAnim);
        }

        public void UpdateStatus(string status, string? detail = null, bool isComplete = false)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateStatus(status, detail, isComplete));
                return;
            }

            StatusTextBlock.Text = status;
            if (!string.IsNullOrEmpty(detail))
            {
                DetailTextBlock.Text = detail;
            }

            if (isComplete)
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 100;
                PulseIndicator.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Emerald Green
            }
        }
    }
}
