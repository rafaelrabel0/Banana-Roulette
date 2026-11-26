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

## Cena de abertura 3D (ramo `abertura-3d`)

A abertura do jogo (menu no caixote, o macaco, o aviador, a roda) foi desenhada fora da
Unity, numa prévia em three.js, e chega aqui **assada**: um único arquivo com todas as
peças e 41 s de movimento.

| Arquivo | O que é |
|---------|---------|
| `Assets/Abertura/abertura.glb` | A cena inteira, com a animação. 12 MB, vai por Git LFS |
| `Assets/Abertura/abertura.json` | O que o `.glb` não carrega: campo de visão por quadro, quando é céu, quando entra o letreiro |
| `Assets/Abertura/Modelos/*.glb` | Macaco, aviador, caixote, maço de dinamite e carimbo, soltos e parados |
| `Assets/Scripts/Abertura/AberturaPlayer.cs` | Toca a cena e move a câmera |
| `Assets/Editor/MontaAbertura.cs` | Menu que monta a cena sozinho |

### Como ver

1. Abra o projeto. Na primeira vez a Unity baixa o pacote **glTFast** (`com.unity.cloud.gltfast`),
   que é quem sabe ler `.glb`.
2. No menu de cima: **Banana Roulette → Montar cena de abertura**.
3. Aparece o aviso "Cena criada em Assets/Scenes/Abertura.unity". Aperte **Play**.

Se a cena aparecer de costas, selecione o objeto `Abertura` e desmarque
**Girar Camera 180** no componente `Abertura Player`.

### Visual retrô

Desde 26/11/2026 o padrão do jogo é o visual de poucos triângulos: formas angulares, faces
chapadas, texturas de 64 pixels. **Os modelos e as texturas já vêm assim no `.glb`.** O que
depende da Unity ainda não foi feito:

- desenhar a imagem com 320 linhas e ampliar sem suavizar (pixel aparente);
- prender os vértices a uma grade na tela (o tremor).

### O que ainda não veio

- O **traço de desenho** (sombra em degraus). O contorno preto veio, porque é malha; a
  sombra em degraus depende de um shader que ainda não foi escrito para a URP.
- O **menu** (carimbo, opções nos caixotes). Só a cena de abertura foi assada.
- **Som**, **letreiro** e **balões com transparência suave**.
- Otimização: são cerca de 5.400 objetos. Serve para ver, não para entregar.
