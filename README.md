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
| `Assets/Resources/Abertura/abertura.glb` | A cena inteira, com a animação. 12 MB, vai por Git LFS |
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

### Uma cena só

Desde 27/11/2026 o jogo inteiro roda em `Assets/Scenes/Abertura.unity`: menu, abertura, partida e tutorial.
Não há troca de cena. `Mundo.cs` põe a floresta na medida da partida; no fim da abertura o menu cria um
`GameBootstrap` que aproveita o mundo e a câmera que já existem, e a câmera gira até as costas do jogador.
`AppEntry.GoTo` passa por `MenuAbertura` (`AppEntry.Roteador`), que desmonta e monta a partida no lugar.
A cena `Prototipo` ficou só para abrir a partida sozinha durante o desenvolvimento.

### As duas cenas (como era)

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

O **idioma** troca tudo de uma vez: a tinta dos caixotes, da tampa e dos créditos (imagens
`*-en.png` em `Assets/Resources/Abertura`), as dicas no pé da tela, as páginas e a partida.
As marcas dos caixotes do cenário (EXPLOSIVOS, DINAMITE) ficam em português: são parte do desenho.

### Partida em 3D

Desde 26/11/2026 a partida usa os modelos da abertura. A regra é a mesma do protótipo.

| Arquivo | O que faz |
|---------|-----------|
| `Assets/Resources/Jogo/macaco-jogo.glb` + `.json` | O macaco sentado, com um clipe de 9 s que traz todas as animações em fila; o `.json` diz onde cada uma começa |
| `Assets/Resources/Jogo/maco-jogo.glb` | O maço de dinamite (a bomba) |
| `Assets/Scripts/Jogo3D/MacacoVisual.cs` | Toca as animações: parado, olhando, segura1/2/3, erro, jogaE, jogaD, recebe, explode, vence. Troca a cor do pelo |
| `Assets/Scripts/Jogo3D/PavioTopo3D.cs` | O pavio do topo da tela em 3D, preso à câmera |
| `GameBootstrap.BuildWorld` | Monta em volta da roda o mesmo cenário da abertura |

Quem segura a bomba fica mais desesperado conforme o pavio queima (`MonkeySlot.Tensao`).
Para voltar ao greybox de cápsulas: objeto `GameBootstrap` na cena `Prototipo`, desmarcar **Usar 3D**.

As animações nascem na prévia (`poseJogo` em `cena-abertura.html`) e são exportadas por
`__cena.exportaJogo()`. No Inspector do `macaco-jogo.glb`, **Animation** precisa estar em **Legacy**.

### Menu, primeira vez e personalização

- **Menu:** JOGAR grande, OPÇÕES menor, TUTORIAL e PERSONAGEM lado a lado, CRÉDITOS por último.
  Quem nunca jogou vê o TUTORIAL logo abaixo do JOGAR. As quatro pinturas (duas arrumações × dois
  idiomas) e as áreas de clique estão em `Assets/Resources/Abertura/menu-frente-*.png` e `menu-frente.json`.
- **Primeira vez:** ao clicar em JOGAR sem nunca ter jogado, a tampa do caixote de trás pergunta se
  quer o tutorial. A resposta fica guardada (`GameSettings.Played`).
- **PERSONAGEM:** as escolhas ficam escritas na frente do caixote do menu e o macaco aparece ao lado,
  mudando na hora. Dá para trocar a cor do pelo, o que vai na cabeça (palha, boné, cartola, fone),
  os óculos, o brinco e a cor da bomba (amarela, verde, podre).

| Arquivo | O que faz |
|---------|-----------|
| `Assets/Scripts/Jogo3D/Aparencia.cs` | A lista do que existe: pelos, acessórios e cores de bomba. Veste o jogador e sorteia a roupa dos bots |
| `MacacoVisual.Vestir` | Liga os acessórios escolhidos. Eles já vêm dentro do `macaco-jogo.glb`, em grupos `Ac_lugar_nome` |

