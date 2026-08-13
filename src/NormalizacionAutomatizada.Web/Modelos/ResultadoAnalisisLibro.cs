namespace NormalizacionAutomatizada.Web.Modelos;

public sealed record ResultadoAnalisisLibro(IReadOnlyList<ResumenTablaAnalizada> Tablas, IReadOnlyList<string> Errores)
{
    public bool EsValido => Errores.Count == 0;

    public EntradaNormalizacion? Entrada { get; init; }
}

public sealed record ResumenTablaAnalizada(string Numero, int CantidadColumnas, int CantidadRegistros);
