namespace NormalizacionAutomatizada.Web.Modelos;

public sealed record EntradaNormalizacion(IReadOnlyList<TablaNormalizacion> Tablas);

public sealed record TablaNormalizacion(
    string Numero,
    IReadOnlyList<ColumnaNormalizacion> Columnas,
    IReadOnlyList<RegistroOriginal> Registros);

public sealed record ColumnaNormalizacion(string Nombre, string TipoDato, bool EsClavePrimaria, bool EsClaveForanea);

public sealed record RegistroOriginal(IReadOnlyDictionary<string, string?> Valores);
