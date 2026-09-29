using System;
using System.Collections.Generic;
using UnityEngine;

namespace BananaRoulette
{
    public enum Language
    {
        Portuguese = 0,
        English = 1
    }

    /// <summary>
    /// Textos do jogo em portugues e ingles. O jogo tem pouco texto, entao um
    /// dicionario na mao resolve sem pesar nada.
    /// </summary>
    public static class Loc
    {
        public static event Action OnLanguageChanged;

        private static Language _current = Language.Portuguese;

        public static Language Current
        {
            get { return _current; }
            set
            {
                if (_current == value) return;
                _current = value;
                if (OnLanguageChanged != null) OnLanguageChanged();
            }
        }

        public static string Get(string key)
        {
            string[] entry;
            if (!Table.TryGetValue(key, out entry)) return key;
            int index = (int)_current;
            return index < entry.Length ? entry[index] : entry[0];
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        // [0] = portugues, [1] = ingles
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // ---- Menu ----
            { "menu.subtitle",   new[] { "Ape Games apresenta", "Ape Games presents" } },
            { "menu.play",       new[] { "JOGAR", "PLAY" } },
            { "menu.tutorial",   new[] { "TUTORIAL", "TUTORIAL" } },
            { "menu.settings",   new[] { "CONFIGURAÇÕES", "SETTINGS" } },
            { "menu.tagline",    new[] { "O último macaco na roda vence.",
                                         "Last monkey standing wins." } },

            // ---- Configuracoes ----
            { "set.title",       new[] { "CONFIGURAÇÕES", "SETTINGS" } },
            { "set.audio",       new[] { "SOM", "AUDIO" } },
            { "set.music",       new[] { "Música", "Music" } },
            { "set.sfx",         new[] { "Efeitos", "Sound effects" } },
            { "set.nosound",     new[] { "O protótipo ainda não tem som. Os controles ficam guardados.",
                                         "The prototype has no sound yet. These settings are saved anyway." } },
            { "set.video",       new[] { "VÍDEO", "VIDEO" } },
            { "set.fullscreen",  new[] { "Tela cheia", "Fullscreen" } },
            { "set.quality",     new[] { "Qualidade", "Quality" } },
            { "set.quality.low", new[] { "Baixa", "Low" } },
            { "set.quality.mid", new[] { "Média", "Medium" } },
            { "set.quality.high",new[] { "Alta", "High" } },
            { "set.language",    new[] { "IDIOMA", "LANGUAGE" } },
            { "set.pt",          new[] { "Português", "Portuguese" } },
            { "set.en",          new[] { "Inglês", "English" } },
            { "set.controls",    new[] { "CONTROLES", "CONTROLS" } },
            { "set.controls.seq",new[] { "Digitar a sequência", "Type the sequence" } },
            { "set.controls.pass", new[] { "Escolher o lado e arremessar", "Pick a side and throw" } },
            { "set.controls.note", new[] { "Só as setas. Nada mais.", "Arrow keys only. Nothing else." } },
            { "set.on",          new[] { "LIGADO", "ON" } },
            { "set.off",         new[] { "DESLIGADO", "OFF" } },
            { "set.back",        new[] { "VOLTAR", "BACK" } },

            // ---- Partida ----
            { "game.monkeys",    new[] { "MACACOS: {0} / {1}", "MONKEYS: {0} / {1}" } },
            { "game.lit",        new[] { "A BANANA ESTÁ ACESA", "THE BANANA IS LIT" } },
            { "game.yourturn",   new[] { "SUA VEZ", "YOUR TURN" } },
            { "game.holder",     new[] { "MACACO {0} ESTÁ COM A BANANA", "MONKEY {0} HAS THE BANANA" } },
            { "game.chooseside", new[] { "ESCOLHA O LADO", "PICK A SIDE" } },
            { "game.memory",     new[] { "DE MEMÓRIA", "FROM MEMORY" } },
            { "game.youexploded",new[] { "VOCÊ EXPLODIU", "YOU EXPLODED" } },
            { "game.exploded",   new[] { "MACACO {0} EXPLODIU", "MONKEY {0} EXPLODED" } },
            { "game.yousurvived",new[] { "VOCÊ SOBREVIVEU", "YOU SURVIVED" } },
            { "game.lastmonkey", new[] { "ÚLTIMO MACACO NA RODA", "LAST MONKEY STANDING" } },
            { "game.monkeywon",  new[] { "MACACO {0} VENCEU", "MONKEY {0} WON" } },
            { "game.again",      new[] { "ENTER para jogar de novo  ·  ESC para o menu",
                                         "ENTER to play again  ·  ESC for menu" } },
            { "game.esc",        new[] { "ESC: menu", "ESC: menu" } },

            // ---- Tutorial ----
            { "tut.title",       new[] { "TUTORIAL", "TUTORIAL" } },
            { "tut.skip",        new[] { "ESC: pular", "ESC: skip" } },
            { "tut.next",        new[] { "ESPAÇO para continuar", "SPACE to continue" } },

            { "tut.1", new[] {
                "Você é o macaco amarelo, na frente.",
                "You are the yellow monkey, at the front." } },
            { "tut.2", new[] {
                "Essa banana é uma dinamite. O pavio está aceso e ninguém sabe quanto falta.",
                "That banana is dynamite. The fuse is lit and nobody knows how much is left." } },
            { "tut.3", new[] {
                "Para passar a banana adiante, digite a sequência de setas. O mais rápido que você conseguir.",
                "To pass the banana on, type the arrow sequence. As fast as you can." } },
            { "tut.4", new[] {
                "Sua vez. Digite as setas.",
                "Your turn. Type the arrows." } },
            { "tut.5", new[] {
                "Agora escolha para quem jogar: seta esquerda ou direita.",
                "Now pick who gets it: left or right arrow." } },
            { "tut.6", new[] {
                "Você se livrou. Mas o pavio não parou de queimar em nenhum momento.",
                "You got rid of it. But the fuse never stopped burning." } },
            { "tut.7", new[] {
                "Quem estiver segurando quando o pavio acabar, explode.",
                "Whoever is holding it when the fuse runs out explodes." } },
            { "tut.8", new[] {
                "Um macaco a menos. Pavio novo aceso, e mais curto que o anterior.",
                "One monkey down. A new fuse is lit, shorter than the last one." } },
            { "tut.9", new[] {
                "Agora o mais importante. Erre de propósito: aperte uma seta ERRADA.",
                "Now the important part. Miss on purpose: press a WRONG arrow." } },
            { "tut.10", new[] {
                "Errou. A banana escapou da mão, a sequência recomeçou do zero e o pavio queimou um pedaço extra.",
                "You missed. The banana slipped, the sequence restarted and the fuse burned an extra chunk." } },
            { "tut.11", new[] {
                "É por isso que pressa e calma brigam: errar custa mais caro que ir devagar.",
                "That is why speed fights calm: missing costs more than going slow." } },
            { "tut.12", new[] {
                "Agora acerte a sequência inteira e passe a banana.",
                "Now nail the whole sequence and pass the banana on." } },
            { "tut.13", new[] {
                "É isso. Último macaco na roda vence.",
                "That is it. Last monkey standing wins." } },
            { "tut.done", new[] { "JOGAR AGORA", "PLAY NOW" } },
            { "tut.menu", new[] { "VOLTAR AO MENU", "BACK TO MENU" } },
            { "tut.wrongonpurpose", new[] {
                "Essa estava certa. Aperte uma seta ERRADA de propósito.",
                "That one was right. Press a WRONG arrow on purpose." } },
            { "tut.fuseburn", new[] { "PAVIO QUEIMOU", "FUSE BURNED" } }
        };
    }
}
