using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Windows;

namespace tjilp
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        const bool AUTO_UPDATE = true;
        public const ushort APP_VERSION = 0044;
        public static readonly ParseClient ParseClient = new ParseClient("xxx", "xxx");
        public static readonly List<string> ChangeLog = new List<string>();

        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += this.CurrentDomain_AssemblyResolve;
#if DEBUG
#else
            this.Startup += (s, e) => { ParseClient.TrackAppOpened(); };
            UpdateApp();
#endif
        }

        public static async void UpdateApp()
        {
            try
            {
                Directory.GetFiles(Directory.GetCurrentDirectory()).Where(x => x.EndsWith("_tjilpUpdate.bat") || x.EndsWith("_tjilpNew.exe")).ForEach(File.Delete);
                using(var client = new WebClient())
                {
                    if (AUTO_UPDATE && Convert.ToUInt32(client.DownloadString("http://tjilp.parseapp.com/version.txt")) > APP_VERSION)
                    {
                        var newLine = Environment.NewLine;
                        var rndm = new Random();

                        var batFile = rndm.Next() + "_tjilpUpdate.bat";
                        var tjilpName = rndm.Next() + "_tjilpNew.exe";
                        var currentName = AppDomain.CurrentDomain.FriendlyName;

                        var downloadTask = client.DownloadFileTaskAsync(new Uri("http://tjilp.parseapp.com/tjilp.exe"), tjilpName);
                        File.WriteAllText(batFile, "@echo off" + newLine +                                             // Hide output.
                                                   "timeout /t 1 /NOBREAK" + newLine +                                // Wait 1 second.
                                                   "del /F " + currentName + newLine +                               // Delete old tjilp.
                                                   String.Format("ren {0} {1}", tjilpName, currentName) + newLine + // Rename new tjip to name of old tjilp.
                                                   "start " + currentName);                                        // Start tjilp.
                        await downloadTask;
                        Process.Start(new ProcessStartInfo(batFile)
                        {
                            WindowStyle = ProcessWindowStyle.Hidden
                        });
                        Environment.Exit(0);
                    }
                }
            }
            catch { /* We just want this to never be the thing that lets the program crash. */ }
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var dllName = args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            var rm = new ResourceManager(this.GetType().Namespace + ".Properties.Resources", Assembly.GetExecutingAssembly());
            var bytes = (byte[])rm.GetObject(dllName);
            return Assembly.Load(bytes);
        }
    }
}
