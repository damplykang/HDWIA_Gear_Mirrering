using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WIA_ViewerProgram
{
    public class _Directory
    {
        public string FTP { get; set; }
    }
    internal class DirectoryManager
    {
        public _Directory JsonDirectorydata;
        public string ftpdirectory;


        public DirectoryManager()
        {
            DirectoryJsonLoad();
            ftpdirectory = JsonDirectorydata.FTP;
        }
        public void SetFtpDirectory(string Path)
        {
            ftpdirectory = Path;
            JsonDirectorydata.FTP = Path;
            DirectoryJsonSave();
        }
        private void DirectoryJsonLoad()
        {
            const string directoryJsonPath = "./Directory.json";
            if (!File.Exists(directoryJsonPath))
            {
                JsonDirectorydata = new _Directory { FTP = string.Empty };
                DirectoryJsonSave();
                return;
            }

            string json = File.ReadAllText(directoryJsonPath);
            JsonDirectorydata = JsonConvert.DeserializeObject<_Directory>(json) ?? new _Directory { FTP = string.Empty };
        }

        private void DirectoryJsonSave()
        {
            {
                string json = System.Text.Json.JsonSerializer.Serialize(JsonDirectorydata, new JsonSerializerOptions
                {
                    WriteIndented = true   // 보기 좋게 들여쓰기
                });

                File.WriteAllText("./Directory.json", json);
            }
        }
    }
}
