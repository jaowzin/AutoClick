# AutoClick para Windows

Autoclick de botão direito para mouse com botão lateral: **só clica enquanto o lateral escolhido estiver realmente pressionado**. Ao soltar, o clique para. O botão direito original continua funcionando sem remapeamento.

## Baixar

Na aba **Actions**, abra a execução mais recente com selo verde do workflow "Compilar AutoClick (Windows)". Baixe o artifact `AutoClick-Windows-x64`, extraia o ZIP e execute `AutoClick.exe` no Windows 10/11 de 64 bits. **Não use o executável da primeira versão:** ela podia continuar clicando após perder um evento de soltura e interferir no botão direito físico.

## Usar

1. Feche qualquer `AutoClick.exe` antigo no Gerenciador de Tarefas antes de abrir o novo.
2. Escolha "Lateral 1 (Voltar)" ou "Lateral 2 (Avançar)" e ajuste entre 1 e 40 cliques por segundo.
3. Clique em **Ativar autoclick**. Ele inicia pausado por segurança.
4. Segure o lateral selecionado para clicar; solte para parar. Clique em **Pausar autoclick** para desativar imediatamente.

**O botão direito físico tem prioridade:** se você pressioná-lo enquanto segura o lateral, o programa suspende os cliques virtuais até você soltar o botão direito. Assim não envia solturas virtuais que interromperiam o clique/arraste direito real.

A opção de bloquear a ação original do lateral evita voltar/avançar no navegador. O programa recebe eventos físicos pelo Windows Raw Input e usa o evento de soltura do hook como redundância. Também verifica `GetAsyncKeyState` do botão direito antes de cada clique; quando o bloqueio original do lateral está desativado, verifica o estado do lateral nos ticks.

Se o software do mouse remapeia o lateral para uma tecla do teclado ou macro, configure-o como **Mouse Button 4 / Mouse Button 5** para que o Windows receba o botão físico. O funcionamento com seu modelo específico de mouse ainda precisa ser testado. A velocidade depende do timer do Windows.

## Compilar localmente

Instale o SDK .NET 8 e execute no Windows:

```powershell
dotnet run --project tests/StateTests.csproj --configuration Release
dotnet publish AutoClick.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -o dist
```
