using UnityEngine;

namespace DigitalPlague.UI
{
    /// <summary>Local presentation preferences. No simulation state is stored here.</summary>
    public sealed class GamePreferences
    {
        public bool ShowConnections { get; set; } = true;
        public bool HighContrast { get; set; }
        public float ZoomSensitivity { get; set; } = 1;

        public static GamePreferences Load()
        {
            return new GamePreferences
            {
                ShowConnections = PlayerPrefs.GetInt("DigitalPlague.ShowConnections", 1) == 1,
                HighContrast = PlayerPrefs.GetInt("DigitalPlague.HighContrast", 0) == 1,
                ZoomSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("DigitalPlague.ZoomSensitivity", 1), .5f, 1.5f)
            };
        }

        public void Save()
        {
            PlayerPrefs.SetInt("DigitalPlague.ShowConnections", ShowConnections ? 1 : 0);
            PlayerPrefs.SetInt("DigitalPlague.HighContrast", HighContrast ? 1 : 0);
            PlayerPrefs.SetFloat("DigitalPlague.ZoomSensitivity", ZoomSensitivity);
            PlayerPrefs.Save();
        }
    }
}
