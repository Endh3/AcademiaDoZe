using AcademiaDoZe.Application.DTOs;
namespace AcademiaDoZe.Application.Interfaces;
/// <summary>
/// Contrato de serviço para o controle de entrada/saída (catraca) de colaboradores.
/// </summary>
public interface IAcessoColaboradorService
{
    /// <summary>
    /// Registra a próxima movimentação do colaborador: entrada (valida o limite diário de 8h para CLT e 6h para estágio)
    /// ou saída (informa o tempo somado de todos os registros do dia).
    /// </summary>
    Task<AcessoResultadoDto> RegistrarAcessoAsync(int colaboradorId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Indica se o colaborador está atualmente dentro da academia (entrada sem saída correspondente hoje).
    /// </summary>
    Task<bool> EstaNaAcademiaAsync(int colaboradorId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtém os acessos de colaboradores registrados hoje, do mais recente para o mais antigo.
    /// </summary>
    Task<IEnumerable<AcessoRegistroDto>> ObterAcessosDoDiaAsync(CancellationToken cancellationToken = default);
}
