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
    "pressionar lateral selecionado inicia cliques");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.None && state.CanClick,
    "DOWN repetido nao gera nova ativacao");
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
