# Suporte a controle: passar diálogo e mapa sobreposto

Data: 2026-11-21 · Branch: `feat/gamepad-support` (a partir de `origin/refactor/20260930`)

## Intenção

O usuário joga Genshin com um DualSense (modo controle do jogo). Nesse modo o BetterGI não funciona:

1. **Não detecta nada.** A detecção de tela procura ícones da interface de teclado/mouse, que no modo controle mudam de lugar ou de desenho.
2. **Não consegue agir.** Com o jogo no modo controle, o teclado é ignorado; trocar de modo exige ir nas configurações do jogo.

Primeira entrega: **passar diálogo** (detectar a fala e avançar) e **mapa sobreposto** (detectar que o mapa grande está aberto). O resto fica para depois.

## Metas

- Detectar o diálogo com a interface de controle (1920×1080 de referência, escalando como o resto).
- Detectar o mapa grande com a interface de controle, para o `MapMaskTrigger` ligar o mapa sobreposto.
- Avançar falas e escolher opções de diálogo por um **controle virtual DualShock 4** (ViGEmBus), conectado como **segundo controle**, sem atrapalhar o DualSense do usuário.

## Fora do escopo (agora)

Escolha de opções por texto/cor no modo controle (prioridade de opções laranja ou com "!", escolhas embutidas como comissão diária e expedições, textos prioritários configurados): nesta versão o modo controle só faz primeira/última/aleatória (decisão do usuário). Pegar itens, convites (hangout), fechar pop-ups, enviar itens, menus, rotas, combate, pesca e as demais tarefas. Também: repassar o DualSense para o virtual, vibração e configurar o HidHide.

## Anti-metas

- Não quebrar nada no modo teclado/mouse: com a opção desligada, o comportamento é idêntico ao atual.
- Não exigir o driver para quem não usa controle: sem ViGEmBus, o BetterGI funciona igual e só avisa uma vez se a opção estiver ligada.
- Não desconectar o controle virtual no meio do jogo sem necessidade: se ele for o único controle, o Genshin mostra "Erro de conexão do controle".

## O que os testes mostraram (spike descartável, `scratchpad/gamepad-spike`)

- O ViGEmBus clássico 1.22 (gratuito) funciona ao lado do VirtualPad (G2) que o DSX instala. O SDK do VirtualPad é licenciado e não pode ser usado.
- O Genshin aceita o DualShock 4 virtual **com o DualSense ligado ao mesmo tempo**, e os dois funcionam.
- **Botão de confirmar:** o usuário configurou ✕ = confirmar no jogo (o ícone na tela mostra ✕), mas o controle virtual confirmou com **○**, o padrão do jogo. □ alternou o modo automático da fala, como esperado. Conclusão: o botão de confirmar do virtual não pode ser deduzido do ícone da tela; vira opção, com padrão ○.
- O Xbox 360 virtual também foi testado, mas apertando A (que seria "voltar" no padrão do jogo), então não foi conclusivo. Não é necessário: o DualShock 4 mantém os ícones de PlayStation.

## Como funciona hoje (pontos tocados)

- **Entrada:** `Core/Input/InputHub` troca o backend inteiro (`IInputBackend`: Win32, WebSdk, Gamepad). `Backends/Gamepad/GamepadInputBackend.cs` já existe como **reserva**, com as convenções escritas no comentário: sobrescrever `SimulateAction` mapeando `GIActions` para botões; teclas mapeadas de volta para `GIActions` pela configuração de teclas; `MoveMouseBy` para o analógico direito; clique cru e `MoveMouseTo` ignorados com aviso. Quem anexa o backend é `GameTask/Runtime/GameRuntimeService` (`InputHub.Attach(runtime.Input)`).
- **Diálogo:** `Bv.IsInTalkUi` procura `AutoSkip/DisabledUiButton` (`disabled_ui.png`, ROI `rect(0, 0, cw / 3, ch / 8)`), por template matching do OpenCV. `Bv.WhichGameUiForTriggers` classifica cada quadro (Talk/BigMap/Unknown) no `TaskTriggerDispatcher`. O `AutoSkipTrigger` avança com `SimulateAction(GIActions.PickUpOrInteract)` ou `KeyPress(VK_SPACE)`, e escolhe opções com F, W e S (`ChatOptionChooseUseKey`) ou com clique (`ChatOptionChoose`).
- **Mapa:** `Bv.IsInBigMapUi` procura `QuickTeleport/MapScaleButton` (ROI `rect(30*s, 440*s, 40*s, 200*s)`, com máscara) ou `MapSettingsButton` (ROI `rect(25*s, 990*s, 58*s, 62*s)`). O `MapMaskTrigger` usa `CurrentGameUiCategory == BigMap || Bv.IsInBigMapUi`.

## Abordagens consideradas

1. **ViGEmBus imitando DualShock 4, como segundo controle** — escolhida. Driver gratuito e consolidado (usado pelo DS4Windows), o jogo aceita junto com o DualSense, e os ícones continuam de PlayStation, então um único conjunto de imagens de referência serve.
2. **ViGEmBus imitando Xbox 360.** Mais compatível em geral, mas pode trocar os ícones para A/B/X/Y e dobrar as imagens de referência.
3. **VirtualPad do DSX / vJoy.** VirtualPad: SDK licenciado. vJoy: o Genshin não reconhece.
4. **BetterGI como único controle (repassando o DualSense, com HidHide).** Desnecessário, porque o jogo aceita dois controles; ficaria muito maior.

