using AutoClick;

static void Check(bool condition, string scenario)
{
    if (!condition) throw new Exception($"FALHOU: {scenario}");
    Console.WriteLine($"OK: {scenario}");
}

var state = new HoldState();
Check(!state.CanClick, "inicia pausado e nunca clica sem pressionar");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.None && !state.CanClick,
    "pressionamento com programa pausado nao inicia autoclick");

state.SetEnabled(true);
Check(!state.CanClick, "habilitar nao reaproveita pressao antiga");
Check(state.OnRawMouseButtons(0x0100) == HoldState.Transition.None && !state.CanClick,
    "botao lateral diferente nao inicia clique");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.Started && state.CanClick,
    "pressionar lateral selecionado inicia cliques esquerdos");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.None && state.CanClick,
    "DOWN repetido nao gera nova ativacao");
Check(state.OnRawMouseButtons(0x0004) == HoldState.Transition.None && state.CanClick,
    "pressionar direito fisico nao interfere em autoclick esquerdo");
Check(state.OnRawMouseButtons(0x0008) == HoldState.Transition.None && state.CanClick,
    "soltar direito fisico nao interfere em autoclick esquerdo");
Check(state.OnRawMouseButtons(0x0001) == HoldState.Transition.None && !state.CanClick && state.PhysicalLeftHeld,
    "pressionar esquerdo real suspende clique sintetico para preservar seu UP");
Check(state.OnRawMouseButtons(0x0000) == HoldState.Transition.None && !state.CanClick,
    "mover mouse com esquerdo real pressionado nao reativa autoclick");
Check(state.OnRawMouseButtons(0x0002) == HoldState.Transition.None && state.CanClick && !state.PhysicalLeftHeld,
    "soltar esquerdo real retoma apenas se lateral segue segurado");
Check(state.OnRawMouseButtons(0x0080) == HoldState.Transition.Stopped && !state.CanClick,
    "soltar lateral para imediatamente");
Check(state.OnRawMouseButtons(0x0080) == HoldState.Transition.None && !state.CanClick,
    "UP repetido nao volta a clicar");

state.OnRawMouseButtons(0x0040);
state.SetEnabled(false);
Check(!state.CanClick, "pausar enquanto segura interrompe cliques");
state.SetEnabled(true);
Check(!state.CanClick, "retomar exige novo DOWN");

state.SelectButton(2);
Check(!state.CanClick, "trocar de botao limpa estado anterior");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.None && !state.CanClick,
    "botao antigo nao ativa");
Check(state.OnRawMouseButtons(0x0100) == HoldState.Transition.Started && state.CanClick,
    "lateral 2 inicia");
Check(state.OnRawMouseButtons(0x0200) == HoldState.Transition.Stopped && !state.CanClick,
    "lateral 2 solto para imediatamente");
Check(state.OnRawMouseButtons(0x0100 | 0x0200) == HoldState.Transition.None && !state.CanClick,
    "UP tem prioridade sobre DOWN no mesmo pacote");

Console.WriteLine("Todos os testes de estado passaram.");
