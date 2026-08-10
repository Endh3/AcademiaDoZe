//Matheus Ribeiro Pites De Liz
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Services;

namespace AcademiaDoZe.Domain.ValueObjects;

public record Senha
{
    public string Valor { get; }
    private Senha(string valor)
    {
        Valor = valor;
    }
    public static Result<Senha> Criar(string valor)
    {
        if (NormalizadoService.TextoVazioOuNulo(valor))
            return Result<Senha>.Failure("Senha", "SENHA_OBRIGATORIO");
        var textoLimpo = NormalizadoService.LimparEDigitos(valor);
        if (textoLimpo.Length != 11)
            return Result<Senha>.Failure("Senha", "SENHA_DIGITOS_MINIMOS");
        return Result<Senha>.Success(new Senha(textoLimpo));
    }
}