## Desenho

### Opções (configuração)

- **"Modo controle (controle virtual)"**, ligado/desligado, padrão desligado. Decisão minha: é **manual**, porque o próprio jogo só troca de modo pelas configurações, e decidir pela tela poderia oscilar. Quando ligado, o `InputHub` usa o backend Gamepad em vez do Win32.
- **"Botão de confirmar do controle virtual"**: ○ ou ✕, padrão ○ (resultado do teste).
- Onde ficam na interface: perto das opções de passar diálogo ou na página de configurações gerais/entrada. Fica a critério do builder, seguindo o padrão das telas existentes. Todo texto novo passa por i18n (chave em chinês, como no resto do projeto).

### Controle virtual (`GamepadInputBackend`)

- Usar o pacote `Nefarius.ViGEm.Client` (testado na versão 1.21.256).
- Ao anexar o runtime com a opção ligada, testar o driver criando o `ViGEmClient` (decisão minha). Se funcionar, anexar o backend Gamepad; o DualShock 4 é criado e conectado na primeira vez que for apertar algo e fica conectado até o backend ser descartado (fim do runtime ou opção desligada).
- Se o driver faltar (exceção ao criar o `ViGEmClient`): avisar **uma vez** no log, com o link `https://github.com/nefarius/ViGEmBus/releases`, e continuar com o backend Win32, sem lançar exceção. As detecções (inclusive o mapa sobreposto) continuam funcionando; só as ações não chegam ao jogo, já que no modo controle ele ignora o teclado.
- Apertos: botão para baixo, ~100–150 ms, botão para cima. Os testes funcionaram com 300 ms; vale manter um intervalo seguro, por causa dos 30 fps.
- Mapeamento mínimo para o escopo:
  - `GIActions.PickUpOrInteract`, Espaço e a tecla de interação (F) → botão de confirmar configurado (○ ou ✕).
  - W / S nas opções de diálogo → direcional ↑ / ↓.
  - Clique cru de mouse e `MoveMouseTo` → ignorar com aviso (como já descrito na reserva).
  - O resto das teclas → ignorar com aviso (e, opcionalmente, mapear `GIActions` óbvias, se for trivial).
- `IsKeyDown`/`ReleaseAll`: soltar todos os botões do virtual.

### Detecção do diálogo

- Nova imagem de referência do **olho riscado da barra de baixo** da interface de controle (visível nas capturas: ~x 255–290, y 990–1025 em 1920×1080), com ROI na faixa inferior esquerda (algo como `rect(0, ch * 0.85, cw / 4, ch * 0.15)`).
- `Bv.IsInTalkUi` passa a retornar verdadeiro se achar o ícone do teclado **ou** o do controle. As duas checagens rodam sempre, porque são baratas e evitam depender da opção.
- **Opções de diálogo no modo controle:** a primeira opção já vem selecionada, então "primeira" = só confirmar. Para "última"/"aleatória", **contar as opções na tela** (ícones de opção, `FindMulti`) e descer com ↓ até a desejada antes de confirmar (decisão minha). Assim não depende de o ↑ dar a volta para a última, como faz hoje o `ChatOptionChooseUseKey` com W, o que não foi testado no controle.
- O ícone de opção do diálogo (`OptionIcon`) e o triângulo do fim da fala provavelmente continuam iguais; o builder deve conferir nas capturas e, se não forem, criar a variante de controle.

### Detecção do mapa grande

- Nova imagem de referência estável da interface de controle no mapa. Candidatas: a **engrenagem** do canto inferior esquerdo (~x 140–170, y 990–1025) ou a **barra de zoom sem +/−** (~x 100–115, y 475–605).
- `Bv.IsInBigMapUi` aceita as do teclado **ou** a do controle.

### Imagens de referência

Recortar das capturas do usuário (1920×1080), guardadas pelo builder junto às imagens de cada tarefa (`GameTask/AutoSkip/Assets/1920x1080/` e `GameTask/QuickTeleport/Assets/1920x1080/`; isso **não** é a pasta `BetterGenshinImpact/Assets` proibida pelo AGENTS.md) e registradas no `Recognition.json` de cada uma:

- mapa: `.local/gamepad-screens/map-controller.png`
- diálogo: `.local/gamepad-screens/talk-controller.png` (barra de baixo), `talk-controller-confirm.png` ("□ Automático ✕ Confirmar") e `talk-controller-option.png` (opção com ícone)

`.local/` não é versionado; as capturas são só a fonte dos recortes.

## Verificação

- Compilação com o comando do AGENTS.md (o programa principal não é executado).
- Teste unitário para o mapeamento do backend Gamepad (`GIActions`/teclas → botões), usando uma abstração fina sobre o controle virtual, sem driver.
- Teste unitário das novas detecções contra as capturas reais (os testes existentes de reconhecimento servem de modelo, se houver).
- Teste final no jogo pelo usuário: falas avançando com o DualSense ligado, opções escolhidas, mapa sobreposto aparecendo no mapa grande.

## Fica a critério do builder

- Nome e lugar exato das opções e textos.
- Recorte exato e limiar de semelhança das imagens novas.
- Estrutura interna do backend (classe de mapeamento separada, etc.), seguindo o AGENTS.md (comentários em chinês, UTF-8 sem BOM, um tipo por arquivo importante).
