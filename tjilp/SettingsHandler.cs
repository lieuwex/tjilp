using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace tjilp
{
    public class Settings
    {
        readonly Dictionary<string, string> RawSettings = new Dictionary<string, string>();
        public Settings(string path) { this.Path = path; }
        string Path { get; set; }
        public double FontSize
        {
            get
            {
                this.readSettings();
                return Convert.ToDouble(this.RawSettings["font-size"]);
            }
            set
            {
                this.RawSettings["font-size"] = value.ToString();
                this.saveSettings();
            }
        }
        public bool OnTop
        {
            get
            {
                this.readSettings();
                return this.RawSettings["on-top"] == "1";
            }
            set
            {
                this.RawSettings["on-top"] = value ? "1" : "0";
                this.saveSettings();
            }
        }

        void saveSettings()
        {
            var sb = new StringBuilder();
            foreach(var pair in this.RawSettings)
            {
                sb.Append(pair.Key + "=" + pair.Value);
                sb.Append(Environment.NewLine);
            }
            File.WriteAllText(this.Path, sb.ToString());
        }

        void readSettings()
        {
            if (!File.Exists(this.Path))
                File.WriteAllText(this.Path, "font-size=22" + Environment.NewLine + "on-top=1");

            foreach(var pair in File.ReadAllLines(this.Path)
                                    .Where(l     => !l.StartsWith("#"))
                                    .Select(line => line.Split('=')))
                this.RawSettings[pair[0].Trim().ToLower()] = pair[1].Trim();
        }
    }
}