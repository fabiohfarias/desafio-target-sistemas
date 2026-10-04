namespace DesafioTarget;

public record Venda(string Vendedor, decimal Valor);
public record ArquivoVendas(List<Venda> Vendas);
public record ComissaoVendedor(string Vendedor, decimal TotalVendas, decimal Comissao);

public static class Comissoes
{
    public static decimal Calcular(decimal valor)
    {
        if (valor < 0) throw new ArgumentException("O valor da venda não pode ser negativo.");
        decimal taxa = valor < 100m ? 0m : valor < 500m ? 0.01m : 0.05m;
        return decimal.Round(valor * taxa, 2, MidpointRounding.AwayFromZero);
    }

    public static List<ComissaoVendedor> Agrupar(IEnumerable<Venda> vendas)
    {
        var lista = vendas.ToList();
        if (lista.Any(v => v is null || string.IsNullOrWhiteSpace(v.Vendedor)))
            throw new ArgumentException("Toda venda deve informar um vendedor.");

        return lista.GroupBy(v => v.Vendedor.Trim())
            .Select(g => new ComissaoVendedor(g.Key, g.Sum(v => v.Valor), g.Sum(v => Calcular(v.Valor))))
            .OrderBy(v => v.Vendedor).ToList();
    }
}
