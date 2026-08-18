using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class CompletadorReferenciasForaneas
{
    public IReadOnlyList<ResultadoTablaNormalizada> Completar(EntradaNormalizacion entrada, IReadOnlyList<ResultadoTablaNormalizada> resultados)
    {
        var destinos = resultados
            .SelectMany(resultado => resultado.TablasTerceraFormaNormal.Select(tabla => new DestinoReferencia(resultado.Numero, tabla)))
            .Where(destino => destino.Tabla.ClavePrimaria.Count == 1)
            .ToArray();

        return resultados.Select(resultado => CompletarResultado(entrada, resultado, destinos)).ToArray();
    }

    private static ResultadoTablaNormalizada CompletarResultado(EntradaNormalizacion entrada, ResultadoTablaNormalizada resultado, IReadOnlyList<DestinoReferencia> destinos)
    {
        var tablaOrigen = entrada.Tablas.Single(tabla => tabla.Numero == resultado.Numero);
        var advertencias = resultado.Advertencias.ToList();
        var tablas = resultado.TablasTerceraFormaNormal
            .Select(tabla => CompletarTabla(tabla, tablaOrigen, resultado.Numero, destinos, advertencias))
            .ToArray();

        return resultado with { TablasTerceraFormaNormal = tablas, Advertencias = advertencias };
    }

    private static TablaNormalizada CompletarTabla(
        TablaNormalizada tabla,
        TablaNormalizacion tablaOrigen,
        string numeroOrigen,
        IReadOnlyList<DestinoReferencia> destinos,
        ICollection<string> advertencias)
    {
        var referencias = tabla.ClavesForaneas.ToList();
        var columnasForaneas = tablaOrigen.Columnas
            .Where(columna => columna.EsClaveForanea && tabla.Columnas.Any(columnaTabla => columnaTabla.Nombre == columna.Nombre && columnaTabla.EsClaveForanea))
            .ToArray();

        foreach (var columnaForanea in columnasForaneas)
        {
            if (referencias.Any(referencia => referencia.Columnas.SequenceEqual([columnaForanea.Nombre])))
            {
                continue;
            }

            var coincidencias = destinos
                .Where(destino => destino.NumeroOrigen != numeroOrigen
                    && destino.Tabla.ClavePrimaria.SequenceEqual([columnaForanea.Nombre])
                    && destino.Tabla.Columnas.Any(columna => columna.Nombre == columnaForanea.Nombre && string.Equals(columna.TipoDato, columnaForanea.TipoDato, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (coincidencias.Length == 1)
            {
                var destino = coincidencias[0].Tabla;
                if (!referencias.Any(referencia => referencia.Columnas.SequenceEqual([columnaForanea.Nombre]) && referencia.TablaDestino == destino.Nombre))
                {
                    referencias.Add(new([columnaForanea.Nombre], destino.Nombre, [columnaForanea.Nombre]));
                }

                continue;
            }

            var advertencia = coincidencias.Length == 0
                ? $"No se encontro una clave primaria compatible para la clave foranea '{columnaForanea.Nombre}' de Tabla_{numeroOrigen}."
                : $"La clave foranea '{columnaForanea.Nombre}' de Tabla_{numeroOrigen} tiene mas de un posible destino y no se genero una referencia.";

            if (!advertencias.Contains(advertencia, StringComparer.Ordinal))
            {
                advertencias.Add(advertencia);
            }
        }

        return tabla with { ClavesForaneas = referencias };
    }

    private sealed record DestinoReferencia(string NumeroOrigen, TablaNormalizada Tabla);
}
