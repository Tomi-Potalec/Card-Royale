using System;
using System.IO;
using Newtonsoft.Json;

namespace Card_Royale
{
    public class SettingsManager
    {
        // Singleton instanca
        private static SettingsManager _instance;
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SettingsManager();
                return _instance;
            }
        }

        // Privatni konstruktor s zadanim vrijednostima
        private SettingsManager()
        {
            Theme = "Light";
            SelectMusic = "Jazz Time";
            MusicVolume = 50;      // 🎵 Music volume
            SfxVolume = 70;        // 🔊 Sound effects volume (NEW)
            CardBack = "Black";
        }

        // Properties
        public string Theme { get; set; }
        public string SelectMusic { get; set; }

        public int MusicVolume { get; set; }   // 🎵 Music volume
        public int SfxVolume { get; set; }     // 🔊 NEW: Sound effects volume

        public string CardBack { get; set; }

        // File path
        private string GetSettingsFilePath()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Card Royale"
            );

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, "Settings.json");
        }

        // Spremi postavke u JSON
        public void Save()
        {
            try
            {
                string filePath = GetSettingsFilePath();
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save settings: " + ex.Message);
            }
        }

        // Učitaj postavke iz JSON-a
        public void Load()
        {
            try
            {
                string filePath = GetSettingsFilePath();

                if (!File.Exists(filePath))
                {
                    Save();
                    return;
                }

                string json = File.ReadAllText(filePath);
                var loaded = JsonConvert.DeserializeObject<SettingsManager>(json);

                if (loaded != null)
                {
                    Theme = loaded.Theme;
                    SelectMusic = loaded.SelectMusic;

                    MusicVolume = loaded.MusicVolume;
                    SfxVolume = loaded.SfxVolume;

                    
                    CardBack = loaded.CardBack;
                }
                else
                {
                    Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load settings: " + ex.Message);
            }
        }
    }
}
