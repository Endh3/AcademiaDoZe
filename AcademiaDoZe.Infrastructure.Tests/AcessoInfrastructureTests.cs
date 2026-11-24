using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Infrastructure.Repositories;
namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoInfrastructureTests : TestBase
{
    private readonly LogradouroRepository _logradouroRepo;
    private readonly AlunoRepository _alunoRepo;
    private readonly ColaboradorRepository _colaboradorRepo;
    private readonly AcessoAlunoRepository _acessoAlunoRepo;
    private readonly AcessoColaboradorRepository _acessoColaboradorRepo;
    public AcessoInfrastructureTests()
    {
        _logradouroRepo = new LogradouroRepository(ConnectionString, DatabaseType);
        _alunoRepo = new AlunoRepository(ConnectionString, DatabaseType);
        _colaboradorRepo = new ColaboradorRepository(ConnectionString, DatabaseType);
        _acessoAlunoRepo = new AcessoAlunoRepository(ConnectionString, DatabaseType);
        _acessoColaboradorRepo = new AcessoColaboradorRepository(ConnectionString, DatabaseType);
    }
    private async Task<AcessoAluno> RegistrarAcessoAlunoAsync(Aluno aluno, DateTime dataHora)
    {
        var acesso = AcessoAluno.Criar(0, aluno, dataHora);
        Assert.True(acesso.IsSuccess);
        return await _acessoAlunoRepo.Adicionar(acesso.Value!);
    }
    private async Task<AcessoColaborador> RegistrarAcessoColaboradorAsync(Colaborador colaborador, DateTime dataHora)
    {
        var acesso = AcessoColaborador.Criar(0, colaborador, dataHora);
        Assert.True(acesso.IsSuccess);
        return await _acessoColaboradorRepo.Adicionar(acesso.Value!);
    }
    [Fact]
    public async Task AcessoAluno_Adicionar_E_ObterPorId_Sucesso()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var dataHora = DateTime.Today.AddHours(10);
        var inserido = await RegistrarAcessoAlunoAsync(aluno, dataHora);
        Assert.True(inserido.Id > 0);
        var obtido = await _acessoAlunoRepo.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal(aluno.Id, obtido!.AlunoId);
        Assert.Equal(dataHora, obtido.DataHora);
    }
    [Fact]
    public async Task AcessoAluno_EstaNaAcademia_AlternaEntreEntradaESaida()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        Assert.False(await _acessoAlunoRepo.EstaNaAcademia(aluno.Id));
        await RegistrarAcessoAlunoAsync(aluno, DateTime.Today.AddHours(10));
        Assert.True(await _acessoAlunoRepo.EstaNaAcademia(aluno.Id));
        await RegistrarAcessoAlunoAsync(aluno, DateTime.Today.AddHours(11));
        Assert.False(await _acessoAlunoRepo.EstaNaAcademia(aluno.Id));
    }
    [Fact]
    public async Task AcessoAluno_ObterUltimoAcesso_RetornaOMaisRecente()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        Assert.Null(await _acessoAlunoRepo.ObterUltimoAcesso(aluno.Id));
        await RegistrarAcessoAlunoAsync(aluno, DateTime.Today.AddHours(8));
        var ultimo = await RegistrarAcessoAlunoAsync(aluno, DateTime.Today.AddHours(9));
        var obtido = await _acessoAlunoRepo.ObterUltimoAcesso(aluno.Id);
        Assert.NotNull(obtido);
        Assert.Equal(ultimo.Id, obtido!.Id);
    }
    [Fact]
    public async Task AcessoAluno_ObterAcessosPorPeriodo_FiltraPorAlunoEData()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        await RegistrarAcessoAlunoAsync(aluno, DateTime.Today.AddDays(-2).AddHours(10));
        await RegistrarAcessoAlunoAsync(aluno, DateTime.Today.AddHours(10));
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var somenteHoje = (await _acessoAlunoRepo.ObterAcessosPorAlunoPeriodo(aluno.Id, hoje, hoje)).ToList();
        Assert.Single(somenteHoje);
        var todos = (await _acessoAlunoRepo.ObterAcessosPorAlunoPeriodo(aluno.Id)).ToList();
        Assert.Equal(2, todos.Count);
    }
    [Fact]
    public async Task AcessoAluno_Remover_Sucesso()
    {
        var aluno = await AlunoInfrastructureTests.CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var inserido = await RegistrarAcessoAlunoAsync(aluno, DateTime.Today.AddHours(10));
        Assert.True(await _acessoAlunoRepo.Remover(inserido.Id));
        Assert.Null(await _acessoAlunoRepo.ObterPorId(inserido.Id));
    }
    [Fact]
    public async Task AcessoColaborador_Adicionar_E_ObterPorId_Sucesso()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        var dataHora = DateTime.Today.AddHours(8);
        var inserido = await RegistrarAcessoColaboradorAsync(colaborador, dataHora);
        Assert.True(inserido.Id > 0);
        var obtido = await _acessoColaboradorRepo.ObterPorId(inserido.Id);
        Assert.NotNull(obtido);
        Assert.Equal(colaborador.Id, obtido!.ColaboradorId);
        Assert.Equal(dataHora, obtido.DataHora);
    }
    [Fact]
    public async Task AcessoColaborador_HorasTrabalhadasNoDia_SomaTodosOsRegistrosDoDia()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        // 08:00-12:00 (4h) + 13:00-15:30 (2h30)
        await RegistrarAcessoColaboradorAsync(colaborador, DateTime.Today.AddHours(8));
        await RegistrarAcessoColaboradorAsync(colaborador, DateTime.Today.AddHours(12));
        await RegistrarAcessoColaboradorAsync(colaborador, DateTime.Today.AddHours(13));
        await RegistrarAcessoColaboradorAsync(colaborador, DateTime.Today.AddHours(15).AddMinutes(30));
        var horas = await _acessoColaboradorRepo.ObterHorasTrabalhadasNoDia(colaborador.Id, DateOnly.FromDateTime(DateTime.Today));
        Assert.Equal(new TimeSpan(6, 30, 0), horas);
    }
    [Fact]
    public async Task AcessoColaborador_HorasTrabalhadasNoDia_SemRegistros_RetornaZero()
    {
        var colaborador = await ColaboradorInfrastructureTests.CriarEInserirColaboradorAsync(_colaboradorRepo, _logradouroRepo);
        var horas = await _acessoColaboradorRepo.ObterHorasTrabalhadasNoDia(colaborador.Id, DateOnly.FromDateTime(DateTime.Today));
        Assert.Equal(TimeSpan.Zero, horas);
    }
}
