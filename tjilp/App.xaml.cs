using System;
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
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const bool AUTO_UPDATE = false;
        public const ushort APP_VERSION = 0035;

        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            new System.Threading.Thread(UpdateApp).Start();
        }

        public static void UpdateApp()
        {
            try
            {
                Directory.GetFiles(Directory.GetCurrentDirectory()).Where(x => x.EndsWith("_tjilpUpdate.bat") || x.EndsWith("_tjilpNew.exe")).ForEach(f => File.Delete(f));

                using (var client = new WebClient())
                    if (AUTO_UPDATE && Convert.ToUInt32(client.DownloadString("http://tjilp.parseapp.com/version.txt")) > APP_VERSION)
                    {
                        var rndm = new Random();

                        var batFile = rndm.Next().ToString() + "_tjilpUpdate.bat";
                        var tjilpName = rndm.Next().ToString() + "_tjilpNew.exe";
                        var currentName = AppDomain.CurrentDomain.FriendlyName;

                        client.DownloadFileAsync(new Uri("http://tjilp.parseapp.com/tjilp.exe"), tjilpName);

                        File.WriteAllText(batFile,
                                          "@echo off" + Environment.NewLine +                                             // Hide output.
                                          "timeout /t 1 /NOBREAK" + Environment.NewLine +                                // Wait 1 second.
                                          "del /F " + currentName + Environment.NewLine +                               // Delete old tjilp.
                                          String.Format("ren {0} {1}", tjilpName, currentName) + Environment.NewLine + // Rename new tjip to name of old tjilp.
                                          "start " + currentName);                                                    // Start tjilp.

                        client.DownloadFileCompleted += (s, e) =>
                        {
                            Process.Start(new ProcessStartInfo(batFile) { WindowStyle = ProcessWindowStyle.Hidden });

                            Environment.Exit(0);
                        };
                    }
            }
            catch { /* We just want this to never be the thing that lets the program crash. */ }
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var dllName = args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");

            dllName = dllName.Replace(".", "_");

            if (dllName.EndsWith("_resources")) return null;

            var rm = new ResourceManager(GetType().Namespace + ".Properties.Resources", Assembly.GetExecutingAssembly());

            var bytes = (byte[])rm.GetObject(dllName);

            return Assembly.Load(bytes);
        }
    }
}
