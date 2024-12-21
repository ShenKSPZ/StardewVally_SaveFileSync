using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewVally_SaveFileSync
{
    [Serializable]
    public class Config
    {
        public string URL = "http://123.57.229.186:8082/";
        public bool isAutoUploadSave = true;
        public string SaveFileNameForAutoUpload = "";
        public string SaveFileDefaultCharacter = "";
    }

    public static class GlobalConfig
    {
        public static Config current
        {
            get
            {
                if (m_cur == null)
                    LoadFromFile();

                return m_cur;
            }
            private set
            {
                m_cur = value;
            }
        }
        private static Config m_cur = null;

        public static readonly string configPath = Path.Combine(Environment.CurrentDirectory, "config.json");

        public static void LoadFromFile()
        {
            if(File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath, Encoding.UTF8);
                current = JsonConvert.DeserializeObject<Config>(json);
            }
            else
            {
                current = new Config();
                File.WriteAllText(configPath, JsonConvert.SerializeObject(current));
            }
        }

        public static void SaveConfig()
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(current));
        }
    }
}
