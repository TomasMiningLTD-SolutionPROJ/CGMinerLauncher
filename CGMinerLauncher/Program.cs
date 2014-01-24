using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Management;
using System.Configuration;

namespace CGMinerLauncher
{
    [Flags]
    enum InternetConnectionState : int
    {
        INTERNET_CONNECTION_MODEM = 0x1,
        INTERNET_CONNECTION_LAN = 0x2,
        INTERNET_CONNECTION_PROXY = 0x4,
        INTERNET_RAS_INSTALLED = 0x10,
        INTERNET_CONNECTION_OFFLINE = 0x20,
        INTERNET_CONNECTION_CONFIGURED = 0x40
    }

    class Program
    {
        [DllImport("WININET", CharSet = CharSet.Auto)]
        static extern bool InternetGetConnectedState(ref InternetConnectionState lpdwFlags, int dwReserved);

        

        static void Main(string[] args)
        {
            string waitForMSIAfterburner = ConfigurationManager.AppSettings["WaitforMSIAfterburner"];
            if (string.Compare(waitForMSIAfterburner, "true", true) == 0)
                WaitForProcess("MSIAfterburner");

            string waitForInternetConnection = ConfigurationManager.AppSettings["WaitforInternetConnection"];
            if (string.Compare(waitForInternetConnection, "true", true) == 0)
                WaitForInternetConnection();

            int delayStart = 0;
            if (int.TryParse(ConfigurationManager.AppSettings["DelayStart"], out delayStart))
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

            //Thread th = MonitorCGMiner();

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

        private static Thread MonitorCGMiner()
        {
            ThreadStart threadStart = new ThreadStart(MonitorAPI);
            Thread th = new Thread(threadStart);
            th.Start();
            return th;
        }

        private static void MonitorAPI()
        {
            ApiWorker worker = new ApiWorker("127.0.0.1", 4028);
            while (true)
            {
                Console.Clear();

                Console.WriteLine(worker.Request("summary"));

                Console.WriteLine(worker.Request("devs"));

                Thread.Sleep(1000);
            }
        }

        private static bool IsAbleToConnect()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            ApiWorker worker = new ApiWorker("127.0.0.1", 4028);

            while (true)
            {
                string ret = worker.Request("summary");
                if (ret != null)
                    break;
                if (sw.ElapsedMilliseconds > 60000)
                    return false;
                Thread.Sleep(1000);
                Console.Write(".");
            }
            Console.WriteLine("");
            return true;
        }

        private static void Parse(string data)
        {
            if (data != null)
            {
                string[] ary = data.Split('|');

                foreach (string s in ary)
                {
                    if (s.Length > 0)
                    {
                        Console.WriteLine("---");
                        string[] ary2 = s.Split(',');
                        foreach (string s2 in ary2)
                        {
                            Console.WriteLine(s2);
                        }
                    }
                }
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

            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.WorkingDirectory = workingDirectory;
            startInfo.FileName = @"cgminer.exe";
            startInfo.Arguments = arguments;
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
