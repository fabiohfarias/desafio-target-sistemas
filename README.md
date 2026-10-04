# Target Sistemas — Desafio técnico

Solução em C# para os três exercícios do desafio: comissões de vendedores, movimentação de estoque e juros por atraso. A aplicação funciona pelo terminal e utiliza os arquivos JSON fornecidos no enunciado.

## Como executar

É necessário ter o [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) instalado. Na pasta raiz do projeto, execute:

```sh
dotnet run --project src/DesafioTarget
```

O menu reúne as consultas de comissões e estoque, o registro de movimentações, o histórico e o cálculo de juros. Digite `0` para sair.

| Campo | Formato de entrada |
| --- | --- |
| Valor em reais, sem separador de milhar | `1000,50` |
| Data de vencimento | `04/10/2026` |
| Tipo de movimentação | `E` para entrada ou `S` para saída |

## Comissões

A comissão é calculada por venda e depois somada por vendedor.

| Valor da venda | Comissão |
| --- | ---: |
| Abaixo de R$ 100,00 | 0% |
| De R$ 100,00 até menos de R$ 500,00 | 1% |
| A partir de R$ 500,00 | 5% |

Com os dados do desafio, o resultado é:

| Vendedor | Total vendido | Comissão |
| --- | ---: | ---: |
| Ana Lima | R$ 8.763,95 | R$ 404,99 |
| Carlos Oliveira | R$ 7.928,35 | R$ 379,38 |
| João Silva | R$ 10.754,70 | R$ 495,69 |
| Maria Souza | R$ 9.874,30 | R$ 465,96 |
| **Total** | **R$ 37.321,30** | **R$ 1.746,02** |

Os valores usam `decimal`. Como o enunciado não especifica o arredondamento, cada comissão é arredondada para duas casas antes da soma, usando `MidpointRounding.AwayFromZero`. Por exemplo, R$ 60,025 resulta em R$ 60,03.

## Estoque

As entradas acrescentam unidades ao saldo; as saídas descontam. Cada movimentação recebe um ID sequencial e registra data, produto, tipo, descrição, quantidade e saldo final. A sequência dos IDs continua após fechar e abrir o programa.

Exemplo com o produto **101 — Caneta Azul**:

| Operação | Quantidade | Saldo final |
| --- | ---: | ---: |
| Estoque inicial | — | 150 |
| Entrada: compra | 20 | 170 |
| Saída: venda | 15 | 155 |

O programa rejeita produtos inexistentes, quantidades menores ou iguais a zero, descrições vazias e saídas superiores ao estoque disponível.

O saldo e o histórico ficam em `dados-locais/estoque.json`, criado a partir dos cinco produtos do enunciado no primeiro acesso ao estoque. Execute o programa pela raiz do projeto para reutilizar os mesmos dados.

A gravação substitui o arquivo somente depois de concluir a escrita. Um bloqueio impede movimentações simultâneas de sobrescreverem o saldo. Se o arquivo estiver inválido, o programa informa o erro e preserva o conteúdo.

Para voltar ao estoque inicial, encerre a aplicação e faça backup ou remova a pasta `dados-locais`.

## Juros por atraso

Foi adotado o cálculo de **juros simples de 2,5% por dia corrido de atraso**, sem multa adicional:

```text
juros = valor × 0,025 × dias de atraso
total = valor + juros
```

A data de referência é a data local da máquina. O dia do vencimento não conta como atraso, e vencimentos futuros não geram juros. Valores negativos são rejeitados.

Por exemplo, **R$ 1.000,00** com **10 dias de atraso** gera **R$ 250,00** de juros, totalizando **R$ 1.250,00**. Os juros são arredondados para duas casas ao final do cálculo, com o mesmo critério usado nas comissões.

## Testes

Para executar os testes das regras:

```sh
dotnet run --project tests/DesafioTarget.Tests -c Release
```

São 40 testes que cobrem as faixas de comissão, os dados do enunciado, movimentações e persistência do estoque, identificadores, validações, arredondamento e datas. Cada teste imprime `OK` ou `FALHA`, e uma falha retorna código de saída `1`. O projeto usa um executável de testes próprio; o comando para rodá-lo é `dotnet run`.

No Windows, também é possível executar as 13 verificações dos fluxos pelo console:

```powershell
dotnet build DesafioTarget.slnx -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File tests/console-tests.ps1
```

Todos os testes de estoque usam pastas temporárias, sem alterar os dados da aplicação. Antes de recompilar uma configuração, encerre qualquer instância do programa que esteja usando essa versão.

## Estrutura do projeto

```text
DesafioTarget.slnx
src/DesafioTarget/
    Program.cs        Menu e leitura de entradas
    Comissoes.cs      Cálculo e agrupamento das comissões
    Estoque.cs        Movimentações e persistência
    Juros.cs          Cálculo dos juros
    Dados/            JSONs do enunciado
tests/
    DesafioTarget.Tests/
    console-tests.ps1
```

As regras ficam separadas do menu para permitir testes diretos de cada exercício.