Para criar um acessório: modelar na função `acessorios` da prévia, rodar `__cena.exportaJogo()`
e acrescentar o nome na lista de `Aparencia.cs`.

### Menu da partida, tutorial, som e música

- **Menu da partida:** Esc ou o botão MENU leva a câmera ao caixote de dinamite ao lado da roda. A partida
  não para. Se a bomba chegar na mão do jogador, aparece um alerta que devolve a câmera na hora.
  Tem NOVA PARTIDA e SAIR DA PARTIDA. Sair não corta: a câmera anda até a logo e a cena do menu
  entra na tela "aperte qualquer tecla" (`PausaNoCaixote.cs`, `MenuAbertura.EntrarNoTitulo`).
- **Tutorial:** a bomba cai na mão do macaco, as falas ficam no topo e avançam com Espaço. O pavio
  queima enquanto se lê e para em um terço até a hora de digitar (`TutorialDirector.cs`).
- **Explosão:** clarão, bolas de fogo e marca de queimado no chão (`Explosao.cs`); o macaco não morre nem fica
  tostado, sai voando para o lado, gritando (`MonkeySlot.VoaLonge`). O macaco da abertura fica lá atrás com as
  bombas acesas e joga uma nova para a roda a cada rodada (`GameManager.Lancador`).
- **Sons:** acerto, erro, arremesso, susto de quem recebe, risada de quem jogou, gritos de quem segura
  (mais rápidos conforme o pavio acaba), estalo do pavio, explosão; e a floresta ao fundo
  (`AmbienteSelva.cs`). Tudo fabricado por código em `SomRetro.cs`.
- **Música:** `Assets/Scripts/Som/MusicaRetro.cs` é uma orquestrinha sintetizada (flauta, clarinete,
  fagote, trompete, trombone, cordas, tímpano, caixa). Tem um tema e sete climas: Menu, Calmo, Dúvida,
  Alegria, Vilão, Tensão e Jogo. Na abertura o clima troca conforme a cena; na partida o tema acelera
  e ganha tímpano, caixa e cordas tremidas conforme `MusicaRetro.Perigo` sobe. **É rascunho:** não é
  orquestra gravada. Para trocar por música de verdade, basta substituir essa classe.

### Duas vidas e power-ups

Cada macaco tem 2 vidas. O poder aparece só para quem está com a bomba, com uma sequência de 3 teclas cuja
primeira é diferente da primeira da bomba: a primeira tecla apertada decide qual das duas está sendo feita.
Guarda-se um por vez e **Espaço** usa. Há quatro de ajuda (vida extra, pavio molhado, meia sequência, passe
relâmpago) e cinco de ataque em quem está com a bomba (comandos invertidos, sequência dobrada, pavio curto,
névoa, cola). Lista e regras em `Assets/Scripts/Jogo3D/PowerUps.cs`; efeitos em `GameManager.UsarPowerUp`.

### Emojis e emotes

Teclas **1 a 6** mandam um balão com uma carinha de macaco animada (riso, língua, bravo, choro, medo, sono) para
quem está com a bomba. Tecla **E** faz o seu macaco fazer a gracinha escolhida em PERSONAGEM. Os bots também
mandam emoji. Código em `Assets/Scripts/Jogo3D/Recados.cs`; as carinhas ficam em `Assets/Resources/Jogo/emojis.png`
e são desenhadas na prévia por `__cena.exportaEmojis()`.

### O que ainda não veio

- Modelos 3D dos power-ups; gracinhas com animação própria; música gravada.
- Música gravada, fumaça na explosão.

- O texto do **tutorial pintado** não vale: o botão TUTORIAL abre o tutorial do protótipo.
- O **traço de desenho** (sombra em degraus) e **balões com transparência suave**.
- Som e música de verdade: o que toca é rascunho sintetizado.
- Otimização: são cerca de 5.400 objetos. Serve para ver, não para entregar.
