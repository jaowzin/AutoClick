# AutoClick para Windows — versão com verificação física

Autoclick de **botão esquerdo** enquanto o lateral escolhido está **fisicamente pressionado**. Soltar o lateral interrompe os cliques. O botão direito original permanece livre para mirar.

## Recuperar o mouse caso esteja usando uma versão antiga

Feche todos os AutoClick anteriores, inclusive cópias renomeadas. Se o botão esquerdo não funcionar, use o teclado: pressione **Win + R**, digite e confirme com Enter:

```powershell
powershell -NoProfile -Command "Get-Process AutoClick* | Stop-Process -Force"
```

Se não voltar ao normal, use **Ctrl + Alt + Del** e reinicie o Windows pelo teclado. Não execute o AutoClick antigo junto com esta versão.

## Baixar e usar

Em [Actions](https://github.com/jaowzin/AutoClick/actions), escolha uma execução verde do workflow **Compilar AutoClick (Windows)** posterior à correção e baixe o artifact `AutoClick-Windows-x64`. Extraia o ZIP e execute `AutoClick.exe` em Windows 10/11 x64.

1. O programa abre **pausado**. Escolha o lateral 1 ou 2 e a velocidade de 1 a 40 cliques por segundo.
2. Clique em **Ativar autoclick**.
3. Segure o lateral para enviar cliques esquerdos; solte para parar. O botão direito físico continua normal.
4. Clique em **Pausar autoclick** para desativar.

**O que mudou:** eliminamos o hook global que bloqueava o lateral e podia impedir a atualização de seu estado pelo Windows. A cada tentativa de clique, `GetAsyncKeyState` confirma que o lateral **continua pressionado**; se não estiver, o programa desarma. Os eventos do mouse físico (Raw Input) continuam sendo usados para iniciar/parar. O esquerdo físico tem prioridade: ao segurá-lo, o autoclick não envia `LEFTUP` sobre seu clique. Um envio parcial de `LEFTDOWN` recebe uma tentativa imediata de `LEFTUP` e o programa pausa em caso de erro.

**Observação:** o lateral preserva sua ação normal de Voltar/Avançar em navegadores, pois interceptá-la prejudicava a verificação de pressão. Se o software de seu mouse remapeia o lateral para teclas ou macros, configure-o como **Mouse Button 4 / Mouse Button 5**. O funcionamento com seu mouse e com seu jogo específico ainda depende de teste real; alguns jogos ignoram cliques simulados ou proíbem automações.

## Compilar localmente

Com o SDK .NET 8 no Windows:

```powershell
dotnet run --project tests/StateTests.csproj --configuration Release
dotnet publish AutoClick.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -o dist
```
