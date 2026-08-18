using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class NormalizadorTerceraFormaNormal
{
    private readonly CalculadorClavesCandidatas calculadorClaves = new();
    private readonly DetectorDependenciasFuncionales detectorDependencias = new();

    public ResultadoTablaNormalizada Normalizar(TablaNormalizacion tabla)
    {
        var clavesCandidatas = calculadorClaves.Calcular(tabla);
        var clavePrincipal = ObtenerClavePrincipal(tabla, clavesCandidatas);
        var dependencias = detectorDependencias.Detectar(tabla);
        var tablaBase = CrearTabla($"Tabla_{tabla.Numero}_01", tabla.Columnas, clavePrincipal);
        var advertencias = new List<string>();

        if (clavePrincipal.Count == 0)
        {
            advertencias.Add($"No se pudo inferir una clave candidata para Tabla_{tabla.Numero}; se conserva la tabla sin descomponer.");
            return new(tabla.Numero, clavesCandidatas, dependencias, [tablaBase], [tablaBase], [tablaBase], advertencias);
        }

        if (tabla.Columnas.Any(columna => columna.EsClaveForanea))
        {
            return new(tabla.Numero, clavesCandidatas, dependencias, [tablaBase], [tablaBase], [tablaBase], advertencias);
        }

        var columnasBase = tabla.Columnas.ToList();
        var tablasTerceraFormaNormal = new List<TablaNormalizada>();
        var dependenciasTransitivas = dependencias
            .Where(dependencia => EsDeterminanteElegible(dependencia.Determinante)
                && !dependencia.Determinante.SequenceEqual(clavePrincipal)
                && dependencia.Dependiente is not null
                && !clavePrincipal.Contains(dependencia.Dependiente, StringComparer.Ordinal))
            .GroupBy(dependencia => string.Join("|", dependencia.Determinante), StringComparer.Ordinal)
            .ToArray();

        foreach (var grupo in dependenciasTransitivas)
        {
            var determinante = grupo.First().Determinante;
            var dependientes = grupo.Select(dependencia => dependencia.Dependiente).Distinct(StringComparer.Ordinal).ToArray();
            var nombresColumnas = determinante.Concat(dependientes).ToHashSet(StringComparer.Ordinal);
            var columnas = tabla.Columnas.Where(columna => nombresColumnas.Contains(columna.Nombre)).ToArray();

            if (columnas.Length == determinante.Count)
            {
                continue;
            }

            tablasTerceraFormaNormal.Add(CrearTabla($"Tabla_{tabla.Numero}_{tablasTerceraFormaNormal.Count + 2:00}", columnas, determinante));
            columnasBase.RemoveAll(columna => dependientes.Contains(columna.Nombre, StringComparer.Ordinal));
        }

        tablasTerceraFormaNormal.Insert(0, CrearTabla($"Tabla_{tabla.Numero}_01", columnasBase, clavePrincipal));
        var tablasConReferencias = AgregarReferenciasForaneas(tablasTerceraFormaNormal);

        return new(tabla.Numero, clavesCandidatas, dependencias, [tablaBase], [tablaBase], tablasConReferencias, advertencias);
    }

    private static IReadOnlyList<string> ObtenerClavePrincipal(TablaNormalizacion tabla, IReadOnlyList<ClaveCandidata> clavesCandidatas)
    {
        var clavesDeclaradas = tabla.Columnas.Where(columna => columna.EsClavePrimaria).Select(columna => columna.Nombre).ToArray();
        return clavesDeclaradas.Length > 0 ? clavesDeclaradas : clavesCandidatas.FirstOrDefault()?.Columnas ?? [];
    }

    private static bool EsDeterminanteElegible(IReadOnlyList<string> determinante)
    {
        return determinante.All(nombre => nombre.StartsWith("id_", StringComparison.OrdinalIgnoreCase)
            || nombre.StartsWith("codigo_", StringComparison.OrdinalIgnoreCase)
            || nombre.StartsWith("cod_", StringComparison.OrdinalIgnoreCase));
    }

    private static TablaNormalizada CrearTabla(string nombre, IReadOnlyList<ColumnaNormalizacion> columnas, IReadOnlyList<string> clavePrimaria)
    {
        return new(nombre, columnas, clavePrimaria, []);
    }

    private static IReadOnlyList<TablaNormalizada> AgregarReferenciasForaneas(IReadOnlyList<TablaNormalizada> tablas)
    {
        return tablas.Select(tabla =>
        {
            var referencias = tablas
                .Where(destino => destino.Nombre != tabla.Nombre && destino.ClavePrimaria.Count > 0 && destino.ClavePrimaria.All(clave => tabla.Columnas.Any(columna => columna.Nombre == clave)))
                .Select(destino => new ReferenciaForanea(destino.ClavePrimaria, destino.Nombre, destino.ClavePrimaria))
                .ToArray();
            return tabla with { ClavesForaneas = referencias };
        }).ToArray();
    }
}
