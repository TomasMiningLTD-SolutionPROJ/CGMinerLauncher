using System;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CGMinerLauncher
{
    [Flags]
    internal enum InternetConnectionState : int
    {
        INTERNET_CONNECTION_MODEM = 0x1,
        INTERNET_CONNECTION_LAN = 0x2,
        INTERNET_CONNECTION_PROXY = 0x4,
        INTERNET_RAS_INSTALLED = 0x10,
        INTERNET_CONNECTION_OFFLINE = 0x20,
        INTERNET_CONNECTION_CONFIGURED = 0x40
    }

    internal class Program
    {
        [DllImport("WININET", CharSet = CharSet.Auto)]
        private static extern bool InternetGetConnectedState(ref InternetConnectionState lpdwFlags, int dwReserved);

        private static void Main(string[] args)
        {
            string waitForMSIAfterburner = ConfigurationManager.AppSettings["WaitforMSIAfterburner"];
            if (string.Compare(waitForMSIAfterburner, "true", true) == 0)
                WaitForProcess("MSIAfterburner");

            string waitForInternetConnection = ConfigurationManager.AppSettings["WaitforInternetConnection"];
            if (string.Compare(waitForInternetConnection, "true", true) == 0)
                WaitForInternetConnection();

            if (int.TryParse(ConfigurationManager.AppSettings["DelayStart"], out int delayStart))
            {
                Console.WriteLine("Delay Start " + delayStart);
                Thread.Sleep(delayStart);
            }

            Process process = StartCGMiner();

            //if (!IsAbleToConnect())
            //{
            //    Restart();
            //    return;
            //}

            process.WaitForExit();
            Restart();
            //th.Abort();
        }

        private static void WaitForProcess(string process)
        {
            Console.WriteLine("Waiting for " + process);
            while (true)
            {
                Process[] proc = Process.GetProcessesByName(process);
                if (proc.Length > 0)
                {
                    Console.WriteLine("Found " + process);
                    break;
                }
                Thread.Sleep(1000);
            }
        }

        private static void WaitForInternetConnection()
        {
            Console.WriteLine("Waiting for internet connection");
            while (true)
            {
                InternetConnectionState flags = 0;
                if (InternetGetConnectedState(ref flags, 0))
                {
                    Console.WriteLine("Connected to internet.");
                    break;
                }
                Thread.Sleep(1000);
            }
        }

        private static Process StartCGMiner()
        {
            string workingDirectory = ConfigurationManager.AppSettings["CGMinerWorkingDirectory"];
            string arguments = ConfigurationManager.AppSettings["CGMinerArguments"];

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WorkingDirectory = workingDirectory,
                FileName = @"cgminer.exe",
                Arguments = arguments
            };
            Process process = Process.Start(startInfo);
            return process;
        }

        public static void Restart()
        {
            Console.WriteLine("RESTART!");
            RestartRelease();
        }

        public static void RestartRelease()
        {
            Process.Start("shutdown.exe", "/r /f /t 0");
        }
    }
}