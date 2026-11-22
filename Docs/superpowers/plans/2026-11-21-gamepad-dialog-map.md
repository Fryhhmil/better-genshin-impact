# Suporte a controle (diálogo + mapa) — Plano de implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Com o jogo no modo controle, o BetterGI detecta diálogo e mapa grande pela interface de controle e passa falas/escolhe opções por um DualShock 4 virtual (ViGEmBus).

**Architecture:** Preencher o backend reservado `GamepadInputBackend` (o `InputHub` troca o backend inteiro) sobre uma abstração `IVirtualGamepad`, com implementação ViGEm. O `Win32RuntimeProvider` escolhe o backend Gamepad quando a opção está ligada e o driver existe. As detecções `Bv.IsInTalkUi`/`Bv.IsInBigMapUi` ganham um segundo template (interface de controle). O `AutoSkipTrigger` usa o caminho por teclas no modo controle e conta as opções.

**Tech Stack:** .NET 10 / WPF, CommunityToolkit.Mvvm, OpenCV (template matching existente), `Nefarius.ViGEm.Client` 1.21.256, xUnit 2.5.

**Spec:** `docs/superpowers/specs/2026-11-21-gamepad-dialog-map-design.md`

## Global Constraints

- AGENTS.md: C# UTF-8 **sem BOM**; comentários em chinês; todo tipo/propriedade/campo/método novo comentado; `_camelCase` em campos privados; sem caminhos completos de tipo sem conflito; `?` em tipos anuláveis; bindings só-leitura com `Mode=OneWay`.
- Não executar o programa principal. Compilar: `dotnet msbuild BetterGenshinImpact.sln -restore -p:Configuration=Debug -nologo -tl:off -clp:ErrorsOnly` (se o BetterGI aberto travar a compilação, parar e avisar).
- Testes: `dotnet test Test\BetterGenshinImpact.UnitTest\BetterGenshinImpact.UnitTest.csproj --no-build --filter "FullyQualifiedName~<Classe>"` após compilar.
- Proibido criar `BetterGenshinImpact/Assets`; imagens novas vão em `GameTask/AutoSkip/Assets/1920x1080/` e `GameTask/QuickTeleport/Assets/1920x1080/`.
- Opção desligada (padrão) ⇒ comportamento idêntico ao atual.
- Sem driver ⇒ avisar **uma vez** no log com `https://github.com/nefarius/ViGEmBus/releases` e seguir com Win32.
- Botão de confirmar padrão: **○ (Circle)**.
- Escolha por texto/cor (laranja/"!", embutidas, textos prioritários) **não** é suportada no modo controle nesta versão.
- Textos de interface/log novos: chave em chinês via `{i18n:T ...}` / `I18nService` como no resto da página.
- Capturas de referência (não versionadas, contêm UID): `.local/gamepad-screens/{map-controller,talk-controller,talk-controller-confirm,talk-controller-option}.png`, 1920×1080.
- Commits sem atribuição/co-autoria do Claude.

## Review Focus

1. **Falso positivo do olho do controle fora do diálogo** (ex.: mundo aberto no modo controle): o `AutoSkipTrigger` apertaria confirmar sem parar. Esperado: só diálogo. Sem captura negativa do modo controle; Task 4 testa as capturas de mapa como negativas e o usuário valida no jogo.
2. **Mapa detectado como diálogo, ou o contrário**, pela barra de baixo parecida: Task 4 testa as duas negativas cruzadas.
3. **Driver removido/ausente com a opção ligada**: não pode lançar exceção no bind do runtime. Task 2 cobre com `TryCreate` retornando falso.
4. **Teclas sem mapeamento chegando ao backend Gamepad** (outras tarefas usando teclado/mouse): ignorar com aviso limitado, nunca lançar. Task 1 testa tecla desconhecida e clique de mouse.
5. **"Última"/"aleatória" com zero ícones contados** (opção detectada só pelo prompt de pegar): tratar como 1 opção e só confirmar. Task 5 testa a função de contagem.

---

### Task 1: Mapeamento e canal do controle virtual (sem driver)

