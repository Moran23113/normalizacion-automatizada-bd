namespace NormalizacionAutomatizada.Web.Modelos;

public sealed record EntradaNormalizacion(IReadOnlyList<TablaNormalizacion> Tablas);

public sealed record TablaNormalizacion(
    string Numero,
    IReadOnlyList<ColumnaNormalizacion> Columnas,
    IReadOnlyList<RegistroOriginal> Registros)
{
    public string Nombre { get; init; } = string.Empty;

    public string NombreBase => string.IsNullOrWhiteSpace(Nombre) ? $"Tabla_{Numero}" : Nombre;
}

public sealed record ColumnaNormalizacion(string Nombre, string TipoDato, bool EsClavePrimaria, bool EsClaveForanea);

public sealed record RegistroOriginal(IReadOnlyDictionary<string, string?> Valores);
