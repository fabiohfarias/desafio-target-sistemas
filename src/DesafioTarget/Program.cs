using System.Globalization;
using System.Text;
using System.Text.Json;
using DesafioTarget;

Console.OutputEncoding = Encoding.UTF8;
var cultura = CultureInfo.GetCultureInfo("pt-BR");
CultureInfo.CurrentCulture = cultura;
string dados = Path.Combine(AppContext.BaseDirectory, "Dados");
string estado = Path.Combine(Environment.CurrentDirectory, "dados-locais", "estoque.json");

try
{
    while (true)
    {
        Console.WriteLine("\nDESAFIO TARGET\n1 - Comissões\n2 - Consultar estoque\n3 - Movimentar estoque\n4 - Histórico\n5 - Juros por atraso\n0 - Sair");
        string opcao = Ler("Opção: ");
        if (opcao == "0") break;
        try
        {
            switch (opcao)
            {
                case "1":
                    var arquivo = ArquivosJson.Ler<ArquivoVendas>(Path.Combine(dados, "vendas.json"));
                    if (arquivo.Vendas is null) throw new InvalidDataException("A lista de vendas não foi informada.");
                    var comissoes = Comissoes.Agrupar(arquivo.Vendas);
                    Console.WriteLine($"{ "Vendedor",-22} {"Vendas",15} {"Comissão",15}");
                    foreach (var item in comissoes)
                        Console.WriteLine($"{item.Vendedor,-22} {item.TotalVendas,15:C2} {item.Comissao,15:C2}");
                    Console.WriteLine($"Total de comissões: {comissoes.Sum(c => c.Comissao):C2}");
                    break;
                case "2":
                    foreach (var produto in Estoque().Consultar().Produtos)
                        Console.WriteLine($"{produto.CodigoProduto} - {produto.DescricaoProduto}: {produto.Estoque} unidades");
                    break;
                case "3":
                    int codigo = Inteiro("Código do produto: ");
                    string tipo = Ler("Tipo (E = entrada, S = saída): ").ToUpperInvariant();
                    if (tipo != "E" && tipo != "S") throw new ArgumentException("Informe E ou S.");
                    int quantidade = Inteiro("Quantidade: ");
                    string descricao = Ler("Descrição (ex.: compra, venda, devolução): ");
                    var movimento = Estoque().Movimentar(codigo,
                        tipo == "E" ? TipoMovimentacao.Entrada : TipoMovimentacao.Saida, quantidade, descricao);
                    Console.WriteLine($"Movimentação registrada: {movimento.Id}\nEstoque final: {movimento.SaldoFinal} unidades");
                    break;
                case "4":
                    var historico = Estoque().Consultar().Movimentacoes;
                    if (historico.Count == 0) Console.WriteLine("Nenhuma movimentação registrada.");
                    foreach (var m in historico)
                        Console.WriteLine($"{m.Id} | {m.Data:dd/MM/yyyy HH:mm:ss zzz} | Produto {m.CodigoProduto} | {m.Tipo} | {m.Descricao} | Qtde {m.Quantidade} | Saldo {m.SaldoFinal}");
                    break;
                case "5":
                    string entrada = Ler("Valor em reais (ex.: 1000,50, sem separador de milhar): ");
                    if (!decimal.TryParse(entrada, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, cultura, out decimal valor))
                        throw new ArgumentException("Informe um valor válido, usando vírgula para os centavos.");
                    if (!DateOnly.TryParseExact(Ler("Vencimento (dd/MM/aaaa): "), "dd/MM/yyyy", cultura, DateTimeStyles.None, out var vencimento))
                        throw new ArgumentException("Informe uma data válida no formato dd/MM/aaaa.");
                    var hoje = DateOnly.FromDateTime(DateTime.Today);
                    var resultado = Juros.Calcular(valor, vencimento, hoje);
                    Console.WriteLine($"Data do cálculo: {hoje:dd/MM/yyyy}\nDias em atraso: {resultado.DiasAtraso}\nJuros: {resultado.Juros:C2}\nTotal: {resultado.Total:C2}");
                    break;
                default:
                    Console.WriteLine("Opção inválida.");
                    break;
            }
        }
        catch (EndOfStreamException) { throw; }
        catch (Exception ex) when (ex is ArgumentException or IOException or JsonException or UnauthorizedAccessException or OverflowException)
        {
            Console.WriteLine($"Não foi possível concluir: {ex.Message}");
        }
    }
}
catch (EndOfStreamException) { Console.WriteLine("\nEntrada encerrada."); }

ControleEstoque Estoque() => new(Path.Combine(dados, "estoque.json"), estado);
string Ler(string mensagem)
{
    Console.Write(mensagem);
    return Console.ReadLine()?.Trim() ?? throw new EndOfStreamException();
}
int Inteiro(string mensagem) => int.TryParse(Ler(mensagem), out int valor)
    ? valor : throw new ArgumentException("Informe um número inteiro válido.");

