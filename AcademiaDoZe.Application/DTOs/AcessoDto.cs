using AcademiaDoZe.Application.Enums;
namespace AcademiaDoZe.Application.DTOs;

/// <summary>
/// Resultado de uma tentativa de registro de entrada/saída (aluno ou colaborador).
/// </summary>
public class AcessoResultadoDto
{
    public bool Autorizado { get; set; }
    public AppAcessoEvento Evento { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataHora { get; set; }
    // Aluno, na entrada: dias que ainda restam no plano
    public int? DiasRestantesPlano { get; set; }
    // Aluno, na saída: tempo que permaneceu na academia
    public TimeSpan? TempoPermanencia { get; set; }
    // Colaborador: soma de todos os registros do dia
    public TimeSpan? HorasTrabalhadasHoje { get; set; }
}

/// <summary>
/// Linha da listagem de acessos registrados no dia.
/// </summary>
public class AcessoRegistroDto
{
    public int Id { get; set; }
    public int PessoaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Colaborador { get; set; }
    public AppAcessoEvento Evento { get; set; }
    public DateTime DataHora { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string HoraTexto => DataHora.ToString("HH:mm:ss");
}