**Files:**
- Create: `BetterGenshinImpact/Core/Input/Backends/Gamepad/GamepadButton.cs` (enum `GamepadButton { Cross, Circle, Square, Triangle, DpadUp, DpadDown, DpadLeft, DpadRight }` + enum `GamepadConfirmButton { Circle, Cross }`)
- Create: `BetterGenshinImpact/Core/Input/Backends/Gamepad/IVirtualGamepad.cs`
- Create: `BetterGenshinImpact/Core/Input/Backends/Gamepad/GamepadKeyMap.cs`
- Create: `BetterGenshinImpact/Core/Input/Backends/Gamepad/GamepadConfig.cs`
- Modify: `BetterGenshinImpact/Core/Input/Backends/Gamepad/GamepadInputBackend.cs` (substituir a reserva; `GamepadChannel` pode ir para `GamepadChannel.cs`)
- Modify: `BetterGenshinImpact/Core/Config/AllConfig.cs` (propriedade `GamepadConfig`, comentada, ao lado de `KeyBindingsConfig`)
- Test: `Test/BetterGenshinImpact.UnitTest/CoreTests/InputTests/GamepadChannelTests.cs`

**Interfaces:**
- Produces:
  - `interface IVirtualGamepad : IDisposable { void Press(GamepadButton button, int holdMs); void ReleaseAll(); }`
  - `sealed class GamepadKeyMap { GamepadKeyMap(GamepadConfirmButton confirm, IEnumerable<User32.VK> confirmKeys); GamepadButton Confirm { get; } GamepadButton Cancel { get; } bool TryMap(User32.VK key, out GamepadButton button); }`; teclas de confirmar → `Confirm`; `VK_ESCAPE` → `Cancel`; `VK_W`/`VK_UP` → `DpadUp`; `VK_S`/`VK_DOWN` → `DpadDown`; `VK_A`/`VK_LEFT` → `DpadLeft`; `VK_D`/`VK_RIGHT` → `DpadRight`.
  - `partial class GamepadConfig : ObservableObject` com `[ObservableProperty] bool _enabled = false;` e `[ObservableProperty] GamepadConfirmButton _confirmButton = GamepadConfirmButton.Circle;` (enum serializado como string, como `MapMaskConfig` faz).
  - `GamepadInputBackend(IVirtualGamepad pad, GamepadKeyMap keyMap)`; `Foreground` e `Background` são o mesmo `GamepadChannel`; `Dispose()` descarta o pad; `ReleaseAll()` chama `pad.ReleaseAll()`.
  - `GamepadChannel`: `OnKeyPress`/`OnKeyDown` mapeados → `pad.Press(button, PressHoldMs)` com `const int PressHoldMs = 120` (`OnKeyUp` mapeado é no-op, porque o `Press` já solta); não mapeados → `WarnUnsupported`. `SimulateAction(GIActions.PickUpOrInteract, …)` → `Press(keyMap.Confirm)`; outras ações → `WarnUnsupported`. Mouse, `MoveBy`, `MoveTo`, `Scroll` → `WarnUnsupported` (sem lançar).

- [ ] **Step 1: Write the failing tests** (fake `RecordingGamepad : IVirtualGamepad` que grava os botões)

```csharp
[Fact] void SpaceAndPickKeyPressConfiguredConfirm()   // Circle: KeyPress(VK_SPACE), KeyPress(VK_F) → [Circle, Circle]
[Fact] void CrossConfigSwapsConfirmAndCancel()        // Cross: VK_SPACE → Cross; VK_ESCAPE → Circle
[Fact] void WAndSMoveDpad()                          // VK_W → DpadUp; VK_S → DpadDown
[Fact] void InteractActionPressesConfirm()            // SimulateAction(GIActions.PickUpOrInteract) → [Circle]
[Fact] void UnmappedInputIsIgnored()                  // KeyPress(VK_Q), Mouse.LeftButtonClick(), MoveMouseBy(5,5) → nenhum botão, sem exceção
[Fact] void ReleaseAllReachesPad()                    // backend.ReleaseAll() → pad.ReleaseAll chamado
```

