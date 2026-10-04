namespace DesafioTarget;

public record ResultadoJuros(int DiasAtraso, decimal Juros, decimal Total);

public static class Juros
{
    public static ResultadoJuros Calcular(decimal valor, DateOnly vencimento, DateOnly hoje)
    {
        if (valor < 0) throw new ArgumentException("O valor não pode ser negativo.");
        int dias = Math.Max(0, hoje.DayNumber - vencimento.DayNumber);
        decimal juros = decimal.Round(valor * 0.025m * dias, 2, MidpointRounding.AwayFromZero);
        return new ResultadoJuros(dias, juros, valor + juros);
    }
}
