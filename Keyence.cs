using System.IO;
using System.Text.Json;

namespace WIA_ViewerProgram
{
    internal sealed class KeyenceSettingJson
    {
        public string Ip { get; set; } = "";
        public int PortNumber { get; set; }
    }

    public class Keyence
    {
        private const string JsonFileName = "KeyenceSetting.json";

        public string Ip { get; set; } = "";
        public int PortNumber { get; set; }

        private static string JsonPath => Path.Combine(AppContext.BaseDirectory, JsonFileName);

        public void LoadFromJson()
        {
            try
            {
                if (!File.Exists(JsonPath))
                {
                    Ip = "";
                    PortNumber = 0;
                    SaveToJson();
                    return;
                }

                string json = File.ReadAllText(JsonPath);
                var data = JsonSerializer.Deserialize<KeyenceSettingJson>(json);
                if (data == null)
                {
                    Ip = "";
                    PortNumber = 0;
                    return;
                }

                Ip = data.Ip ?? "";
                PortNumber = data.PortNumber;
            }
            catch
            {
                Ip = "";
                PortNumber = 0;
            }
        }

        public void SaveToJson()
        {
            var data = new KeyenceSettingJson { Ip = Ip, PortNumber = PortNumber };
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(JsonPath, json);
        }
    }
}
