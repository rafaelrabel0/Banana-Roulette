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

### Como ver e jogar

1. Abra o projeto. Na primeira vez a Unity baixa o pacote **glTFast** (`com.unity.cloud.gltfast`),
   que é quem sabe ler `.glb`.
2. Na janela **Project**, abra `Assets/Scenes/Abertura.unity` (duplo clique) e aperte **Play**.
3. Clique na tela: o carimbo bate na tampa, a câmera desce e o menu aparece no caixote da esquerda.
4. **JOGAR** toca a abertura (41 s; **Enter** pula) e, no fim, abre a partida.
   **TUTORIAL** abre o tutorial. **Esc** na partida volta ao menu.

Se a cena não existir ou quebrar: menu de cima **Banana Roulette → Montar cena de abertura**.
Se a cena aparecer de costas, selecione o objeto `Abertura` e desmarque
**Girar Camera 180** no componente `Abertura Player`.

### As duas cenas

| Cena | O que tem | Quem monta |
|------|-----------|------------|
| `Assets/Scenes/Abertura.unity` | Menu em 3D nos caixotes e a cena de abertura | `MenuAbertura` + `AberturaPlayer` |
| `Assets/Scenes/Prototipo.unity` | A partida e o tutorial | `GameBootstrap`, por código |

`AppEntry.GoTo(...)` troca de uma para a outra. As duas estão em **File → Build Profiles**,
com a Abertura em primeiro. A regra do jogo não mudou: é a mesma do protótipo.

### Menu 3D, som e visual retrô

| Arquivo | O que faz |
|---------|-----------|
| `Assets/Scripts/Abertura/MenuAbertura.cs` | O menu: carimbo, câmera, clique nas opções pintadas, créditos no chão |
| `Assets/Resources/Abertura/menu.json` | Posições de câmera e áreas de clique, exportadas da prévia |
| `Assets/Resources/Abertura/*.png` | Tampa lisa e carimbada, páginas das opções, créditos |
| `Assets/Scripts/Abertura/PaginaOpcoes.cs` | Páginas de opções escritas na tampa do caixote de trás, ligadas ao `GameSettings` |
| `Assets/Scripts/Abertura/LetraEstencil.cs` | Escreve texto em 3D com a letra de estêncil dos caixotes (`letra-estencil.png` + `.json` em `Assets/Resources/Abertura`) |
| `Assets/Scripts/Abertura/SomRetro.cs` | Som de rascunho fabricado por código (sem arquivo de áudio) |
| `Assets/Scripts/Visual/VisualRetro.cs` | Pixel aparente (320 linhas) e troca dos materiais; entra sozinho em toda cena |
| `Assets/Resources/Shaders/BananaRetro.shader` | Tremor dos vértices e faces chapadas, com a luz e a sombra da Unity |

Teclas de teste: **F1** liga/desliga o pixel, **F2** liga/desliga o tremor.

Nas páginas de opções: clique à esquerda da linha diminui, à direita aumenta; setas também
valem. O que cada página muda: **Som** e **Música** (volume), **Tela** (tela cheia, qualidade,
pixel, tremor), **Idioma** (português/inglês), **Jogabilidade** (jogar com setas ou com
W A S D). **Controles** só mostra as teclas. Tudo fica guardado entre uma sessão e outra.

### O que ainda não veio

- O texto do **tutorial pintado** não vale: o botão TUTORIAL abre o tutorial do protótipo.
- A **partida** ainda é greybox (cápsulas); só ganhou o pixel aparente.
- O **traço de desenho** (sombra em degraus) e **balões com transparência suave**.
- Som e música de verdade: o que toca é rascunho sintetizado.
- Otimização: são cerca de 5.400 objetos. Serve para ver, não para entregar.
