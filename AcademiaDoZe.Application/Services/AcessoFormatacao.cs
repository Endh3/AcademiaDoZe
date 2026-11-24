namespace AcademiaDoZe.Application.Services;

internal static class AcessoFormatacao
{
    // Formata como hh:mm:ss (horas podem passar de 99 sem quebrar o formato)
    public static string Tempo(TimeSpan tempo)
    {
        if (tempo < TimeSpan.Zero) tempo = TimeSpan.Zero;
        return $"{(int)tempo.TotalHours:00}:{tempo.Minutes:00}:{tempo.Seconds:00}";
    }
    public static DateTime TruncarSegundos(DateTime dataHora) => new(dataHora.Ticks - (dataHora.Ticks % TimeSpan.TicksPerSecond), dataHora.Kind);
    public const string MensagemForaDoHorario = "Registro fora do horário de funcionamento (06:00 às 22:00).";
}
