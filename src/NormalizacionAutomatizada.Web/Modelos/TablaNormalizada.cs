namespace NormalizacionAutomatizada.Web.Modelos;

public sealed record ReferenciaForanea(IReadOnlyList<string> Columnas, string TablaDestino, IReadOnlyList<string> ColumnasDestino);

public sealed record TablaNormalizada(
    string Nombre,
    IReadOnlyList<ColumnaNormalizacion> Columnas,
    IReadOnlyList<string> ClavePrimaria,
    IReadOnlyList<ReferenciaForanea> ClavesForaneas);

public sealed record ResultadoTablaNormalizada(
    string Numero,
    IReadOnlyList<ClaveCandidata> ClavesCandidatas,
    IReadOnlyList<DependenciaFuncional> DependenciasObservadas,
    IReadOnlyList<TablaNormalizada> TablasPrimeraFormaNormal,
    IReadOnlyList<TablaNormalizada> TablasSegundaFormaNormal,
    IReadOnlyList<TablaNormalizada> TablasTerceraFormaNormal,
    IReadOnlyList<string> Advertencias);
