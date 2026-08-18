using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class CalculadorClavesCandidatas
{
    private const int MaximoColumnasPorClave = 3;

    public IReadOnlyList<ClaveCandidata> Calcular(TablaNormalizacion tabla)
    {
        var nombresColumnas = tabla.Columnas.Select(columna => columna.Nombre).ToArray();
        var claves = new List<ClaveCandidata>();

        for (var cantidadColumnas = 1; cantidadColumnas <= Math.Min(MaximoColumnasPorClave, nombresColumnas.Length); cantidadColumnas++)
        {
            foreach (var columnas in CrearCombinaciones(nombresColumnas, cantidadColumnas))
            {
                var nombresCombinacion = columnas.ToHashSet(StringComparer.Ordinal);
                if (claves.Any(clave => clave.Columnas.All(nombresCombinacion.Contains)))
                {
                    continue;
                }

                if (EsClaveCandidata(tabla, columnas))
                {
                    claves.Add(new(columnas));
                }
            }
        }

        return claves;
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

    private static bool EsClaveCandidata(TablaNormalizacion tabla, IReadOnlyList<string> nombresColumnas)
    {
        var valores = tabla.Registros
            .Select(registro => nombresColumnas.Select(nombreColumna => registro.Valores.TryGetValue(nombreColumna, out var valor) ? valor : null).ToArray())
            .ToArray();

        return valores.Length > 0
            && valores.All(valoresClave => valoresClave.All(valor => valor is not null))
            && valores.Select(valoresClave => string.Join('\u001F', valoresClave)).Distinct(StringComparer.Ordinal).Count() == valores.Length;
    }
}
