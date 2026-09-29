using UnityEngine;

namespace BananaRoulette
{
    /// <summary>
    /// Configuracoes do jogador, guardadas em PlayerPrefs.
    ///
    /// O prototipo ainda nao tem audio. Os controles de som existem e persistem
    /// de proposito: quando o som entrar, ja ha onde ligar.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyMusic = "br_music";
        private const string KeySfx = "br_sfx";
        private const string KeyFullscreen = "br_fullscreen";
        private const string KeyQuality = "br_quality";
        private const string KeyLanguage = "br_language";

        public static float MusicVolume { get; private set; }
        public static float SfxVolume { get; private set; }
        public static bool Fullscreen { get; private set; }

        /// <summary>0 = baixa, 1 = media, 2 = alta.</summary>
        public static int Quality { get; private set; }

        public static void Load()
        {
            MusicVolume = PlayerPrefs.GetFloat(KeyMusic, 0.7f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfx, 0.9f);
            Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, 0) == 1;
            Quality = PlayerPrefs.GetInt(KeyQuality, 2);
            Loc.Current = (Language)PlayerPrefs.GetInt(KeyLanguage, (int)Language.Portuguese);

            ApplyAudio();
        }

        public static void SetMusic(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public static void SetSfx(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeySfx, SfxVolume);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        public static void SetFullscreen(bool value)
        {
            Fullscreen = value;
            PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
            PlayerPrefs.Save();

            // No Editor a janela nao vira tela cheia; a preferencia fica guardada
            // para valer na build.
            if (!Application.isEditor)
            {
                Screen.fullScreen = value;
            }
        }

        public static void SetQuality(int level)
        {
            Quality = Mathf.Clamp(level, 0, 2);
            PlayerPrefs.SetInt(KeyQuality, Quality);
            PlayerPrefs.Save();

            int count = QualitySettings.names.Length;
            if (count > 0)
            {
                // Mapeia os tres niveis do menu para a escala do projeto.
                int target = Mathf.RoundToInt(Quality / 2f * (count - 1));
                QualitySettings.SetQualityLevel(target, true);
            }
        }

        public static void SetLanguage(Language language)
        {
            Loc.Current = language;
            PlayerPrefs.SetInt(KeyLanguage, (int)language);
            PlayerPrefs.Save();
        }

        private static void ApplyAudio()
        {
            // Sem fontes de audio ainda: o volume global ja reflete o ajuste.
            AudioListener.volume = SfxVolume;
        }
    }
}