- [ ] **Step 2: Compile and run `GamepadChannelTests`** — Expected: FAIL (tipos não existem).
- [ ] **Step 3: Implement the types above.**
- [ ] **Step 4: Compile and run `GamepadChannelTests`** — Expected: 6 PASS.
- [ ] **Step 5: Commit** `feat(input): 手柄后端按键映射与虚拟手柄抽象`

### Task 2: DualShock 4 virtual via ViGEm + escolha do backend no runtime

**Files:**
- Modify: `BetterGenshinImpact/BetterGenshinImpact.csproj` (`<PackageReference Include="Nefarius.ViGEm.Client" Version="1.21.256" />`)
- Create: `BetterGenshinImpact/Core/Input/Backends/Gamepad/ViGEmDualShock4Gamepad.cs`
- Modify: `BetterGenshinImpact/Core/Input/Backends/Gamepad/GamepadInputBackend.cs` (fábrica)
- Modify: `BetterGenshinImpact/GameTask/Runtime/Win32/Win32RuntimeProvider.cs:89`

**Interfaces:**
- Consumes: Task 1 (`IVirtualGamepad`, `GamepadKeyMap`, `GamepadConfig`, construtor do backend).
- Produces:
  - `ViGEmDualShock4Gamepad : IVirtualGamepad`; `static bool TryCreate([NotNullWhen(true)] out ViGEmDualShock4Gamepad? pad, out string? error)`: cria `ViGEmClient` + `CreateDualShock4Controller()` + `Connect()` **na criação** (decisão: conectar já no bind evita perder o primeiro aperto enquanto o jogo reconhece o controle; atualiza a linha correspondente do spec). Botões de face via `SetButtonState(DualShock4Button.*)`, direcional via `SetDPadDirection(DualShock4DPadDirection.North/South/West/East)` e `None`; `Press` = baixa → `Thread.Sleep(holdMs)` → solta, sob `lock`. `Dispose` = `Disconnect` + `client.Dispose`, sem lançar.
  - `static IInputBackend GamepadInputBackend.CreateOrFallback(AllConfig config, IntPtr hWnd)`: se `!config.GamepadConfig.Enabled` → `new Win32InputBackend(hWnd)`; se `TryCreate` falhar → aviso **uma vez por processo** (flag estática), texto `"未检测到 ViGEmBus 驱动，手柄模式无法发送按键。请安装：https://github.com/nefarius/ViGEmBus/releases"`, e retorna Win32; senão o backend Gamepad com `GamepadKeyMap(config.GamepadConfig.ConfirmButton, [VK_SPACE, VK_RETURN, VK_F, <VK da tecla de pegar do AutoPickConfig.PickKey, convertida como em AutoPickAssets>])`.
  - `Win32RuntimeProvider`: `new Win32InputBackend(hWnd)` → `GamepadInputBackend.CreateOrFallback(config, hWnd)`. Ligar/desligar a opção vale a partir do próximo bind (reiniciar o BetterGI/captura). Isso vai no texto de ajuda da Task 3.

- [ ] **Step 1: Add the package; implement `ViGEmDualShock4Gamepad` and `CreateOrFallback`; wire the provider.**
- [ ] **Step 2: Compile** — Expected: exit 0.
- [ ] **Step 3: Run Task 1 tests again** — Expected: PASS (sem regressão).
- [ ] **Step 4: Commit** `feat(input): 通过 ViGEmBus 虚拟 DualShock 4 实现手柄后端`

> Sem teste unitário para o driver (o CI não tem ViGEmBus). A validação é o spike já feito e o teste final no jogo.

### Task 3: Opções na página de gatilhos

**Files:**
- Modify: `BetterGenshinImpact/View/Pages/TriggerSettingsPage.xaml` (dentro da seção do passar diálogo automático, perto da linha 561 `QuicklySkipConversationsEnabled`)
- Modify: arquivos de i18n existentes, se o projeto tiver a ferramenta `Build/BetterGenshinImpact.I18nSync` nesta branch (rodar com `-AddOnly`); senão, só as chaves em chinês.

