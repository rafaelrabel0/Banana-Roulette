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
        private const string KeyLetters = "br_letters";
        private const string KeyPixel = "br_pixel";
        private const string KeyWobble = "br_wobble";
        private const string KeyPlayed = "br_played";
        private const string KeyFur = "br_fur";
        private const string KeyHat = "br_hat";
        private const string KeyGlasses = "br_glasses";
        private const string KeyEarring = "br_earring";
        private const string KeyBomb = "br_bomb";
        private const string KeyEmote = "br_emote";

        public static float MusicVolume { get; private set; }
        public static float SfxVolume { get; private set; }
        public static bool Fullscreen { get; private set; }

        /// <summary>Verdadeiro = joga com W A S D no lugar das setas.</summary>
        public static bool UseLetters { get; private set; }

        /// <summary>Visual retro: imagem em poucas linhas (pixel aparente).</summary>
        public static bool Pixel { get; private set; } = true;

        /// <summary>Falso ate o jogador responder ao aviso de primeira vez (ou terminar o tutorial).</summary>
        public static bool Played { get; private set; }

        // Personalizacao: posicao de cada escolha nas listas de Aparencia.
        public static int Fur { get; private set; }
        public static int Hat { get; private set; }
        public static int Glasses { get; private set; }
        public static int Earring { get; private set; }
        public static int Bomb { get; private set; }
        /// <summary>A gracinha escolhida (posicao em Aparencia.Emotes).</summary>
        public static int Emote { get; private set; }

        /// <summary>Visual retro: tremor dos vertices.</summary>
        public static bool Wobble { get; private set; } = true;

        /// <summary>0 = baixa, 1 = media, 2 = alta.</summary>
        public static int Quality { get; private set; }

        public static void Load()
        {
            MusicVolume = PlayerPrefs.GetFloat(KeyMusic, 0.7f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfx, 0.9f);
            Fullscreen = PlayerPrefs.GetInt(KeyFullscreen, 0) == 1;
            Quality = PlayerPrefs.GetInt(KeyQuality, 2);
            Loc.Current = (Language)PlayerPrefs.GetInt(KeyLanguage, (int)Language.Portuguese);
            UseLetters = PlayerPrefs.GetInt(KeyLetters, 0) == 1;
            Pixel = PlayerPrefs.GetInt(KeyPixel, 1) == 1;
            Wobble = PlayerPrefs.GetInt(KeyWobble, 1) == 1;
            Played = PlayerPrefs.GetInt(KeyPlayed, 0) == 1;
            Fur = PlayerPrefs.GetInt(KeyFur, 0);
            Hat = PlayerPrefs.GetInt(KeyHat, 0);
            Glasses = PlayerPrefs.GetInt(KeyGlasses, 0);
            Earring = PlayerPrefs.GetInt(KeyEarring, 0);
            Bomb = PlayerPrefs.GetInt(KeyBomb, 0);
            Emote = PlayerPrefs.GetInt(KeyEmote, 0);
            VisualRetro.Pixel = Pixel;
            VisualRetro.Tremor = Wobble;

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

        public static void SetPlayed(bool value) { Played = value; Guarda(KeyPlayed, value ? 1 : 0); }
        public static void SetFur(int value) { Fur = value; Guarda(KeyFur, value); }
        public static void SetHat(int value) { Hat = value; Guarda(KeyHat, value); }
        public static void SetGlasses(int value) { Glasses = value; Guarda(KeyGlasses, value); }
        public static void SetEarring(int value) { Earring = value; Guarda(KeyEarring, value); }
        public static void SetBomb(int value) { Bomb = value; Guarda(KeyBomb, value); }
        public static void SetEmote(int value) { Emote = value; Guarda(KeyEmote, value); }

        private static void Guarda(string chave, int valor)
        {
            PlayerPrefs.SetInt(chave, valor);
            PlayerPrefs.Save();
        }

        public static void SetUseLetters(bool value)
        {
            UseLetters = value;
            PlayerPrefs.SetInt(KeyLetters, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetPixel(bool value)
        {
            Pixel = value;
            VisualRetro.Pixel = value;
            PlayerPrefs.SetInt(KeyPixel, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetWobble(bool value)
        {
            Wobble = value;
            VisualRetro.Tremor = value;
            PlayerPrefs.SetInt(KeyWobble, value ? 1 : 0);
            PlayerPrefs.Save();
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
