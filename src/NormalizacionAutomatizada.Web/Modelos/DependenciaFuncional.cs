namespace NormalizacionAutomatizada.Web.Modelos;

public sealed record DependenciaFuncional(IReadOnlyList<string> Determinante, string Dependiente);
