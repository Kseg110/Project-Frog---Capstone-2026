using UnityEngine;
using System.Collections.Generic;

public static class LanguageManager
{
    public static string CurrentLanguage;

    private static readonly Dictionary<string, Dictionary<string, string>> translations =
        new()
        {
            { "start_button", new Dictionary<string, string> {
                { "fr", "Nouvelle Partie" },
                { "en", "New Game" },
                { "es", "Nuevo Juego" },
                { "zh", "???" }
            }},
            { "load_button", new Dictionary<string, string> {
                { "fr", "Charger Partie" },
                { "en", "Load Game" },
                { "es", "Cargar Juego" },
                { "zh", "????" }
            }},
            { "continue_button", new Dictionary<string, string> {
                { "fr", "Continuer" },
                { "en", "Continue" },
                { "es", "Continuar" },
                { "zh", "??" }
            }},
            { "settings_button", new Dictionary<string, string> {
                { "fr", "Paramètres" },
                { "en", "Settings" },
                { "es", "Configuración" },
                { "zh", "??" }
            }},
            { "audio_button", new Dictionary<string, string> {
                { "fr", "Audio" },
                { "en", "Audio" },
                { "es", "Audio" },
                { "zh", "??" }
            }},
            { "video_button", new Dictionary<string, string> {
                { "fr", "Vidéo" },
                { "en", "Video" },
                { "es", "Vídeo" },
                { "zh", "??" }
            }},
            { "language_button", new Dictionary<string, string> {
                { "fr", "Langue" },
                { "en", "Language" },
                { "es", "Idioma" },
                { "zh", "??" }
            }},
            { "controls_button", new Dictionary<string, string> {
                { "fr", "Contrôles" },
                { "en", "Controls" },
                { "es", "Controles" },
                { "zh", "??" }
            }},
            { "exit_button", new Dictionary<string, string> {
                { "fr", "Quitter" },
                { "en", "Exit Game" },
                { "es", "Salir del Juego" },
                { "zh", "????" }
            }}
        };

    public static void Initialize()
    {
        // Verifie if a language is already set
        if (PlayerPrefs.HasKey("language"))
        {
            CurrentLanguage = PlayerPrefs.GetString("language");
        }
        else
        {
            // Define default language based on system language
            SystemLanguage sysLang = Application.systemLanguage;
            CurrentLanguage = sysLang switch
            {
                SystemLanguage.French => "fr",
                SystemLanguage.Spanish => "es",
                SystemLanguage.Chinese or SystemLanguage.ChineseSimplified or SystemLanguage.ChineseTraditional => "zh",
                _ => "en",
            };

            PlayerPrefs.SetString("language", CurrentLanguage);
            PlayerPrefs.Save();
        }
    }


    public static string GetTranslation(string key)
    {
        if (translations.ContainsKey(key) && translations[key].ContainsKey(CurrentLanguage))
            return translations[key][CurrentLanguage];
        return key; // fallback 
    }

    public static void SetLanguage(string lang)
    {
        CurrentLanguage = lang;
        PlayerPrefs.SetString("language", lang);
        PlayerPrefs.Save();
    }
}