**Interfaces:**
- Consumes: `Config.GamepadConfig.Enabled`, `Config.GamepadConfig.ConfirmButton` (Task 1).
- Produces: dois cartões no padrão dos vizinhos (`ui:CardControl`/`ui:ToggleSwitch` como os do auto-skip):
  - Título `{i18n:T 手柄模式（虚拟手柄）}`, descrição `{i18n:T 游戏使用手柄操作时开启：通过虚拟 DualShock 4 手柄跳过对话，需要安装 ViGEmBus 驱动，重新启动截图器后生效}`, `IsChecked="{Binding Config.GamepadConfig.Enabled, Mode=TwoWay}"`.
  - Título `{i18n:T 虚拟手柄确认键}`, ComboBox com os itens `Circle`/`Cross`, exibidos como `{i18n:T ○ 圆圈}` / `{i18n:T ✕ 叉}`, ligado a `Config.GamepadConfig.ConfirmButton` (`Mode=TwoWay`), habilitado só quando `Enabled`.

- [ ] **Step 1: Add the cards** (verificar o `DataContext` da página: os bindings vizinhos usam `Config.AutoSkipConfig...`).
- [ ] **Step 2: Compile** — Expected: exit 0.
- [ ] **Step 3: Commit** `feat(ui): 实时触发页增加手柄模式设置`

### Task 4: Detecção de diálogo e mapa na interface de controle

**Files:**
- Create: `BetterGenshinImpact/GameTask/AutoSkip/Assets/1920x1080/disabled_ui_gamepad.png` (olho riscado da barra de baixo, de `talk-controller.png` ~x 255–290, y 990–1025)
- Create: `BetterGenshinImpact/GameTask/QuickTeleport/Assets/1920x1080/MapSettingsButtonGamepad.png` (engrenagem embaixo à esquerda de `map-controller.png` ~x 140–170, y 990–1025; se não for estável, usar a barra de zoom ~x 100–115, y 475–605)
- Modify: `GameTask/AutoSkip/Assets/Recognition.json` (entrada `DisabledUiButtonGamepad`, `TemplateMatch`, ROI em `s` em volta do recorte com folga de ~40 px)
- Modify: `GameTask/QuickTeleport/Assets/Recognition.json` (entrada `MapSettingsButtonGamepad`, idem)
- Modify: `GameTask/Common/BgiVision/BvStatus.cs:202-212` e `:349-353`
- Test: `Test/BetterGenshinImpact.UnitTest/GameTaskTests/GamepadUiRecognitionTests.cs`

**Interfaces:**
- Produces: `Bv.IsInTalkUi(region)` = template de teclado **ou** `DisabledUiButtonGamepad`; `Bv.IsInBigMapUi(region)` = os dois do teclado **ou** `MapSettingsButtonGamepad`. Assinaturas inalteradas.
- Recorte: salvar em PNG sem perdas (cortar com OpenCV por um teste temporário ou script; `cv2` não está instalado no Python local). Usar a resolução exata da captura (já é 1920×1080).

