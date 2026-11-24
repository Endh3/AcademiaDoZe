using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
namespace AcademiaDoZe.Presentation.AppMaui.ViewModels;

// Controle de acesso (catraca): registra entrada/saída de alunos e colaboradores
public partial class AcessoViewModel : BaseViewModel
{
    private static readonly Color CorAutorizado = Color.FromArgb("#0F5C45");
    private static readonly Color CorNegado = Color.FromArgb("#7A1F2B");
    private readonly IAlunoService _alunoService;
    private readonly IColaboradorService _colaboradorService;
    private readonly IAcessoAlunoService _acessoAlunoService;
    private readonly IAcessoColaboradorService _acessoColaboradorService;
    private List<PessoaDto> _todasPessoas = [];
    private List<PessoaDto> _pessoasFiltradas = [];
    private bool _modoAluno = true;
    public bool ModoAluno
    {
        get => _modoAluno;
        private set
        {
            if (SetProperty(ref _modoAluno, value))
            {
                OnPropertyChanged(nameof(ModoTexto));
                OnPropertyChanged(nameof(SelecioneTexto));
            }
        }
    }
    public string ModoTexto => $"Modo atual selecionado: {(ModoAluno ? "Aluno" : "Colaborador")}";
    public string SelecioneTexto => $"Selecione o {(ModoAluno ? "Aluno" : "Colaborador")}:";
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                AplicarFiltro();
            }
        }
    }
    public ObservableCollection<string> Opcoes { get; } = [];
    private int _indiceSelecionado = -1;
    public int IndiceSelecionado
    {
        get => _indiceSelecionado;
        set
        {
            if (SetProperty(ref _indiceSelecionado, value))
            {
                _ = AtualizarSelecaoAsync();
            }
        }
    }
    private PessoaDto? _pessoaSelecionada;
    public PessoaDto? PessoaSelecionada
    {
        get => _pessoaSelecionada;
        private set
        {
            if (SetProperty(ref _pessoaSelecionada, value))
            {
                OnPropertyChanged(nameof(TemPessoaSelecionada));
                OnPropertyChanged(nameof(SemPessoaSelecionada));
            }
        }
    }
    public bool TemPessoaSelecionada => PessoaSelecionada != null;
    public bool SemPessoaSelecionada => PessoaSelecionada == null;
    private bool _estaNaAcademia;
    public bool EstaNaAcademia
    {
        get => _estaNaAcademia;
        private set
        {
            if (SetProperty(ref _estaNaAcademia, value))
            {
                OnPropertyChanged(nameof(StatusTexto));
                OnPropertyChanged(nameof(BotaoTexto));
            }
        }
    }
    public string StatusTexto => EstaNaAcademia ? "DENTRO DA ACADEMIA" : "FORA DA ACADEMIA";
    public string BotaoTexto => EstaNaAcademia ? "Registrar Saída" : "Registrar Entrada";
    private AcessoResultadoDto? _resultado;
    public AcessoResultadoDto? Resultado
    {
        get => _resultado;
        private set
        {
            if (SetProperty(ref _resultado, value))
            {
                OnPropertyChanged(nameof(TemResultado));
                OnPropertyChanged(nameof(ResultadoCor));
                OnPropertyChanged(nameof(ResultadoIcone));
            }
        }
    }
    public bool TemResultado => Resultado != null;
    public Color ResultadoCor => Resultado is { Autorizado: false } ? CorNegado : CorAutorizado;
    // ícones da fonte MaterialIcons: check_circle (e86c) e cancel (e5c9)
    public string ResultadoIcone => Resultado is { Autorizado: false } ? "\ue5c9" : "\ue86c";
    public ObservableCollection<AcessoRegistroDto> AcessosHoje { get; } = [];
    public AcessoViewModel(IAlunoService alunoService, IColaboradorService colaboradorService, IAcessoAlunoService acessoAlunoService, IAcessoColaboradorService acessoColaboradorService)
    {
        _alunoService = alunoService;
        _colaboradorService = colaboradorService;
        _acessoAlunoService = acessoAlunoService;
        _acessoColaboradorService = acessoColaboradorService;
        Title = "Controle de Acesso";
    }
    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            await CarregarPessoasAsync();
            await CarregarAcessosDoDiaAsync();
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento dos dados expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao carregar o controle de acesso: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    [RelayCommand]
    private async Task SelecionarAlunoAsync() => await TrocarModoAsync(true);
    [RelayCommand]
    private async Task SelecionarColaboradorAsync() => await TrocarModoAsync(false);
    private async Task TrocarModoAsync(bool aluno)
    {
        if (IsBusy || ModoAluno == aluno)
            return;
        try
        {
            IsBusy = true;
            ModoAluno = aluno;
            Resultado = null;
            _searchText = string.Empty;
            OnPropertyChanged(nameof(SearchText));
            await CarregarPessoasAsync();
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O carregamento dos dados expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao trocar o modo: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    [RelayCommand]
    private async Task RegistrarAsync()
    {
        if (IsBusy)
            return;
        if (PessoaSelecionada == null)
        {
            await Shell.Current.DisplayAlertAsync("Validação", $"Selecione {(ModoAluno ? "um aluno" : "um colaborador")} para registrar o acesso.", "OK");
            return;
        }
        try
        {
            IsBusy = true;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            Resultado = ModoAluno
            ? await _acessoAlunoService.RegistrarAcessoAsync(PessoaSelecionada.Id, cts.Token)
            : await _acessoColaboradorService.RegistrarAcessoAsync(PessoaSelecionada.Id, cts.Token);
            await AtualizarStatusAsync(cts.Token);
            await CarregarAcessosDoDiaAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            await Shell.Current.DisplayAlertAsync("Tempo Esgotado", "O registro do acesso expirou. Verifique a conexão com o banco.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao registrar o acesso: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    [RelayCommand]
    private async Task AtualizarAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            await CarregarAcessosDoDiaAsync();
            await AtualizarStatusAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao atualizar os acessos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    private async Task CarregarPessoasAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        IEnumerable<PessoaDto> pessoas = ModoAluno
        ? await _alunoService.ObterTodosAsync(cts.Token)
        : await _colaboradorService.ObterTodosAsync(cts.Token);
        _todasPessoas = [.. pessoas.OrderBy(p => p.Nome)];
        AplicarFiltro();
    }
    // Filtra por nome ou CPF e preenche a lista de seleção; com texto digitado, já seleciona o primeiro resultado
    private void AplicarFiltro()
    {
        var termo = SearchText.Trim();
        var digitos = new string([.. termo.Where(char.IsDigit)]);
        _pessoasFiltradas = string.IsNullOrEmpty(termo)
        ? [.. _todasPessoas]
        : [.. _todasPessoas.Where(p => p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) || (digitos.Length > 0 && p.Cpf.Contains(digitos)))];
        Opcoes.Clear();
        foreach (var pessoa in _pessoasFiltradas)
        {
            Opcoes.Add(pessoa.Nome);
        }
        int novoIndice = !string.IsNullOrEmpty(termo) && _pessoasFiltradas.Count > 0 ? 0 : -1;
        // se o índice não mudou, a pessoa selecionada pode ter mudado (a lista foi refeita): força a atualização
        if (IndiceSelecionado == novoIndice)
        {
            _ = AtualizarSelecaoAsync();
        }
        else
        {
            IndiceSelecionado = novoIndice;
        }
    }
    private async Task AtualizarSelecaoAsync()
    {
        try
        {
            var nova = IndiceSelecionado >= 0 && IndiceSelecionado < _pessoasFiltradas.Count ? _pessoasFiltradas[IndiceSelecionado] : null;
            if (nova?.Id != PessoaSelecionada?.Id)
            {
                Resultado = null;
            }
            PessoaSelecionada = nova;
            await AtualizarStatusAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Erro", $"Erro ao consultar a situação da pessoa: {ex.Message}", "OK");
        }
    }
    private async Task AtualizarStatusAsync(CancellationToken cancellationToken = default)
    {
        if (PessoaSelecionada == null)
        {
            EstaNaAcademia = false;
            return;
        }
        var pessoa = PessoaSelecionada;
        bool dentro = ModoAluno
        ? await _acessoAlunoService.EstaNaAcademiaAsync(pessoa.Id, cancellationToken)
        : await _acessoColaboradorService.EstaNaAcademiaAsync(pessoa.Id, cancellationToken);
        // descarta o resultado se a seleção mudou enquanto a consulta rodava
        if (pessoa == PessoaSelecionada)
        {
            EstaNaAcademia = dentro;
        }
    }
    private async Task CarregarAcessosDoDiaAsync(CancellationToken cancellationToken = default)
    {
        var alunosTask = _acessoAlunoService.ObterAcessosDoDiaAsync(cancellationToken);
        var colaboradoresTask = _acessoColaboradorService.ObterAcessosDoDiaAsync(cancellationToken);
        await Task.WhenAll(alunosTask, colaboradoresTask);
        var todos = (await alunosTask).Concat(await colaboradoresTask).OrderByDescending(a => a.DataHora).ThenByDescending(a => a.Id);
        AcessosHoje.Clear();
        foreach (var acesso in todos)
        {
            AcessosHoje.Add(acesso);
        }
    }
}
