using DesafioTarget;

int total = 0, falhas = 0;
foreach (var (valor, esperado) in new (decimal, decimal)[]
{
    (0, 0), (99.99m, 0), (100, 1), (499.99m, 5), (500, 25), (1200.50m, 60.03m)
})
    Testar($"Comissão de {valor}", () => Igual(esperado, Comissoes.Calcular(valor)));
Testar("Venda negativa", () => Rejeita<ArgumentException>(() => Comissoes.Calcular(-1)));
Testar("Agrupamento aplica faixa por venda", () =>
{
    var resultado = Comissoes.Agrupar([new("Ana", 90), new("Ana", 250), new("Ana", 500)]).Single();
    Igual(840m, resultado.TotalVendas);
    Igual(27.50m, resultado.Comissao);
});
Testar("Vendedor sem nome", () => Rejeita<ArgumentException>(() => Comissoes.Agrupar([new(" ", 100)])));
Testar("Lista vazia", () => Igual(0, Comissoes.Agrupar([]).Count));
Testar("Dados completos do desafio", () =>
{
    var vendas = ArquivosJson.Ler<ArquivoVendas>(Path.Combine(AppContext.BaseDirectory, "Dados", "vendas.json"));
    Igual(36, vendas.Vendas.Count);
    var resultado = Comissoes.Agrupar(vendas.Vendas).ToDictionary(c => c.Vendedor);
    Igual(495.69m, resultado["João Silva"].Comissao);
    Igual(465.96m, resultado["Maria Souza"].Comissao);
    Igual(379.38m, resultado["Carlos Oliveira"].Comissao);
    Igual(404.99m, resultado["Ana Lima"].Comissao);
    Igual(10754.70m, resultado["João Silva"].TotalVendas);
    Igual(9874.30m, resultado["Maria Souza"].TotalVendas);
    Igual(7928.35m, resultado["Carlos Oliveira"].TotalVendas);
    Igual(8763.95m, resultado["Ana Lima"].TotalVendas);
    Igual(4, resultado.Count);
    Igual(1746.02m, resultado.Values.Sum(v => v.Comissao));
});
Testar("Vendedores distintos e espaços no nome", () =>
{
    var resultado = Comissoes.Agrupar([new(" Ana ", 100), new("Ana", 500), new("Carlos", 99)]);
    Igual(2, resultado.Count);
    Igual(26m, resultado.Single(v => v.Vendedor == "Ana").Comissao);
    Igual(0m, resultado.Single(v => v.Vendedor == "Carlos").Comissao);
});
Testar("Venda negativa no agrupamento", () => Rejeita<ArgumentException>(() => Comissoes.Agrupar([new("Ana", -1)])));
var hoje = new DateOnly(2026, 10, 4);
Testar("Vencimento hoje", () => Igual(new ResultadoJuros(0, 0, 1000), Juros.Calcular(1000, hoje, hoje)));
Testar("Vencimento futuro", () => Igual(new ResultadoJuros(0, 0, 1000), Juros.Calcular(1000, hoje.AddDays(1), hoje)));
Testar("Dez dias de juros simples", () => Igual(new ResultadoJuros(10, 250, 1250), Juros.Calcular(1000, hoje.AddDays(-10), hoje)));
Testar("Arredondamento dos juros", () => Igual(0.03m, Juros.Calcular(1, hoje.AddDays(-1), hoje).Juros));
Testar("Valor negativo", () => Rejeita<ArgumentException>(() => Juros.Calcular(-1, hoje, hoje)));
Testar("Ano bissexto", () => Igual(2, Juros.Calcular(100, new(2024, 2, 28), new(2024, 3, 1)).DiasAtraso));
Testar("Um dia de atraso", () => Igual(new ResultadoJuros(1, 25, 1025), Juros.Calcular(1000, hoje.AddDays(-1), hoje)));
Testar("Valor zero com atraso", () => Igual(new ResultadoJuros(10, 0, 0), Juros.Calcular(0, hoje.AddDays(-10), hoje)));
Testar("Virada de ano", () => Igual(new ResultadoJuros(1, 2.50m, 102.50m), Juros.Calcular(100, new(2025, 12, 31), new(2026, 1, 1))));
Testar("Quarenta dias: juros simples", () => Igual(new ResultadoJuros(40, 1000, 2000), Juros.Calcular(1000, hoje.AddDays(-40), hoje)));
Testar("Overflow dos juros", () => Rejeita<OverflowException>(() => Juros.Calcular(decimal.MaxValue, hoje.AddDays(-100), hoje)));