- [ ] **Step 1: Write the tests** (carregar as capturas de `..\..\..\..\..\.local\gamepad-screens\` como os testes de pesca carregam `Mat` + `ImageRegion` + `FakeSystemInfo`; se o arquivo não existir, retornar sem afirmar, porque as capturas não são versionadas)

```csharp
[Theory] [InlineData("talk-controller.png")] [InlineData("talk-controller-confirm.png")] [InlineData("talk-controller-option.png")]
void TalkScreensAreTalkNotBigMap(string file)   // Assert.True(Bv.IsInTalkUi(r)); Assert.False(Bv.IsInBigMapUi(r))
[Fact] void MapScreenIsBigMapNotTalk()           // map-controller.png: Assert.True(Bv.IsInBigMapUi(r)); Assert.False(Bv.IsInTalkUi(r))
```

- [ ] **Step 2: Compile and run `GamepadUiRecognitionTests`** — Expected: FAIL nos `True` (template novo ainda não existe).
- [ ] **Step 3: Crop the templates, add the Recognition.json entries, extend the two `Bv` methods.**
- [ ] **Step 4: Compile and run `GamepadUiRecognitionTests`** — Expected: 4 PASS (conferir que as capturas foram de fato encontradas, sem cair no retorno antecipado).
- [ ] **Step 5: Commit** `feat(vision): 识别手柄界面下的对话与大地图`

### Task 5: AutoSkipTrigger no modo controle

**Files:**
- Modify: `BetterGenshinImpact/GameTask/AutoSkip/AutoSkipTrigger.cs` (linhas ~333–352, `ChatOptionChooseUseKey` ~617–670, `ClickBlackGameScreen` ~403–425)
- Create: `BetterGenshinImpact/GameTask/AutoSkip/GamepadOptionPlanner.cs`
- Test: `Test/BetterGenshinImpact.UnitTest/GameTaskTests/AutoSkipTests/GamepadOptionPlannerTests.cs`

**Interfaces:**
- Consumes: `InputHub.Backend.Kind`, `InputBackendKind.Gamepad`, `GamepadChannel` mapeando W/S/F/Espaço (Task 1).
- Produces:
  - `private bool IsGamepadMode => InputHub.Backend.Kind == InputBackendKind.Gamepad;`
  - `static class GamepadOptionPlanner { static int DownPresses(int optionCount, bool last, bool random, Random rng); }`: `count = Math.Max(optionCount, 1)`; primeira → 0; última → `count - 1`; aleatória → `rng.Next(0, count)`.
  - Avanço: com `IsGamepadMode`, o caminho de escolha usa sempre `ChatOptionChooseUseKey` (condição da linha ~345 ganha `|| IsGamepadMode`).
  - Em `ChatOptionChooseUseKey`, quando `IsGamepadMode`: contar `region.FindMulti(GetRecognitionObject("OptionIcon", region)).Count`, apertar `VK_S` `DownPresses(...)` vezes com 150 ms entre apertos e depois a tecla de pegar (vira confirmar). O caminho de teclado continua igual.
  - `ClickBlackGameScreen`: com `IsGamepadMode`, `InputHub.Background.SimulateAction(GIActions.PickUpOrInteract)` em vez do clique (mantém o intervalo de 1200 ms).

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] void FirstOptionNeedsNoMove()        // DownPresses(3, last:false, random:false, rng) == 0
[Fact] void LastOptionMovesToBottom()        // DownPresses(4, true, false, rng) == 3
[Fact] void ZeroCountedTreatedAsOne()        // DownPresses(0, true, false, rng) == 0 ; DownPresses(0, false, true, rng) == 0
[Fact] void RandomStaysInRange()             // 200 iterações com new Random(1): 0 <= r < 3 para count 3
```

- [ ] **Step 2: Compile and run `GamepadOptionPlannerTests`** — Expected: FAIL.
- [ ] **Step 3: Implement the planner and the three trigger changes.**
- [ ] **Step 4: Compile and run `GamepadOptionPlannerTests` + `GamepadChannelTests`** — Expected: PASS.
- [ ] **Step 5: Commit** `feat(auto-skip): 手柄模式下用虚拟手柄跳过对话并选择选项`

### Task 6: Fechamento

- [ ] **Step 1:** Compilar a solução inteira e rodar `dotnet test ... --no-build --filter "FullyQualifiedName~Gamepad"` — Expected: tudo PASS.
- [ ] **Step 2:** Atualizar o spec: conexão no bind (Task 2) em vez de no primeiro aperto.
- [ ] **Step 3:** Revisão da branch inteira por um revisor novo.
- [ ] **Step 4:** Passar ao usuário o roteiro de teste no jogo: ligar o "Modo controle", reiniciar a captura, diálogo com o DualSense ligado (falas avançam; opções primeira/última/aleatória), abrir o mapa grande (mapa sobreposto aparece), mundo aberto parado por 1 min (nenhum aperto, Review Focus 1).
