//Matheus Ribeiro Pites De Liz
namespace AcademiaDoZe.Domain.ValueObjects;

public record Email
{
    public string Valor { get; }
    private Email(string valor)
    {
        Valor = valor;
    }
}