string pasta = Path.Combine(Path.GetTempPath(), "desafio-target-tests-" + Guid.NewGuid());
Directory.CreateDirectory(pasta);
try
{
    string inicial = Path.Combine(AppContext.BaseDirectory, "Dados", "estoque.json");
    string estado = Path.Combine(pasta, "estado.json");
    var estoque = new ControleEstoque(inicial, estado);
    Testar("Carga inicial", () => Igual(150, estoque.Consultar().Produtos.Single(p => p.CodigoProduto == 101).Estoque));
    Testar("Todos os produtos do enunciado", () =>
    {
        var produtos = estoque.Consultar().Produtos;
        Igual(5, produtos.Count);
        foreach (var (codigo, descricao, saldo) in new[]
        {
            (101, "Caneta Azul", 150), (102, "Caderno Universitário", 75),
            (103, "Borracha Branca", 200), (104, "Lápis Preto HB", 320),
            (105, "Marcador de Texto Amarelo", 90)
        })
            Igual(new Produto(codigo, descricao, saldo), produtos.Single(p => p.CodigoProduto == codigo));
        Igual(0, estoque.Consultar().Movimentacoes.Count);
    });
    Testar("Entrada, saída, IDs e reabertura", () =>
    {
        var entrada = estoque.Movimentar(101, TipoMovimentacao.Entrada, 20, "Compra");
        var saida = estoque.Movimentar(101, TipoMovimentacao.Saida, 15, "Venda");
        Igual(1L, entrada.Id);
        Igual(2L, saida.Id);
        Igual(170, entrada.SaldoFinal);
        Igual(155, saida.SaldoFinal);
        if (entrada.Id == saida.Id || entrada.Id <= 0) throw new Exception("IDs inválidos.");
        var reaberto = new ControleEstoque(inicial, estado).Consultar();
        Igual(155, reaberto.Produtos.Single(p => p.CodigoProduto == 101).Estoque);
        Igual(2, reaberto.Movimentacoes.Count);
        Igual("Compra", reaberto.Movimentacoes[0].Descricao);
        Igual(75, reaberto.Produtos.Single(p => p.CodigoProduto == 102).Estoque);
    });
    foreach (var (nome, codigo, tipo, quantidade, descricao) in new[]
    {
        ("Saldo insuficiente", 101, TipoMovimentacao.Saida, 156, "Venda"),
        ("Produto inexistente", 999, TipoMovimentacao.Entrada, 1, "Compra"),
        ("Quantidade zero", 101, TipoMovimentacao.Entrada, 0, "Compra"),
        ("Quantidade negativa", 101, TipoMovimentacao.Entrada, -1, "Compra"),
        ("Descrição vazia", 101, TipoMovimentacao.Entrada, 1, " "),
        ("Tipo inválido", 101, (TipoMovimentacao)9, 1, "Compra")
    })
        Testar(nome + " não altera arquivo", () =>
        {
            string antes = File.ReadAllText(estado);
            Rejeita<ArgumentException>(() => estoque.Movimentar(codigo, tipo, quantidade, descricao));
            Igual(antes, File.ReadAllText(estado));
        });
    Testar("Saída do saldo exato", () => Igual(0, estoque.Movimentar(101, TipoMovimentacao.Saida, 155, "Venda").SaldoFinal));
    Testar("Reabertura continua IDs e preserva campos", () =>
    {
        var outraInstancia = new ControleEstoque(inicial, estado);
        var movimento = outraInstancia.Movimentar(103, TipoMovimentacao.Entrada, 7, " Devolução ");
        Igual(4L, movimento.Id);
        Igual(103, movimento.CodigoProduto);
        Igual(TipoMovimentacao.Entrada, movimento.Tipo);
        Igual(7, movimento.Quantidade);
        Igual("Devolução", movimento.Descricao);
        Igual(207, movimento.SaldoFinal);
        Igual(movimento, estoque.Consultar().Movimentacoes.Last());
    });
    Testar("Instâncias alternadas não perdem saldo ou IDs", () =>
    {
        var outraInstancia = new ControleEstoque(inicial, estado);
        Igual(5L, estoque.Movimentar(104, TipoMovimentacao.Entrada, 10, "Compra").Id);
        var saida = outraInstancia.Movimentar(104, TipoMovimentacao.Saida, 5, "Venda");
        Igual(6L, saida.Id);
        Igual(325, saida.SaldoFinal);
        var movimentos = estoque.Consultar().Movimentacoes;
        Igual(6, movimentos.Select(m => m.Id).Distinct().Count());
    });
    Testar("Saída de estoque zerado é rejeitada", () => Rejeita<ArgumentException>(() => estoque.Movimentar(101, TipoMovimentacao.Saida, 1, "Venda")));
    Testar("Overflow não altera arquivo", () =>
    {
        string antes = File.ReadAllText(estado);
        Rejeita<OverflowException>(() => estoque.Movimentar(102, TipoMovimentacao.Entrada, int.MaxValue, "Compra"));
        Igual(antes, File.ReadAllText(estado));
    });
    Testar("Bloqueio de escrita concorrente", () =>
    {
        using var bloqueio = new FileStream(estado + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Rejeita<IOException>(() => estoque.Movimentar(102, TipoMovimentacao.Entrada, 1, "Compra"));
    });
    Testar("Arquivo corrompido não é reiniciado", () =>
    {
        File.WriteAllText(estado, "inválido");
        Rejeita<System.Text.Json.JsonException>(() => new ControleEstoque(inicial, estado));
        Igual("inválido", File.ReadAllText(estado));
    });
}
finally { Directory.Delete(pasta, recursive: true); }
Console.WriteLine($"\n{total - falhas}/{total} testes passaram.");
return falhas == 0 ? 0 : 1;

void Testar(string nome, Action teste)
{
    total++;
    try { teste(); Console.WriteLine($"OK   {nome}"); }
    catch (Exception ex) { falhas++; Console.WriteLine($"FALHA {nome}: {ex.Message}"); }
}
void Igual<T>(T esperado, T obtido)
{
    if (!EqualityComparer<T>.Default.Equals(esperado, obtido))
        throw new Exception($"Esperado: {esperado}; obtido: {obtido}");
}
void Rejeita<T>(Action acao) where T : Exception
{
    try { acao(); }
    catch (T) { return; }
    throw new Exception($"Era esperada a exceção {typeof(T).Name}.");
}


