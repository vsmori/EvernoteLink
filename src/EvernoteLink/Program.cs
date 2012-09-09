using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Net;
using System.Web;
using System.Reflection;
using Kayak;
using Kayak.Http;
using Microsoft.Win32;

namespace EvernoteLink {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args) {
#if DEBUG
            args = new string[] { "run" };
#endif

            RegistryKey rkey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\ENScript.exe", false);
            EvernoteLinkServer.gENScriptPath = rkey.GetValue("Path").ToString() + @"ENScript.exe";

            if (args.Length == 0) {
                //create another instance of this process
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Assembly.GetExecutingAssembly().Location;
                info.Arguments = "run";
                info.UseShellExecute = false;
                info.CreateNoWindow = true;

                Process.Start(info);
                return;
            }

            if (args[0] != "run") return;

#if DEBUG
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.AutoFlush = true;
#endif

            var scheduler = KayakScheduler.Factory.Create(new SchedulerDelegate());
            var server = KayakServer.Factory.CreateHttp(new RequestDelegate(), scheduler);

            using (server.Listen(new IPEndPoint(IPAddress.Any, 8080)))
            {
                // runs scheduler on calling thread. this method will block until
                // someone calls Stop() on the scheduler.
                scheduler.Start();
            }
        }
    }
}
