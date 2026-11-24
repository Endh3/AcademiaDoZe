using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Repositories;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using System.Data;
using System.Data.Common;
namespace AcademiaDoZe.Infrastructure.Repositories;

public class AcessoAlunoRepository : BaseRepository, IAcessoAlunoRepository
{
    private const int PessoaTipoAluno = 0; // tb_acesso.pessoa_tipo: 0 = Aluno
    public AcessoAlunoRepository(string connectionString, DatabaseType databaseType) : base(connectionString, databaseType)
    {
    }
    private static string BaseSelectQuery => """
SELECT
ac.id_acesso, ac.pessoa_id, ac.data_hora,
a.id_aluno, a.cpf, a.nome AS aluno_nome, a.nascimento, a.telefone, a.email, a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_acesso ac
INNER JOIN tb_aluno a ON ac.pessoa_id = a.id_aluno
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
""";
    private static string BaseAlunoSelectQuery => """
SELECT
a.id_aluno, a.cpf, a.nome, a.nascimento, a.telefone, a.email,
a.logradouro_id, a.numero, a.complemento, a.senha, a.foto,
l.id_logradouro, l.cep, l.nome AS logradouro_nome, l.bairro, l.cidade, l.estado, l.pais
FROM tb_aluno a
INNER JOIN tb_logradouro l ON a.logradouro_id = l.id_logradouro
""";
    public static AcessoAluno Map(DbDataReader reader)
    {
        try
        {
            int id = reader.GetInt32Value("id_acesso");
            DateTime dataHora = reader.GetDateTimeValue("data_hora");
            var aluno = AlunoRepository.Map(reader, "aluno_nome");
            var result = AcessoAluno.Criar(id, aluno, dataHora);
            if (result.IsFailure)
            {
                throw new InfrastructureException("ERRO_DOMINIO_MAPEAMENTO", $"Erro de domínio ao mapear acesso de aluno ID {id}: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
            }
            return result.Value!;
        }
        catch (Exception ex) when (ex is not InfrastructureException)
        {
            throw new InfrastructureException("ERRO_MAPEAMENTO_ACESSO_ALUNO", $"Erro ao mapear dados do acesso do aluno: {ex.Message}", ex);
        }
    }
    public async Task<AcessoAluno?> ObterPorId(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE ac.pessoa_tipo = {PessoaTipoAluno} AND ac.id_acesso = @Id";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_POR_ID", $"Erro ao obter acesso de aluno por ID {id}: {ex.Message}", ex);
        }
    }
    public async Task<IEnumerable<AcessoAluno>> ObterTodos(CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE ac.pessoa_tipo = {PessoaTipoAluno} ORDER BY ac.data_hora DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<AcessoAluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }
            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_TODOS", $"Erro ao obter todos os acessos de alunos: {ex.Message}", ex);
        }
    }
    public async Task<AcessoAluno> Adicionar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = FormatInsertQuery($"INSERT INTO tb_acesso (pessoa_tipo, pessoa_id, data_hora) VALUES ({PessoaTipoAluno}, @AlunoId, @DataHora)");
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@AlunoId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", AcessoCalculo.TruncarSegundos(entity.DataHora), DbType.DateTime);
            int id = await command.ExecuteScalarIdAsync("ERRO_ADICIONAR_ACESSO_ALUNO", "Falha ao obter ID inserido para o acesso do aluno.", cancellationToken);
            var idProperty = typeof(Entity).GetProperty("Id");
            idProperty?.SetValue(entity, id);
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ADICIONAR_ACESSO_ALUNO", $"Erro ao adicionar acesso para o aluno ID {entity.AlunoId}: {ex.Message}", ex);
        }
    }
    public async Task<AcessoAluno> Atualizar(AcessoAluno entity, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"UPDATE tb_acesso SET pessoa_id = @AlunoId, data_hora = @DataHora WHERE pessoa_tipo = {PessoaTipoAluno} AND id_acesso = @Id";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", entity.Id, DbType.Int32);
            command.AddParameter("@AlunoId", entity.AlunoId, DbType.Int32);
            command.AddParameter("@DataHora", AcessoCalculo.TruncarSegundos(entity.DataHora), DbType.DateTime);
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
            {
                throw new InfrastructureException("REGISTRO_NAO_ENCONTRADO", $"Nenhum acesso de aluno encontrado com o ID {entity.Id} para atualização.");
            }
            return entity;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_ATUALIZAR_ACESSO_ALUNO", $"Erro ao atualizar acesso ID {entity.Id}: {ex.Message}", ex);
        }
    }
    public async Task<bool> Remover(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"DELETE FROM tb_acesso WHERE pessoa_tipo = {PessoaTipoAluno} AND id_acesso = @Id";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Id", id, DbType.Int32);
            var result = await command.ExecuteNonQueryAsync(cancellationToken);
            return result > 0;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_REMOVER_ACESSO_ALUNO", $"Erro ao remover acesso ID {id}: {ex.Message}", ex);
        }
    }
    public async Task<IEnumerable<AcessoAluno>> ObterAcessosPorAlunoPeriodo(int? alunoId = null, DateOnly? inicio = null, DateOnly? fim = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var filtros = new List<string> { $"ac.pessoa_tipo = {PessoaTipoAluno}" };
            if (alunoId.HasValue) filtros.Add("ac.pessoa_id = @AlunoId");
            if (inicio.HasValue) filtros.Add("ac.data_hora >= @Inicio");
            if (fim.HasValue) filtros.Add("ac.data_hora < @FimExclusivo");
            string query = $"{BaseSelectQuery} WHERE {string.Join(" AND ", filtros)} ORDER BY ac.data_hora ASC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            if (alunoId.HasValue) command.AddParameter("@AlunoId", alunoId.Value, DbType.Int32);
            if (inicio.HasValue) command.AddParameter("@Inicio", inicio.Value.ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            if (fim.HasValue) command.AddParameter("@FimExclusivo", fim.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DbType.DateTime);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var acessos = new List<AcessoAluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                acessos.Add(Map(reader));
            }
            return acessos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ACESSOS_PERIODO", $"Erro ao obter acessos de alunos por período: {ex.Message}", ex);
        }
    }
    public async Task<AcessoAluno?> ObterUltimoAcesso(int alunoId, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseSelectQuery} WHERE ac.pessoa_tipo = {PessoaTipoAluno} AND ac.pessoa_id = @AlunoId ORDER BY ac.data_hora DESC, ac.id_acesso DESC";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@AlunoId", alunoId, DbType.Int32);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ULTIMO_ACESSO", $"Erro ao obter último acesso do aluno ID {alunoId}: {ex.Message}", ex);
        }
    }
    // O aluno está na academia quando possui uma quantidade ímpar de registros hoje (entrada sem a saída correspondente).
    // Acessos de dias anteriores não contam: a cada dia o ciclo entrada/saída recomeça.
    public async Task<bool> EstaNaAcademia(int alunoId, CancellationToken cancellationToken = default)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        var acessosHoje = await ObterAcessosPorAlunoPeriodo(alunoId, hoje, hoje, cancellationToken);
        return acessosHoje.Count() % 2 == 1;
    }
    public async Task<Dictionary<TimeOnly, int>> ObterHorarioMaisProcuradoPorMes(int mes, CancellationToken cancellationToken = default)
    {
        var acessosDoMes = (await ObterAcessosPorAlunoPeriodo(null, null, null, cancellationToken)).Where(a => a.DataHora.Month == mes);
        var resultado = new Dictionary<TimeOnly, int>();
        // considera somente as entradas (1º, 3º, 5º... registro de cada aluno em cada dia)
        foreach (var grupo in acessosDoMes.GroupBy(a => (a.AlunoId, Dia: a.DataHora.Date)))
        {
            foreach (var entrada in AcessoCalculo.ObterEntradas(grupo.OrderBy(a => a.DataHora).Select(a => a.DataHora)))
            {
                var hora = new TimeOnly(entrada.Hour, 0);
                resultado[hora] = resultado.GetValueOrDefault(hora) + 1;
            }
        }
        return resultado.OrderByDescending(x => x.Value).ThenBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
    }
    public async Task<Dictionary<int, TimeSpan>> ObterPermanenciaMediaPorMes(int mes, CancellationToken cancellationToken = default)
    {
        var acessosDoMes = (await ObterAcessosPorAlunoPeriodo(null, null, null, cancellationToken)).Where(a => a.DataHora.Month == mes);
        var permanencias = new List<TimeSpan>();
        foreach (var grupo in acessosDoMes.GroupBy(a => (a.AlunoId, Dia: a.DataHora.Date)))
        {
            var registros = grupo.OrderBy(a => a.DataHora).Select(a => a.DataHora).ToList();
            for (int i = 0; i + 1 < registros.Count; i += 2)
            {
                permanencias.Add(registros[i + 1] - registros[i]);
            }
        }
        if (permanencias.Count == 0) return [];
        var media = TimeSpan.FromTicks((long)permanencias.Average(p => p.Ticks));
        return new Dictionary<int, TimeSpan> { [mes] = media };
    }
    public async Task<IEnumerable<Aluno>> ObterAlunosSemAcessoNosUltimosDias(int dias, CancellationToken cancellationToken = default)
    {
        try
        {
            string query = $"{BaseAlunoSelectQuery} WHERE NOT EXISTS (SELECT 1 FROM tb_acesso ac WHERE ac.pessoa_tipo = {PessoaTipoAluno} AND ac.pessoa_id = a.id_aluno AND ac.data_hora >= @Limite) ORDER BY a.nome";
            await using var command = await CreateCommandAsync(query, cancellationToken);
            command.AddParameter("@Limite", DateTime.Today.AddDays(-dias), DbType.DateTime);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var alunos = new List<Aluno>();
            while (await reader.ReadAsync(cancellationToken))
            {
                alunos.Add(AlunoRepository.Map(reader));
            }
            return alunos;
        }
        catch (DbException ex)
        {
            throw new InfrastructureException("ERRO_OBTER_ALUNOS_SEM_ACESSO", $"Erro ao obter alunos sem acesso nos últimos {dias} dias: {ex.Message}", ex);
        }
    }
}
