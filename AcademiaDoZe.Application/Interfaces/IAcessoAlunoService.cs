using AcademiaDoZe.Application.DTOs;
namespace AcademiaDoZe.Application.Interfaces;
/// <summary>
/// Contrato de serviço para o controle de entrada/saída (catraca) de alunos.
/// </summary>
public interface IAcessoAlunoService
{
    /// <summary>
    /// Registra a próxima movimentação do aluno: entrada (valida matrícula ativa e informa os dias restantes do plano)
    /// ou saída (informa o tempo de permanência na academia).
    /// </summary>
    Task<AcessoResultadoDto> RegistrarAcessoAsync(int alunoId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Indica se o aluno está atualmente dentro da academia (entrada sem saída correspondente hoje).
    /// </summary>
    Task<bool> EstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém os acessos de alunos registrados hoje, do mais recente para o mais antigo.
    /// </summary>
    Task<IEnumerable<AcessoRegistroDto>> ObterAcessosDoDiaAsync(CancellationToken cancellationToken = default);
}
