namespace PlayniteLauncher
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    internal static class Program
    {
        private const string PlayniteFullscreenUri = "playnitefullscreen://";

        private static int Main(string[] args)
        {
            Log("Helper started");

            bool firstLaunch = LaunchProtocol();
            Log($"First shell launch result: {firstLaunch}");

            Thread.Sleep(1000);

            bool secondLaunch = LaunchProtocol();
            Log($"Second shell launch result: {secondLaunch}");

            return firstLaunch || secondLaunch ? 0 : 1;
        }

        private static bool LaunchProtocol()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = PlayniteFullscreenUri,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Log($"Shell launch exception: {ex.GetType().FullName}: {ex.Message}");
                return false;
            }
        }

        private static void Log(string message)
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logDir = Path.Combine(
                    localAppData,
                    "Packages",
                    "PlayniteXboxWidget_8wekyb3d8bbwe",
                    "LocalState");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(
                    Path.Combine(logDir, "PlayniteLauncher.log"),
                    $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
