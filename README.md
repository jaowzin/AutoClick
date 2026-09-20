# AutoClick para Windows

Autoclick do **botão esquerdo** acionado pelo botão lateral do mouse: só clica enquanto o lateral escolhido estiver pressionado. Soltar o lateral interrompe os cliques. O botão direito físico continua independente para mirar em jogos FPS.

## Baixar

Abra a aba **Actions**, escolha a execução mais recente com selo verde do workflow "Compilar AutoClick (Windows)", baixe o artifact `AutoClick-Windows-x64`, extraia o ZIP e execute `AutoClick.exe` no Windows 10/11 de 64 bits. Versões antigas enviavam clique direito; feche-as pelo Gerenciador de Tarefas antes de iniciar a versão nova.

## Usar

1. Escolha "Lateral 1 (Voltar)" ou "Lateral 2 (Avançar)" e ajuste de 1 a 40 cliques por segundo.
2. Clique em **Ativar autoclick** (inicia pausado por segurança).
3. Segure o lateral para gerar cliques esquerdos; solte para interromper. Use o botão direito normalmente para mirar.
4. Clique em **Pausar autoclick** para desativar imediatamente.

O botão esquerdo físico também é preservado: se você o segurar, o autoclick pausa até soltá-lo, evitando que um `LEFTUP` virtual interrompa seu clique/arraste real. O botão direito **não é injetado, bloqueado nem remapeado**.

A opção de bloquear a ação original do lateral evita voltar/avançar no navegador. O programa detecta botões físicos com Windows Raw Input e usa o evento de soltura do hook como redundância. Quando o lateral não está bloqueado, verifica ainda seu estado a cada tick.

Se o software do seu mouse remapeia o lateral para uma tecla do teclado ou macro, configure-o como **Mouse Button 4 / Mouse Button 5**. O funcionamento no seu mouse e no jogo específico ainda precisa ser testado; certos jogos não aceitam entradas simuladas ou proíbem automações. A velocidade depende do timer do Windows.

## Compilar localmente

Com o SDK .NET 8 no Windows:

```powershell
dotnet run --project tests/StateTests.csproj --configuration Release
dotnet publish AutoClick.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -o dist
```
