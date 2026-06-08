namespace PlayNiteWidget
{
    using Microsoft.Gaming.XboxGameBar;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.Storage;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    internal sealed partial class App : Application
    {
        /// <summary>
        /// Defines the uriToLaunch.
        /// </summary>
        internal static string uriToLaunch = @"playnitefullscreen://";

        /// <summary>
        /// Defines the appLaunched.
        /// </summary>
        private bool appLaunched = false;

        /// <summary>
        /// Defines the playniteWidget.
        /// </summary>
        private XboxGameBarWidget playniteWidget = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
        }

        /// <summary>
        /// The application entry point.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            Application.Start(_ => new App());
        }

        /// <summary>
        /// The OnActivated.
        /// </summary>
        /// <param name="args">The args<see cref="IActivatedEventArgs"/>.</param>
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await LogAsync($"Activated: kind={args.Kind}");
            XboxGameBarWidgetActivatedEventArgs widgetArgs = null;
            if (args.Kind == ActivationKind.Protocol)
            {
                var protocolArgs = args as IProtocolActivatedEventArgs;
                if (protocolArgs != null)
                {
                    string scheme = protocolArgs.Uri.Scheme;
                    await LogAsync($"Protocol activation: scheme={scheme}, uri={protocolArgs.Uri}");
                    if (scheme.Equals("ms-gamebarwidget"))
                    {
                        widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
                        await LogAsync($"Game Bar args cast: {widgetArgs != null}");
                    }
                }
                else
                {
                    await LogAsync("Protocol activation args were not available");
                }
            }
            if (widgetArgs != null)
            {
                await LogAsync($"Game Bar activation: isLaunch={widgetArgs.IsLaunchActivation}");
                if (widgetArgs.IsLaunchActivation)
                {
                    Frame rootFrame = new Frame();
                    Window.Current.Content = rootFrame;

                    // Create Game Bar widget object which bootstraps the connection with Game Bar
                    playniteWidget = new XboxGameBarWidget(
                        widgetArgs,
                        Window.Current.CoreWindow,
                        rootFrame);
                    playniteWidget.WindowStateChanged += Widget1_WindowStateChanged;
                }
            }

            Window.Current.Activate();

            // Open Playnite straight away
            OpenPlaynite();
        }

        /// <summary>
        /// This is triggered on a window state change and auto-closes the widget when it gets restored.
        /// </summary>
        /// <param name="sender">The sender<see cref="XboxGameBarWidget"/>.</param>
        /// <param name="args">The args<see cref="object"/>.</param>
        private void Widget1_WindowStateChanged(XboxGameBarWidget sender, object args)
        {
            if (playniteWidget.WindowState == XboxGameBarWidgetWindowState.Restored)
            {
                QueueForWidgetClose();
            }
        }

        /// <summary>
        /// Queues the widget for closing once playnite has opened.
        /// </summary>
        private void QueueForWidgetClose()
        {
            // If app isn't launched, try again in 1 second
            // Otherwise close the widget
            if (!appLaunched)
            {
                Task.Delay(new TimeSpan(0, 0, 1)).ContinueWith(o => {
                    QueueForWidgetClose();
                });
            } else
            {
                appLaunched = false;
                playniteWidget.Close();
            }
        }

        /// <summary>
        /// Opens Playnite.
        /// </summary>
        private async void OpenPlaynite()
        {
            Uri uri = new Uri(uriToLaunch);
            bool firstLaunch = await Windows.System.Launcher.LaunchUriAsync(uri); // Call once to start the app
            await LogAsync($"First LaunchUriAsync({uri}) result: {firstLaunch}");

            // Call again to bring to focus
            await Task.Delay(1000);
            bool secondLaunch = await Windows.System.Launcher.LaunchUriAsync(uri);
            await LogAsync($"Second LaunchUriAsync({uri}) result: {secondLaunch}");

            if (!firstLaunch && !secondLaunch)
            {
                try
                {
                    await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    await LogAsync("FullTrustProcessLauncher result: launched");
                }
                catch (Exception ex)
                {
                    await LogAsync($"FullTrustProcessLauncher exception: {ex.GetType().FullName}: {ex.Message}");
                }
            }

            appLaunched = true;

            await Task.Delay(500);
            if (playniteWidget != null)
            {
                playniteWidget.Close();
            }
        }

        /// <summary>
        /// Writes simple diagnostics to the package LocalState folder.
        /// </summary>
        /// <param name="message">The message to write.</param>
        private static async Task LogAsync(string message)
        {
            try
            {
                StorageFile logFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    "PlayniteXboxWidget.log",
                    CreationCollisionOption.OpenIfExists);
                await FileIO.AppendTextAsync(
                    logFile,
                    $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
            }
            catch
            {
                Debug.WriteLine(message);
            }
        }
    }
}
