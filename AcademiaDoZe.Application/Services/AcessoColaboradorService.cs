using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.Repositories;
namespace AcademiaDoZe.Application.Services;

public class AcessoColaboradorService : IAcessoColaboradorService
{
    private readonly Func<IAcessoColaboradorRepository> _acessoRepoFactory;
    private readonly Func<IColaboradorRepository> _colaboradorRepoFactory;
    public AcessoColaboradorService(Func<IAcessoColaboradorRepository> acessoRepoFactory, Func<IColaboradorRepository> colaboradorRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _colaboradorRepoFactory = colaboradorRepoFactory ?? throw new ArgumentNullException(nameof(colaboradorRepoFactory));
    }
    // Limite diário de permanência: 8 horas se for CLT, 6 horas se for estágio
    public static TimeSpan ObterLimiteDiario(ColaboradorVinculo vinculo) => vinculo == ColaboradorVinculo.CLT ? TimeSpan.FromHours(8) : TimeSpan.FromHours(6);
    private static string NomeVinculo(ColaboradorVinculo vinculo) => vinculo == ColaboradorVinculo.CLT ? "CLT" : "estágio";
    public async Task<bool> EstaNaAcademiaAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var acessosHoje = await _acessoRepoFactory().ObterAcessosPorColaboradorPeriodo(colaboradorId, hoje, hoje, cancellationToken);
        return acessosHoje.Count() % 2 == 1;
    }
    public async Task<AcessoResultadoDto> RegistrarAcessoAsync(int colaboradorId, CancellationToken cancellationToken = default)
    {
        var colaborador = await _colaboradorRepoFactory().ObterPorId(colaboradorId, cancellationToken)
        ?? throw new InvalidOperationException($"Colaborador com ID {colaboradorId} não encontrado.");
        var acessoRepo = _acessoRepoFactory();
        var agora = AcessoFormatacao.TruncarSegundos(DateTime.Now);
        var hoje = DateOnly.FromDateTime(agora);
        bool estaDentro = await EstaNaAcademiaAsync(colaboradorId, cancellationToken);
        var limite = ObterLimiteDiario(colaborador.Vinculo);
        if (!estaDentro)
        {
            // ENTRADA: valida se já não atingiu o limite diário
            var trabalhadas = await acessoRepo.ObterHorasTrabalhadasNoDia(colaboradorId, hoje, cancellationToken);
            if (trabalhadas >= limite)
            {
                return Negado(colaborador, AppAcessoEvento.Entrada, agora, $"Acesso negado: o colaborador ({NomeVinculo(colaborador.Vinculo)}) já atingiu o limite diário de {limite.TotalHours:0} horas. Horas trabalhadas hoje: {AcessoFormatacao.Tempo(trabalhadas)}.", trabalhadas);
            }
            var criacao = AcessoColaborador.Criar(0, colaborador, agora);
            if (criacao.IsFailure)
            {
                return Negado(colaborador, AppAcessoEvento.Entrada, agora, $"Acesso negado: {AcessoFormatacao.MensagemForaDoHorario}", trabalhadas);
            }
            await acessoRepo.Adicionar(criacao.Value!, cancellationToken);
            return new AcessoResultadoDto
            {
                Autorizado = true,
                Evento = AppAcessoEvento.Entrada,
                Titulo = "Entrada Registrada",
                Mensagem = $"Entrada registrada. Horas trabalhadas hoje até o momento: {AcessoFormatacao.Tempo(trabalhadas)}.",
                PessoaId = colaborador.Id,
                Nome = colaborador.Nome,
                DataHora = agora,
                HorasTrabalhadasHoje = trabalhadas
            };
        }
        // SAÍDA: soma todos os registros do dia
        var saida = AcessoColaborador.Criar(0, colaborador, agora);
        if (saida.IsFailure)
        {
            return Negado(colaborador, AppAcessoEvento.Saida, agora, $"Saída não registrada: {AcessoFormatacao.MensagemForaDoHorario}", null);
        }
        await acessoRepo.Adicionar(saida.Value!, cancellationToken);
        var total = await acessoRepo.ObterHorasTrabalhadasNoDia(colaboradorId, hoje, cancellationToken);
        string mensagem = $"Saída registrada. Tempo de permanência hoje (soma dos registros do dia): {AcessoFormatacao.Tempo(total)}.";
        if (total > limite)
        {
            mensagem += $" Atenção: limite diário de {limite.TotalHours:0} horas ({NomeVinculo(colaborador.Vinculo)}) ultrapassado.";
        }
        return new AcessoResultadoDto
        {
            Autorizado = true,
            Evento = AppAcessoEvento.Saida,
            Titulo = "Saída Registrada",
            Mensagem = mensagem,
            PessoaId = colaborador.Id,
            Nome = colaborador.Nome,
            DataHora = agora,
            HorasTrabalhadasHoje = total
        };
    }
    public async Task<IEnumerable<AcessoRegistroDto>> ObterAcessosDoDiaAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var acessos = (await _acessoRepoFactory().ObterAcessosPorColaboradorPeriodo(null, hoje, hoje, cancellationToken)).ToList();
        if (acessos.Count == 0) return [];
        var colaboradorRepo = _colaboradorRepoFactory();
        var registros = new List<AcessoRegistroDto>();
        foreach (var grupo in acessos.GroupBy(a => a.ColaboradorId))
        {
            var colaborador = await colaboradorRepo.ObterPorId(grupo.Key, cancellationToken);
            if (colaborador == null) continue;
            var ordenados = grupo.OrderBy(a => a.DataHora).ThenBy(a => a.Id).ToList();
            var trabalhado = TimeSpan.Zero; // soma dos pares já fechados
            for (int i = 0; i < ordenados.Count; i++)
            {
                var acesso = ordenados[i];
                bool entrada = i % 2 == 0;
                string descricao;
                if (entrada)
                {
                    descricao = $"Entrada registrada. Horas trabalhadas hoje até o momento: {AcessoFormatacao.Tempo(trabalhado)}.";
                }
                else
                {
                    trabalhado += acesso.DataHora - ordenados[i - 1].DataHora;
                    descricao = $"Saída registrada. Tempo de permanência hoje (soma dos registros do dia): {AcessoFormatacao.Tempo(trabalhado)}.";
                }
                registros.Add(new AcessoRegistroDto
                {
                    Id = acesso.Id,
                    PessoaId = colaborador.Id,
                    Nome = colaborador.Nome,
                    Colaborador = true,
                    Evento = entrada ? AppAcessoEvento.Entrada : AppAcessoEvento.Saida,
                    DataHora = acesso.DataHora,
                    Descricao = descricao
                });
            }
        }
        return [.. registros.OrderByDescending(r => r.DataHora).ThenByDescending(r => r.Id)];
    }
    private static AcessoResultadoDto Negado(Colaborador colaborador, AppAcessoEvento evento, DateTime dataHora, string mensagem, TimeSpan? horas) => new()
    {
        Autorizado = false,
        Evento = evento,
        Titulo = "Acesso Negado",
        Mensagem = mensagem,
        PessoaId = colaborador.Id,
        Nome = colaborador.Nome,
        DataHora = dataHora,
        HorasTrabalhadasHoje = horas
    };
}
