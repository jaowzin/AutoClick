# AutoClick para Windows

Programa simples que envia cliques direitos repetidos enquanto um botão lateral do mouse estiver pressionado. O botão direito físico continua funcionando.

## Baixar

Abra a aba Actions deste repositório, selecione a execução concluída do workflow "Compilar AutoClick (Windows)" e baixe o artifact `AutoClick-Windows-x64`. Extraia o ZIP e abra `AutoClick.exe` no Windows 10 ou 11 de 64 bits.

## Usar

Escolha o botão lateral 1 ou 2 e configure entre 1 e 40 cliques por segundo. Segure o lateral para iniciar; solte para parar. A opção de bloquear a ação original evita que o botão lateral navegue para trás ou para a frente. Clique em "Pausar autoclick" para desativar o recurso.

O programa continua funcionando enquanto sua janela estiver aberta, mesmo em segundo plano. O CPS é aproximado e depende do timer do Windows.

## Compilar localmente

Instale o SDK .NET 8 e execute no Windows:

```powershell
dotnet publish AutoClick.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o dist
```
