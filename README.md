# AutoClick para Windows — botão lateral → clique esquerdo

Programa para Windows 10/11 x64. Envia cliques **esquerdos** apenas enquanto o botão lateral selecionado estiver fisicamente pressionado. O botão direito continua livre para mirar, e o esquerdo físico tem prioridade.

## Baixar

Na aba [Actions](https://github.com/jaowzin/AutoClick/actions), abra a execução mais recente **com selo verde** de `Compilar AutoClick (Windows)`, baixe `AutoClick-Windows-x64`, extraia e execute `AutoClick.exe`. Feche quaisquer executáveis antigos antes de iniciar o novo, inclusive cópias com nomes diferentes.

## Usar

1. O programa inicia pausado. Selecione o lateral 1 ou 2, escolha de **1 a 1.000 CPS** e clique em **Ativar autoclick**.
2. Segure o lateral para enviar cliques esquerdos; solte para parar. O programa verifica o estado físico do lateral antes de cada clique.
3. Clique no **X** para ocultar a janela, não para encerrar: ele permanece na **bandeja do sistema** (setinha `^` perto do relógio). Dê dois cliques no ícone para reabrir.
4. Clique com o botão direito no ícone para **Abrir AutoClick**, **Pausar/Ativar autoclick** ou **Sair completamente**.
5. **Ctrl+Alt+F12** pausa o autoclick de emergência, mesmo com a janela escondida (se essa combinação não estiver ocupada por outro aplicativo).

**1.000 CPS é um limite configurável, não uma velocidade garantida.** Windows, CPU, polling do mouse e jogos podem entregar bem menos cliques e alguns jogos ignoram ou proíbem automações. Valores elevados podem consumir mais CPU. Comece testando com 10–20 CPS fora do jogo e aumente gradualmente.

## Segurança e limitações

- O aplicativo não instala drivers, não modifica firmware e não intercepta o botão esquerdo/direito físico. A ação original do lateral (Voltar/Avançar no navegador) permanece disponível.
- O botão lateral é conferido antes de cada envio; perda do evento de soltura não deve manter o autoclick ativo indefinidamente. O programa não acumula cliques perdidos.
- Quando o botão esquerdo real está pressionado, o autoclick suspende os envios para não interromper arrastes ou cliques físicos. Falha no `SendInput` pausa o autoclick.
- Fechar pelo X **não** desliga o programa. Use o menu **Sair completamente** para encerrar. Na ausência do ícone, verifique a área oculta `^`.
- Não foi possível verificar a operação com seu mouse e jogo específicos apenas com testes automatizados.

**Se uma versão antiga travar o mouse:** pressione `Win + R`, digite `powershell -NoProfile -Command "Get-Process AutoClick* | Stop-Process -Force"` e pressione Enter. Se ainda não voltar, reinicie o Windows pelo teclado (`Ctrl + Alt + Del`).

## Compilar localmente

Com o SDK .NET 8 instalado no Windows:

```powershell
dotnet run --project tests/StateTests.csproj --configuration Release
dotnet publish AutoClick.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -o dist
```
