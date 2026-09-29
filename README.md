# Banana Roulette — protótipo jogável

Protótipo (greybox) do **Banana Roulette**, da [Ape Games](https://github.com/rafaelrabel0).
Trabalho da matéria Desenvolvimento de Jogos Digitais 2.

> Macacos em roda passam uma banana de dinamite de mão em mão. Para passar, é preciso
> digitar uma sequência de setas. Quem estiver segurando quando o pavio acabar, explode.
> Último macaco na roda vence.

## Como abrir

1. **Unity 6000.3.24f1** (Unity 6.3 LTS). Instale essa versão pelo Unity Hub — outra
   versão pode pedir upgrade do projeto.
2. Clone este repositório:
   ```
   git clone https://github.com/rafaelrabel0/Banana-Roulette.git
   ```
3. No Unity Hub: **Add → Add project from disk** e aponte para a pasta clonada.
4. Abra a cena **`Assets/Scenes/Prototipo.unity`**.
5. Play.

> A primeira abertura demora: o Unity precisa reimportar tudo e gerar a pasta `Library/`,
> que não vai no repositório de propósito.

## Como jogar

| Tecla | O que faz |
|-------|-----------|
| `←` `↑` `→` `↓` | Digita a sequência que aparece na tela |
| `←` `→` | Depois da sequência, escolhe para quem jogar a banana |
| `ESC` | Volta ao menu |
| `ENTER` | Reinicia, no fim da partida |

São 5 macacos: você (o amarelo, marcado com um anel no chão) e 4 bots.

**Digite o mais rápido que conseguir.** Errar não custa só o tempo perdido: queima um
pedaço do pavio de uma vez. O pavio tem duração secreta — você nunca sabe quanto falta,
só vê o quanto já queimou, na banana e na barra do topo da tela.

A cada macaco eliminado a sequência ganha uma seta. Na rodada final, com 2 macacos, as
teclas somem da tela e vira memória.

**Comece pelo Tutorial** se for a primeira vez: ele explica a mecânica em uma partida
guiada, incluindo o preço do erro.

## Ajustar o jogo

Todos os números vivem em **`Assets/Settings/TuningConfig.asset`**: quantidade de macacos,
duração do pavio, velocidade e erro dos bots, penalidade por errar.

Por ser um ScriptableObject, **as mudanças feitas durante o Play mode continuam valendo
depois de sair dele**. É a forma de calibrar o jogo sem recompilar.

Para deixar as partidas mais curtas na apresentação, baixe `monkeyCount` para 4.

## Como o projeto é montado

A cena tem **um único objeto** (`Bootstrap`). Chão, câmera, luzes, macacos, banana, HUD e
menus são criados por código no `Start`. Não há prefab nem asset importado: todo o visual
usa primitivas do Unity e sprites gerados em `Texture2D`.

Isso é de propósito — evita conflito de merge na cena e permite levar o protótipo inteiro
copiando `Assets/Scripts/`.

```
GameManager (máquina de estados, em corrotinas)
  Setup -> RoundStart -> Holding -> Passing -> Explosion -> CheckWin -> GameOver
```

Nenhum script conhece rede. `PlayerInputHandler` e `BotBrain` herdam de `TurnSolver`:
quando o multiplayer entrar, ele vira uma terceira implementação e o `GameManager` não
muda uma linha.

## Isto é greybox de propósito

O pitch do jogo promete 3D low-poly multijogador. O que roda aqui são cápsulas coloridas,
sem rede e sem áudio. O protótipo existe para responder **uma** pergunta:

> Digitar sequências de setas sob pressão de um pavio secreto é tenso e engraçado?

Arte, rede e som entram depois da resposta.

## Equipe

Ape Games — Yan · Rafael · Vinicius · Oneir · Carleon
