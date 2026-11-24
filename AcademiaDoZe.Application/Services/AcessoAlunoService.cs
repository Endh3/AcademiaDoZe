using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Enums;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
namespace AcademiaDoZe.Application.Services;

public class AcessoAlunoService : IAcessoAlunoService
{
    private readonly Func<IAcessoAlunoRepository> _acessoRepoFactory;
    private readonly Func<IAlunoRepository> _alunoRepoFactory;
    private readonly Func<IMatriculaRepository> _matriculaRepoFactory;
    public AcessoAlunoService(Func<IAcessoAlunoRepository> acessoRepoFactory, Func<IAlunoRepository> alunoRepoFactory, Func<IMatriculaRepository> matriculaRepoFactory)
    {
        _acessoRepoFactory = acessoRepoFactory ?? throw new ArgumentNullException(nameof(acessoRepoFactory));
        _alunoRepoFactory = alunoRepoFactory ?? throw new ArgumentNullException(nameof(alunoRepoFactory));
        _matriculaRepoFactory = matriculaRepoFactory ?? throw new ArgumentNullException(nameof(matriculaRepoFactory));
    }
    public async Task<bool> EstaNaAcademiaAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        return await _acessoRepoFactory().EstaNaAcademia(alunoId, cancellationToken);
    }
    public async Task<AcessoResultadoDto> RegistrarAcessoAsync(int alunoId, CancellationToken cancellationToken = default)
    {
        var aluno = await _alunoRepoFactory().ObterPorId(alunoId, cancellationToken)
        ?? throw new InvalidOperationException($"Aluno com ID {alunoId} não encontrado.");
        var acessoRepo = _acessoRepoFactory();
        var agora = AcessoFormatacao.TruncarSegundos(DateTime.Now);
        bool estaDentro = await acessoRepo.EstaNaAcademia(alunoId, cancellationToken);
        if (!estaDentro)
        {
            // ENTRADA: valida se possui matrícula ativa
            var matricula = await _matriculaRepoFactory().ObterMatriculaAtivaPorAluno(alunoId, cancellationToken);
            if (matricula == null)
            {
                return Negado(aluno, AppAcessoEvento.Entrada, agora, "Acesso negado: o aluno não possui matrícula ativa.");
            }
            var criacao = AcessoAluno.Criar(0, aluno, agora);
            if (criacao.IsFailure)
            {
                return Negado(aluno, AppAcessoEvento.Entrada, agora, $"Acesso negado: {AcessoFormatacao.MensagemForaDoHorario}");
            }
            await acessoRepo.Adicionar(criacao.Value!, cancellationToken);
            int diasRestantes = Math.Max(0, matricula.DataFim.DayNumber - DateOnly.FromDateTime(agora).DayNumber);
            return new AcessoResultadoDto
            {
                Autorizado = true,
                Evento = AppAcessoEvento.Entrada,
                Titulo = "Entrada Autorizada",
                Mensagem = $"Entrada autorizada. Dias restantes de plano: {diasRestantes} dia(s).",
                PessoaId = aluno.Id,
                Nome = aluno.Nome,
                DataHora = agora,
                DiasRestantesPlano = diasRestantes
            };
        }
        // SAÍDA: calcula o tempo desde a última entrada
        var ultimo = await acessoRepo.ObterUltimoAcesso(alunoId, cancellationToken);
        var saida = AcessoAluno.Criar(0, aluno, agora);
        if (saida.IsFailure || ultimo == null)
        {
            return Negado(aluno, AppAcessoEvento.Saida, agora, $"Saída não registrada: {AcessoFormatacao.MensagemForaDoHorario}");
        }
        await acessoRepo.Adicionar(saida.Value!, cancellationToken);
        var permanencia = agora - ultimo.DataHora;
        return new AcessoResultadoDto
        {
            Autorizado = true,
            Evento = AppAcessoEvento.Saida,
            Titulo = "Saída Registrada",
            Mensagem = $"Saída registrada. Tempo de permanência: {AcessoFormatacao.Tempo(permanencia)}.",
            PessoaId = aluno.Id,
            Nome = aluno.Nome,
            DataHora = agora,
            TempoPermanencia = permanencia
        };
    }
    public async Task<IEnumerable<AcessoRegistroDto>> ObterAcessosDoDiaAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var acessos = (await _acessoRepoFactory().ObterAcessosPorAlunoPeriodo(null, hoje, hoje, cancellationToken)).ToList();
        if (acessos.Count == 0) return [];
        var alunoRepo = _alunoRepoFactory();
        var matriculaRepo = _matriculaRepoFactory();
        var registros = new List<AcessoRegistroDto>();
        foreach (var grupo in acessos.GroupBy(a => a.AlunoId))
        {
            var aluno = await alunoRepo.ObterPorId(grupo.Key, cancellationToken);
            if (aluno == null) continue;
            var matricula = await matriculaRepo.ObterMatriculaAtivaPorAluno(grupo.Key, cancellationToken);
            var ordenados = grupo.OrderBy(a => a.DataHora).ThenBy(a => a.Id).ToList();
            for (int i = 0; i < ordenados.Count; i++)
            {
                var acesso = ordenados[i];
                bool entrada = i % 2 == 0;
                string descricao;
                if (entrada)
                {
                    descricao = matricula != null
                    ? $"Entrada autorizada. Dias restantes de plano: {Math.Max(0, matricula.DataFim.DayNumber - hoje.DayNumber)} dia(s)."
                    : "Entrada autorizada.";
                }
                else
                {
                    descricao = $"Saída registrada. Tempo de permanência: {AcessoFormatacao.Tempo(acesso.DataHora - ordenados[i - 1].DataHora)}.";
                }
                registros.Add(new AcessoRegistroDto
                {
                    Id = acesso.Id,
                    PessoaId = aluno.Id,
                    Nome = aluno.Nome,
                    Colaborador = false,
                    Evento = entrada ? AppAcessoEvento.Entrada : AppAcessoEvento.Saida,
                    DataHora = acesso.DataHora,
                    Descricao = descricao
                });
            }
        }
        return [.. registros.OrderByDescending(r => r.DataHora).ThenByDescending(r => r.Id)];
    }
    private static AcessoResultadoDto Negado(Aluno aluno, AppAcessoEvento evento, DateTime dataHora, string mensagem) => new()
    {
        Autorizado = false,
        Evento = evento,
        Titulo = "Acesso Negado",
        Mensagem = mensagem,
        PessoaId = aluno.Id,
        Nome = aluno.Nome,
        DataHora = dataHora
    };
}
