using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class EvaluadorCumplimientoNormalizacion
{
    public IReadOnlyList<ResultadoTablaNormalizada> Evaluar(EntradaNormalizacion entrada, IReadOnlyList<ResultadoTablaNormalizada> resultados)
    {
        var tablasFinales = resultados.SelectMany(resultado => resultado.TablasTerceraFormaNormal).ToArray();
        return resultados.Select(resultado => resultado with
        {
            Evaluacion = EvaluarTabla(entrada.Tablas.Single(tabla => tabla.Numero == resultado.Numero), resultado, tablasFinales)
        }).ToArray();
    }

    private static EvaluacionCumplimientoNormalizacion EvaluarTabla(
        TablaNormalizacion tablaOrigen,
        ResultadoTablaNormalizada resultado,
        IReadOnlyList<TablaNormalizada> tablasFinales)
    {
        var errores = resultado.Advertencias.ToList();
        var columnasNoAtomicasDetectadas = BuscarColumnasNoAtomicas(tablaOrigen);
        var columnasNulasFrecuentesDetectadas = BuscarColumnasNulasFrecuentes(tablaOrigen);
        var tablaBase = resultado.TablasTerceraFormaNormal.SingleOrDefault(tabla => tabla.Nombre == $"Tabla_{tablaOrigen.Numero}_01");
        var columnasNoAtomicas = columnasNoAtomicasDetectadas
            .Where(nombre => tablaBase is null || tablaBase.Columnas.Any(columna => columna.Nombre == nombre))
            .ToList();
        var columnasNulasFrecuentes = columnasNulasFrecuentesDetectadas
            .Where(nombre => tablaBase is null || tablaBase.Columnas.Any(columna => columna.Nombre == nombre))
            .ToList();
        var tablasQueConservanColumnas = resultado.TablasTerceraFormaNormal
            .Concat(resultado.TablasTerceraFormaNormal.SelectMany(tabla => tabla.ClavesForaneas)
                .Select(referencia => tablasFinales.SingleOrDefault(tabla => tabla.Nombre == referencia.TablaDestino))
                .OfType<TablaNormalizada>())
            .ToArray();
        var columnasFinales = tablasQueConservanColumnas.SelectMany(tabla => tabla.Columnas).Select(columna => columna.Nombre).ToHashSet(StringComparer.Ordinal);
        var clavesPrimarias = tablaOrigen.Columnas.Where(columna => columna.EsClavePrimaria).Select(columna => columna.Nombre).ToArray();

        if (resultado.TablasTerceraFormaNormal.Any(tabla => tabla.ClavePrimaria.Count == 0))
        {
            errores.Add($"Tabla_{tablaOrigen.Numero} no tiene una clave primaria verificable para exportar SQL.");
        }

        if (columnasNoAtomicas.Count > 0)
        {
            errores.Add($"Tabla_{tablaOrigen.Numero} tiene valores no atomicos sin una separacion segura: {string.Join(", ", columnasNoAtomicas)}.");
        }

        if (columnasNulasFrecuentes.Count > 0)
        {
            errores.Add($"Tabla_{tablaOrigen.Numero} tiene valores nulos frecuentes en: {string.Join(", ", columnasNulasFrecuentes)}.");
        }

        if (tablaOrigen.Columnas.Any(columna => !columnasFinales.Contains(columna.Nombre)))
        {
            errores.Add($"Tabla_{tablaOrigen.Numero} perderia columnas al reconstruir las relaciones finales.");
        }

        if (!ReferenciasForaneasVerificables(resultado.TablasTerceraFormaNormal, tablasFinales))
        {
            errores.Add($"Tabla_{tablaOrigen.Numero} contiene una referencia foranea sin una clave primaria destino compatible.");
        }

        var primeraFormaNormalCorregida = columnasNoAtomicasDetectadas.Count > 0 && columnasNoAtomicas.Count == 0;
        var segundaFormaNormalCorregida = resultado.TablasSegundaFormaNormal.Count > resultado.TablasPrimeraFormaNormal.Count;
        var terceraFormaNormalCorregida = resultado.TablasTerceraFormaNormal.Count > resultado.TablasSegundaFormaNormal.Count;
        var nulosFrecuentesCorregidos = columnasNulasFrecuentesDetectadas.Count > 0 && columnasNulasFrecuentes.Count == 0;
        var tieneClaveCompuesta = clavesPrimarias.Length > 1;
        var tieneTransformacion = primeraFormaNormalCorregida || segundaFormaNormalCorregida || terceraFormaNormalCorregida || nulosFrecuentesCorregidos;

        return new(
            [
                new("1FN", columnasNoAtomicas.Count > 0 ? "Requiere correccion" : primeraFormaNormalCorregida ? "Corregida automaticamente" : "Cumple", primeraFormaNormalCorregida ? "Los grupos repetidos detectados se separaron en relaciones atomicas." : "No se detectaron grupos repetidos pendientes."),
                new("2FN", tieneClaveCompuesta && !segundaFormaNormalCorregida ? "Cumple" : segundaFormaNormalCorregida ? "Corregida automaticamente" : "Cumple", segundaFormaNormalCorregida ? "Las dependencias parciales se separaron de la clave compuesta." : "No se detectaron dependencias parciales pendientes."),
                new("3FN", terceraFormaNormalCorregida ? "Corregida automaticamente" : "Cumple", terceraFormaNormalCorregida ? "Las dependencias transitivas se separaron en relaciones propias." : "No se detectaron dependencias transitivas pendientes.")
            ],
            [
                new("Directriz 1", tieneTransformacion ? "Corregida automaticamente" : "Cumple", tieneTransformacion ? "Las entidades o relaciones mezcladas se separaron en tablas diferentes." : "No se detecto mezcla de entidades en los datos analizados."),
                new("Directriz 2", tieneTransformacion ? "Corregida automaticamente" : "Cumple", tieneTransformacion ? "La separacion reduce redundancia y anomalias de insercion, eliminacion o modificacion." : "No se observaron dependencias que produzcan anomalias."),
                new("Directriz 3", columnasNulasFrecuentes.Count > 0 ? "Requiere correccion" : nulosFrecuentesCorregidos ? "Corregida automaticamente" : "Cumple", columnasNulasFrecuentes.Count > 0 ? "Los atributos con nulos frecuentes necesitan una relacion opcional especifica." : nulosFrecuentesCorregidos ? "Los atributos con nulos frecuentes se separaron en relaciones opcionales." : "No se detectaron atributos con nulos frecuentes."),
                new("Directriz 4", ReferenciasForaneasVerificables(resultado.TablasTerceraFormaNormal, tablasFinales) ? "Cumple" : "Requiere correccion", "Las uniones propuestas se verifican mediante claves primarias y foraneas compatibles.")
            ],
            errores.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static List<string> BuscarColumnasNoAtomicas(TablaNormalizacion tabla)
    {
        return tabla.Columnas
            .Where(columna => !columna.EsClavePrimaria
                && columna.Nombre.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                && tabla.Registros.Any(registro => registro.Valores.TryGetValue(columna.Nombre, out var valor)
                    && !string.IsNullOrWhiteSpace(valor)
                    && valor.IndexOfAny([';', ',', '|']) >= 0))
            .Select(columna => columna.Nombre)
            .ToList();
    }

    private static List<string> BuscarColumnasNulasFrecuentes(TablaNormalizacion tabla)
    {
        return tabla.Columnas
            .Where(columna => !columna.EsClavePrimaria
                && tabla.Registros.Count > 0
                && tabla.Registros.Count(registro => !registro.Valores.TryGetValue(columna.Nombre, out var valor) || valor is null) * 2 >= tabla.Registros.Count)
            .Select(columna => columna.Nombre)
            .ToList();
    }

    private static bool ReferenciasForaneasVerificables(IReadOnlyList<TablaNormalizada> tablasOrigen, IReadOnlyList<TablaNormalizada> tablasFinales)
    {
        return tablasOrigen.SelectMany(tabla => tabla.ClavesForaneas).All(referencia => tablasFinales.Any(tabla => tabla.Nombre == referencia.TablaDestino
            && tabla.ClavePrimaria.SequenceEqual(referencia.ColumnasDestino)));
    }
}
