using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesafioTarget;

public record Produto(int CodigoProduto, string DescricaoProduto, int Estoque);
public record ArquivoEstoque(List<Produto> Estoque);
public enum TipoMovimentacao { Entrada, Saida }
public record Movimentacao(long Id, DateTimeOffset Data, int CodigoProduto,
    TipoMovimentacao Tipo, string Descricao, int Quantidade, int SaldoFinal);
public record EstadoEstoque(List<Produto> Produtos, List<Movimentacao> Movimentacoes);

public static class ArquivosJson
{
    public static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        RespectRequiredConstructorParameters = true
    };

    public static T Ler<T>(string caminho) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(caminho), Opcoes)
        ?? throw new InvalidDataException($"Arquivo vazio ou inválido: {caminho}");
}

public sealed class ControleEstoque
{
    private readonly string caminho;
    public ControleEstoque(string arquivoInicial, string arquivoEstado)
    {
        caminho = Path.GetFullPath(arquivoEstado);
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        if (!File.Exists(caminho))
        {
            var inicial = ArquivosJson.Ler<ArquivoEstoque>(arquivoInicial);
            Validar(new EstadoEstoque(inicial.Estoque, []));
            using var arquivo = new FileStream(caminho, FileMode.CreateNew, FileAccess.Write);
            JsonSerializer.Serialize(arquivo, new EstadoEstoque(inicial.Estoque, []), ArquivosJson.Opcoes);
        }
        Consultar();
    }

    public EstadoEstoque Consultar()
    {
        var estado = ArquivosJson.Ler<EstadoEstoque>(caminho);
        Validar(estado);
        return estado;
    }

    public Movimentacao Movimentar(int codigo, TipoMovimentacao tipo, int quantidade, string descricao)
    {
        if (!Enum.IsDefined(tipo)) throw new ArgumentException("Tipo de movimentação inválido.");
        if (quantidade <= 0) throw new ArgumentException("A quantidade deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Informe a descrição da movimentação.");

        // O bloqueio impede duas instâncias de sobrescreverem o saldo uma da outra.
        using var bloqueio = new FileStream(caminho + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var estado = Consultar();
        int indice = estado.Produtos.FindIndex(p => p.CodigoProduto == codigo);
        if (indice < 0) throw new ArgumentException("Produto não encontrado.");
        var produto = estado.Produtos[indice];
        if (tipo == TipoMovimentacao.Saida && quantidade > produto.Estoque)
            throw new ArgumentException("Estoque insuficiente para esta saída.");

        int saldo = checked(produto.Estoque + (tipo == TipoMovimentacao.Entrada ? quantidade : -quantidade));
        long id = checked(estado.Movimentacoes.Select(m => m.Id).DefaultIfEmpty(0).Max() + 1);
        var movimento = new Movimentacao(id, DateTimeOffset.Now, codigo, tipo, descricao.Trim(), quantidade, saldo);
        estado.Produtos[indice] = produto with { Estoque = saldo };
        estado.Movimentacoes.Add(movimento);

        // Saldo e histórico são gravados juntos; a troca só ocorre após a escrita completa.
        string temporario = caminho + ".tmp";
        try
        {
            File.WriteAllText(temporario, JsonSerializer.Serialize(estado, ArquivosJson.Opcoes));
            File.Move(temporario, caminho, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporario)) File.Delete(temporario);
        }
        return movimento;
    }

    private static void Validar(EstadoEstoque estado)
    {
        if (estado.Produtos is null || estado.Movimentacoes is null ||
            estado.Produtos.Any(p => p is null || p.CodigoProduto <= 0 || p.Estoque < 0 || string.IsNullOrWhiteSpace(p.DescricaoProduto)) ||
            estado.Produtos.Select(p => p.CodigoProduto).Distinct().Count() != estado.Produtos.Count)
            throw new InvalidDataException("Dados de estoque inválidos. Verifique os produtos e seus códigos.");
    }
}

