namespace AcademiaDoZe.Infrastructure.Repositories;

// Regras de pareamento dos registros de acesso.
// Cada pessoa possui registros alternados no dia: 1º = entrada, 2º = saída, 3º = entrada, 4º = saída...
internal static class AcessoCalculo
{
    // Remove frações de segundo para manter a comparação consistente entre os SGDBs.
    public static DateTime TruncarSegundos(DateTime dataHora) => new(dataHora.Ticks - (dataHora.Ticks % TimeSpan.TicksPerSecond), dataHora.Kind);

    // Soma o tempo dos pares entrada/saída. Se houver uma entrada em aberto, soma até "agora" quando informado.
    public static TimeSpan SomarPermanencia(IEnumerable<DateTime> registrosOrdenados, DateTime? agora = null)
    {
        var lista = registrosOrdenados.ToList();
        var total = TimeSpan.Zero;
        for (int i = 0; i + 1 < lista.Count; i += 2)
        {
            total += lista[i + 1] - lista[i];
        }
        if (agora.HasValue && lista.Count % 2 == 1)
        {
            var aberto = agora.Value - lista[^1];
            if (aberto > TimeSpan.Zero) total += aberto;
        }
        return total;
    }

    // Retorna apenas os registros que são entradas (posições 0, 2, 4...).
    public static IEnumerable<DateTime> ObterEntradas(IEnumerable<DateTime> registrosOrdenados) => registrosOrdenados.Where((_, i) => i % 2 == 0);
}
