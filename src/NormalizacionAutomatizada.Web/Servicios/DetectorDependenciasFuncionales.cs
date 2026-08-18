using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class DetectorDependenciasFuncionales
{
    private const int MaximoColumnasDeterminantes = 2;

    public IReadOnlyList<DependenciaFuncional> Detectar(TablaNormalizacion tabla)
    {
        var nombresColumnas = tabla.Columnas.Select(columna => columna.Nombre).ToArray();
        var dependencias = new List<DependenciaFuncional>();

        for (var cantidadColumnas = 1; cantidadColumnas <= Math.Min(MaximoColumnasDeterminantes, nombresColumnas.Length); cantidadColumnas++)
        {
            foreach (var determinante in CrearCombinaciones(nombresColumnas, cantidadColumnas))
            {
                var nombresDeterminante = determinante.ToHashSet(StringComparer.Ordinal);
                foreach (var dependiente in nombresColumnas.Where(nombre => !nombresDeterminante.Contains(nombre)))
                {
                    if (dependencias.Any(dependencia => dependencia.Dependiente == dependiente && dependencia.Determinante.All(nombresDeterminante.Contains)))
                    {
                        continue;
                    }

                    if (EsDependenciaObservada(tabla, determinante, dependiente))
                    {
                        dependencias.Add(new(determinante, dependiente));
                    }
                }
            }
        }

        return dependencias;
    }

    private static IEnumerable<IReadOnlyList<string>> CrearCombinaciones(IReadOnlyList<string> nombresColumnas, int cantidadColumnas)
    {
        return CrearCombinaciones(nombresColumnas, cantidadColumnas, 0, []);
    }

    private static IEnumerable<IReadOnlyList<string>> CrearCombinaciones(IReadOnlyList<string> nombresColumnas, int cantidadColumnas, int primerIndice, IReadOnlyList<string> columnasActuales)
    {
        if (columnasActuales.Count == cantidadColumnas)
        {
            yield return columnasActuales;
            yield break;
        }

        for (var indice = primerIndice; indice < nombresColumnas.Count; indice++)
        {
            foreach (var combinacion in CrearCombinaciones(nombresColumnas, cantidadColumnas, indice + 1, [.. columnasActuales, nombresColumnas[indice]]))
            {
                yield return combinacion;
            }
        }
    }

    private static bool EsDependenciaObservada(TablaNormalizacion tabla, IReadOnlyList<string> nombresDeterminante, string nombreDependiente)
    {
        var valores = tabla.Registros
            .Select(registro => (
                Determinante: nombresDeterminante.Select(nombre => ObtenerValor(registro, nombre)).ToArray(),
                Dependiente: ObtenerValor(registro, nombreDependiente)))
            .ToArray();

        var gruposPorDeterminante = valores.GroupBy(valor => string.Join('\u001F', valor.Determinante), StringComparer.Ordinal).ToArray();

        return valores.Length > 0
            && valores.All(valor => valor.Determinante.All(valorDeterminante => valorDeterminante is not null) && valor.Dependiente is not null)
            && gruposPorDeterminante.Any(grupo => grupo.Count() > 1)
            && gruposPorDeterminante.All(grupo => grupo.Select(valor => valor.Dependiente).Distinct(StringComparer.Ordinal).Count() == 1);
    }

    private static string? ObtenerValor(RegistroOriginal registro, string nombreColumna)
    {
        return registro.Valores.TryGetValue(nombreColumna, out var valor) ? valor : null;
    }
}
