using AutoClick;

static void Check(bool condition, string scenario)
{
    if (!condition) throw new Exception($"FALHOU: {scenario}");
    Console.WriteLine($"OK: {scenario}");
}

var state = new HoldState();
Check(!state.CanClickWithPhysicalState(true, false), "inicia pausado sem cliques");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.None && !state.CanClick,
    "DOWN com programa pausado nao inicia autoclick");

state.SetEnabled(true);
Check(!state.CanClick, "reativar nao reaproveita pressionamento antigo");
Check(state.OnRawMouseButtons(0x0100) == HoldState.Transition.None && !state.CanClick,
    "lateral nao selecionado nao ativa");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.Started && state.CanClick,
    "lateral escolhido fisicamente pressionado arma autoclick");
Check(state.CanClickWithPhysicalState(true, false), "pode clicar somente com lateral confirmado");
Check(!state.CanClickWithPhysicalState(false, false),
    "sem confirmacao fisica nao clica MESMO com estado interno preso");
Check(!state.CanClickWithPhysicalState(true, true),
    "esquerdo fisico pressionado preserva clique manual");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.None,
    "DOWN duplicado nao cria nova sessao");
Check(state.OnRawMouseButtons(0x0004) == HoldState.Transition.None && state.CanClick,
    "botao direito livre para mirar");
Check(state.OnRawMouseButtons(0x0008) == HoldState.Transition.None && state.CanClick,
    "soltar direito nao interfere");
Check(state.OnRawMouseButtons(0x0001) == HoldState.Transition.None && !state.CanClick,
    "esquerdo fisico pressionado suspende clique sintetico");
Check(!state.CanClickWithPhysicalState(true, false),
    "estado fisico esquerdo relatado pelo Raw Input tambem bloqueia clique");
Check(state.OnRawMouseButtons(0x0002) == HoldState.Transition.None && state.CanClick,
    "soltar esquerdo real permite autoclick se lateral continua pressionado");
Check(state.OnRawMouseButtons(0x0080) == HoldState.Transition.Stopped && !state.CanClick,
    "soltar lateral para imediatamente");
Check(!state.CanClickWithPhysicalState(true, false), "UP desarma mesmo que consulta ainda mostre DOWN");
Check(state.OnRawMouseButtons(0x0080) == HoldState.Transition.None && !state.CanClick,
    "UP duplicado nao reinicia");

state.OnRawMouseButtons(0x0040);
state.SetEnabled(false);
Check(!state.CanClickWithPhysicalState(true, false), "pausar interrompe mesmo enquanto segura lateral");
state.SetEnabled(true);
Check(!state.CanClick, "retomar exige novo DOWN");

state.SelectButton(2);
Check(!state.CanClick, "troca de lateral limpa estado");
Check(state.OnRawMouseButtons(0x0040) == HoldState.Transition.None && !state.CanClick,
    "lateral antigo nao ativa");
Check(state.OnRawMouseButtons(0x0100) == HoldState.Transition.Started && state.CanClick,
    "lateral 2 inicia");
Check(state.OnRawMouseButtons(0x0200) == HoldState.Transition.Stopped && !state.CanClick,
    "soltar lateral 2 interrompe");
Check(state.OnRawMouseButtons(0x0100 | 0x0200) == HoldState.Transition.None && !state.CanClick,
    "UP prevalece sobre DOWN simultaneo");

Console.WriteLine("Todos os testes de seguranca de estado passaram.");
