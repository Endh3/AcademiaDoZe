//Matheus Ribeiro Pites De Liz
namespace AcademiaDoZe.Domain.ValueObjects;

public record Arquivo
{
    public byte[] Conteudo { get; }
    private Arquivo(byte[] conteudo)
    {
        Conteudo = conteudo;
    }
}