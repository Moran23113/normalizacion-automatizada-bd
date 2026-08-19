using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class NormalizadorTerceraFormaNormal
{
    private readonly CalculadorClavesCandidatas calculadorClaves = new();
    private readonly DetectorDependenciasFuncionales detectorDependencias = new();

    public ResultadoTablaNormalizada Normalizar(TablaNormalizacion tabla)
    {
        return Normalizar(tabla, [tabla]);
    }

    public ResultadoTablaNormalizada Normalizar(TablaNormalizacion tabla, IReadOnlyList<TablaNormalizacion> tablasEntrada)
    {
        var clavesCandidatas = calculadorClaves.Calcular(tabla);
        var clavePrincipal = ObtenerClavePrincipal(tabla, clavesCandidatas);
        var dependencias = detectorDependencias.Detectar(tabla);
        var advertencias = new List<string>();

        if (clavePrincipal.Count == 0)
        {
            advertencias.Add($"No se pudo inferir una clave candidata para Tabla_{tabla.Numero}; se conserva la tabla sin descomponer.");
            var tablaBaseSinDescomponer = CrearTabla(CrearNombreTabla(tabla, 1), tabla.Columnas, clavePrincipal);
            return new(tabla.Numero, clavesCandidatas, dependencias, [tablaBaseSinDescomponer], [tablaBaseSinDescomponer], [tablaBaseSinDescomponer], advertencias);
        }

        var columnasGruposRepetidos = BuscarColumnasGruposRepetidos(tabla, clavePrincipal);
        var columnasNulasFrecuentes = BuscarColumnasNulasFrecuentes(tabla, clavePrincipal)
            .Where(nombre => !columnasGruposRepetidos.Contains(nombre, StringComparer.Ordinal))
            .ToArray();
        var columnasSeparadas = columnasGruposRepetidos.Concat(columnasNulasFrecuentes).ToHashSet(StringComparer.Ordinal);
        var tablaSinGruposRepetidos = tabla with
        {
            Columnas = tabla.Columnas.Where(columna => !columnasSeparadas.Contains(columna.Nombre)).ToArray()
        };
        var tablasPrimeraFormaNormal = CrearTablasPrimeraFormaNormal(tabla, tablaSinGruposRepetidos, clavePrincipal, columnasGruposRepetidos, columnasNulasFrecuentes);
        var tablasSegundaFormaNormal = CrearTablasSegundaFormaNormal(tablaSinGruposRepetidos, clavePrincipal, dependencias, tablasPrimeraFormaNormal);
        var tablasGruposRepetidos = tablasPrimeraFormaNormal.Skip(1).ToArray();

        var tablasBaseTerceraFormaNormal = tablaSinGruposRepetidos.Columnas.Any(columna => columna.EsClaveForanea)
            ? SepararRelacionesDeclaradas(tablaSinGruposRepetidos, clavePrincipal, tablasPrimeraFormaNormal.Count + 1, tablasEntrada)
            : [CrearTabla(CrearNombreTabla(tabla, 1), tablaSinGruposRepetidos.Columnas, clavePrincipal)];
        var tablasInicialesTerceraFormaNormal = tablasBaseTerceraFormaNormal.Concat(tablasGruposRepetidos).OrderBy(tabla => tabla.Nombre, StringComparer.Ordinal).ToArray();
        var tablasTerceraFormaNormal = CrearTablasTerceraFormaNormal(tabla, tablasInicialesTerceraFormaNormal, dependencias);

        return new(tabla.Numero, clavesCandidatas, dependencias, tablasPrimeraFormaNormal, tablasSegundaFormaNormal, tablasTerceraFormaNormal, advertencias);
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

    private static string CrearNombreTabla(TablaNormalizacion tabla, int numero)
    {
        return $"{tabla.NombreBase}_{numero:00}";
    }

    private static IReadOnlyList<TablaNormalizada> CrearTablasPrimeraFormaNormal(
        TablaNormalizacion tabla,
        TablaNormalizacion tablaSinGruposRepetidos,
        IReadOnlyList<string> clavePrincipal,
        IReadOnlyList<string> columnasGruposRepetidos,
        IReadOnlyList<string> columnasNulasFrecuentes)
    {
        var tablaBase = CrearTabla(CrearNombreTabla(tabla, 1), tablaSinGruposRepetidos.Columnas, clavePrincipal);
        var tablas = new List<TablaNormalizada> { tablaBase };

        foreach (var nombreColumna in columnasGruposRepetidos)
        {
            var columna = tabla.Columnas.Single(columna => columna.Nombre == nombreColumna);
            var columnasClave = tabla.Columnas
                .Where(columna => clavePrincipal.Contains(columna.Nombre, StringComparer.Ordinal))
                .Select(columna => columna with { EsClavePrimaria = true, EsClaveForanea = true })
                .ToArray();
            var columnaAtomica = columna with { EsClavePrimaria = true, EsClaveForanea = false };
            var nombreTabla = CrearNombreTabla(tabla, tablas.Count + 1);

            tablas.Add(new(
                nombreTabla,
                columnasClave.Concat([columnaAtomica]).ToArray(),
                clavePrincipal.Concat([columna.Nombre]).ToArray(),
                [new(clavePrincipal, tablaBase.Nombre, clavePrincipal)]));
        }

        foreach (var nombreColumna in columnasNulasFrecuentes)
        {
            var columna = tabla.Columnas.Single(columna => columna.Nombre == nombreColumna);
            var columnasClave = tabla.Columnas
                .Where(columna => clavePrincipal.Contains(columna.Nombre, StringComparer.Ordinal))
                .Select(columna => columna with { EsClavePrimaria = true, EsClaveForanea = true })
                .ToArray();
            var nombreTabla = CrearNombreTabla(tabla, tablas.Count + 1);

            tablas.Add(new(
                nombreTabla,
                columnasClave.Concat([columna with { EsClavePrimaria = false }]).ToArray(),
                clavePrincipal,
                [new(clavePrincipal, tablaBase.Nombre, clavePrincipal)]));
        }

        return tablas;
    }

    private static IReadOnlyList<TablaNormalizada> CrearTablasSegundaFormaNormal(
        TablaNormalizacion tabla,
        IReadOnlyList<string> clavePrincipal,
        IReadOnlyList<DependenciaFuncional> dependencias,
        IReadOnlyList<TablaNormalizada> tablasPrimeraFormaNormal)
    {
        var columnasBase = tablasPrimeraFormaNormal[0].Columnas.ToList();
        var tablas = new List<TablaNormalizada> { tablasPrimeraFormaNormal[0] };
        tablas.AddRange(tablasPrimeraFormaNormal.Skip(1));
        var referenciasBase = new List<ReferenciaForanea>();
        var siguienteNumeroTabla = tablasPrimeraFormaNormal.Count + 1;
        var dependenciasParciales = dependencias
            .Where(dependencia => dependencia.Determinante.Count < clavePrincipal.Count
                && dependencia.Determinante.All(clave => clavePrincipal.Contains(clave, StringComparer.Ordinal))
                && !clavePrincipal.Contains(dependencia.Dependiente, StringComparer.Ordinal))
            .GroupBy(dependencia => string.Join("|", dependencia.Determinante), StringComparer.Ordinal);

        foreach (var grupo in dependenciasParciales)
        {
            var determinante = grupo.First().Determinante;
            var dependientes = grupo.Select(dependencia => dependencia.Dependiente)
                .Where(dependiente => columnasBase.Any(columna => columna.Nombre == dependiente))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (dependientes.Length == 0)
            {
                continue;
            }

            var nombresColumnas = determinante.Concat(dependientes).ToHashSet(StringComparer.Ordinal);
            var columnasRelacionadas = tabla.Columnas.Where(columna => nombresColumnas.Contains(columna.Nombre)).ToArray();
            var nombreTablaRelacionada = CrearNombreTabla(tabla, siguienteNumeroTabla++);
            tablas.Add(CrearTabla(nombreTablaRelacionada, columnasRelacionadas, determinante));
            referenciasBase.Add(new(determinante, nombreTablaRelacionada, determinante));
            columnasBase.RemoveAll(columna => dependientes.Contains(columna.Nombre, StringComparer.Ordinal));
        }

        tablas[0] = CrearTabla(CrearNombreTabla(tabla, 1), columnasBase, clavePrincipal) with { ClavesForaneas = tablas[0].ClavesForaneas.Concat(referenciasBase).ToArray() };
        return AgregarReferenciasForaneas(tablas);
    }

    private static IReadOnlyList<TablaNormalizada> CrearTablasTerceraFormaNormal(
        TablaNormalizacion tabla,
        IReadOnlyList<TablaNormalizada> tablasIniciales,
        IReadOnlyList<DependenciaFuncional> dependencias)
    {
        var siguienteNumeroTabla = tablasIniciales.Count + 1;
        var tablas = tablasIniciales
            .SelectMany(tablaNormalizada => DescomponerDependencias(tabla, tablaNormalizada, dependencias, ref siguienteNumeroTabla))
            .OrderBy(tablaNormalizada => tablaNormalizada.Nombre, StringComparer.Ordinal)
            .ToArray();

        return AgregarReferenciasForaneas(tablas);
    }

    private static IReadOnlyList<TablaNormalizada> DescomponerDependencias(
        TablaNormalizacion tablaOrigen,
        TablaNormalizada tabla,
        IReadOnlyList<DependenciaFuncional> dependencias,
        ref int siguienteNumeroTabla)
    {
        var columnasBase = tabla.Columnas.ToList();
        var gruposDependencias = dependencias
            .Where(dependencia => EsDependenciaParaSeparar(dependencia, tabla, columnasBase))
            .GroupBy(dependencia => string.Join("|", dependencia.Determinante), StringComparer.Ordinal)
            .ToArray();

        if (gruposDependencias.Length == 0)
        {
            return [tabla];
        }

        var tablasRelacionadas = new List<TablaNormalizada>();
        var referenciasBase = tabla.ClavesForaneas.ToList();
        foreach (var grupo in gruposDependencias)
        {
            var determinante = grupo.First().Determinante;
            var dependientes = grupo.Select(dependencia => dependencia.Dependiente)
                .Where(dependiente => columnasBase.Any(columna => columna.Nombre == dependiente))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (dependientes.Length == 0)
            {
                continue;
            }

            var nombresColumnas = determinante.Concat(dependientes).ToHashSet(StringComparer.Ordinal);
            var columnasRelacionadas = tablaOrigen.Columnas.Where(columna => nombresColumnas.Contains(columna.Nombre)).ToArray();
            var nombreTablaRelacionada = CrearNombreTabla(tablaOrigen, siguienteNumeroTabla++);
            tablasRelacionadas.Add(CrearTabla(nombreTablaRelacionada, columnasRelacionadas, determinante));
            referenciasBase.Add(new(determinante, nombreTablaRelacionada, determinante));
            columnasBase.RemoveAll(columna => dependientes.Contains(columna.Nombre, StringComparer.Ordinal));
        }

        var referenciasVigentes = referenciasBase
            .Where(referencia => referencia.Columnas.All(nombre => columnasBase.Any(columna => columna.Nombre == nombre)))
            .ToArray();
        var tablaBase = tabla with { Columnas = columnasBase, ClavesForaneas = referenciasVigentes };
        var resultado = new List<TablaNormalizada> { tablaBase };
        foreach (var tablaRelacionada in tablasRelacionadas)
        {
            resultado.AddRange(DescomponerDependencias(tablaOrigen, tablaRelacionada, dependencias, ref siguienteNumeroTabla));
        }

        return resultado;
    }

    private static bool EsDependenciaParaSeparar(
        DependenciaFuncional dependencia,
        TablaNormalizada tabla,
        IReadOnlyList<ColumnaNormalizacion> columnasDisponibles)
    {
        return dependencia.Determinante.Count > 0
            && !dependencia.Determinante.SequenceEqual(tabla.ClavePrimaria)
            && !tabla.ClavePrimaria.Contains(dependencia.Dependiente, StringComparer.Ordinal)
            && !EsDeterminanteSoloForaneo(dependencia.Determinante, columnasDisponibles)
            && dependencia.Determinante.All(nombre => columnasDisponibles.Any(columna => columna.Nombre == nombre))
            && columnasDisponibles.Any(columna => columna.Nombre == dependencia.Dependiente)
            && (dependencia.Determinante.Count < tabla.ClavePrimaria.Count
                && dependencia.Determinante.All(nombre => tabla.ClavePrimaria.Contains(nombre, StringComparer.Ordinal))
                || EsDeterminanteElegible(dependencia.Determinante));
    }

    private static bool EsDeterminanteSoloForaneo(IReadOnlyList<string> determinante, IReadOnlyList<ColumnaNormalizacion> columnasDisponibles)
    {
        return determinante.All(nombre => columnasDisponibles.Any(columna => columna.Nombre == nombre && columna.EsClaveForanea));
    }

    private static IReadOnlyList<string> BuscarColumnasGruposRepetidos(TablaNormalizacion tabla, IReadOnlyList<string> clavePrincipal)
    {
        return tabla.Columnas
            .Where(columna => !clavePrincipal.Contains(columna.Nombre, StringComparer.Ordinal)
                && EsNombrePlural(columna.Nombre)
                && TieneListasSeparadas(tabla, columna.Nombre))
            .Select(columna => columna.Nombre)
            .ToArray();
    }

    private static IReadOnlyList<string> BuscarColumnasNulasFrecuentes(TablaNormalizacion tabla, IReadOnlyList<string> clavePrincipal)
    {
        return tabla.Columnas
            .Where(columna => !clavePrincipal.Contains(columna.Nombre, StringComparer.Ordinal)
                && tabla.Registros.Count > 0
                && tabla.Registros.Count(registro => !registro.Valores.TryGetValue(columna.Nombre, out var valor) || valor is null) * 2 >= tabla.Registros.Count)
            .Select(columna => columna.Nombre)
            .ToArray();
    }

    private static bool EsNombrePlural(string nombreColumna)
    {
        return nombreColumna.EndsWith("s", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TieneListasSeparadas(TablaNormalizacion tabla, string nombreColumna)
    {
        var valores = tabla.Registros
            .Select(registro => registro.Valores.TryGetValue(nombreColumna, out var valor) ? valor : null)
            .Where(valor => !string.IsNullOrWhiteSpace(valor))
            .ToArray();

        return valores.Length >= 2
            && new[] { ';', ',', '|' }.Any(separador => valores.Count(valor => valor!.Split(separador, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length > 1) >= 2);
    }

    private static IReadOnlyList<TablaNormalizada> SepararRelacionesDeclaradas(
        TablaNormalizacion tabla,
        IReadOnlyList<string> clavePrincipal,
        int siguienteNumeroTabla,
        IReadOnlyList<TablaNormalizacion> tablasEntrada)
    {
        var columnasBase = tabla.Columnas.ToList();
        var tablasRelacionadas = new List<TablaNormalizada>();
        var referenciasBase = new List<ReferenciaForanea>();

        foreach (var claveForanea in tabla.Columnas.Where(columna => columna.EsClaveForanea))
        {
            var contexto = ObtenerContexto(claveForanea.Nombre);
            if (contexto is null)
            {
                continue;
            }

            var dependientes = tabla.Columnas
                .Where(columna => !columna.EsClavePrimaria
                    && !columna.EsClaveForanea
                    && columna.Nombre.EndsWith($"_{contexto}", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (dependientes.Length == 0)
            {
                continue;
            }

            var tablaExterna = BuscarTablaExterna(tabla, tablasEntrada, claveForanea, dependientes);
            if (tablaExterna is not null)
            {
                referenciasBase.Add(new([claveForanea.Nombre], CrearNombreTabla(tablaExterna, 1), [claveForanea.Nombre]));
                columnasBase.RemoveAll(columna => dependientes.Contains(columna));
                continue;
            }

            var clavePrincipalRelacionada = claveForanea with { EsClavePrimaria = true, EsClaveForanea = false };
            var columnasRelacionadas = new[] { clavePrincipalRelacionada }.Concat(dependientes).ToArray();
            var nombreTablaRelacionada = CrearNombreTabla(tabla, siguienteNumeroTabla++);
            tablasRelacionadas.Add(CrearTabla(nombreTablaRelacionada, columnasRelacionadas, [claveForanea.Nombre]));
            referenciasBase.Add(new([claveForanea.Nombre], nombreTablaRelacionada, [claveForanea.Nombre]));
            columnasBase.RemoveAll(columna => dependientes.Contains(columna));
        }

        tablasRelacionadas.Insert(0, CrearTabla(CrearNombreTabla(tabla, 1), columnasBase, clavePrincipal) with { ClavesForaneas = referenciasBase });
        return tablasRelacionadas;
    }

    private static TablaNormalizacion? BuscarTablaExterna(
        TablaNormalizacion tablaOrigen,
        IReadOnlyList<TablaNormalizacion> tablasEntrada,
        ColumnaNormalizacion claveForanea,
        IReadOnlyList<ColumnaNormalizacion> dependientes)
    {
        var coincidencias = tablasEntrada
            .Where(tabla => tabla.Numero != tablaOrigen.Numero
                && tabla.Columnas.Count(columna => columna.EsClavePrimaria) == 1
                && tabla.Columnas.Any(columna => columna.EsClavePrimaria
                    && columna.Nombre == claveForanea.Nombre
                    && string.Equals(columna.TipoDato, claveForanea.TipoDato, StringComparison.OrdinalIgnoreCase))
                && dependientes.All(dependiente => tabla.Columnas.Any(columna => columna.Nombre == dependiente.Nombre
                    && string.Equals(columna.TipoDato, dependiente.TipoDato, StringComparison.OrdinalIgnoreCase))))
            .ToArray();

        return coincidencias.Length == 1 ? coincidencias[0] : null;
    }

    private static IReadOnlyList<TablaNormalizada> CompletarTablasTerceraFormaNormal(IReadOnlyList<TablaNormalizada> tablasTerceraFormaNormal, IReadOnlyList<TablaNormalizada> tablasGruposRepetidos)
    {
        return AgregarReferenciasForaneas(tablasTerceraFormaNormal.Concat(tablasGruposRepetidos).OrderBy(tabla => tabla.Nombre, StringComparer.Ordinal).ToArray());
    }

    private static string? ObtenerContexto(string nombre)
    {
        var prefijo = new[] { "id_", "codigo_", "cod_" }
            .FirstOrDefault(valor => nombre.StartsWith(valor, StringComparison.OrdinalIgnoreCase));
        return prefijo is null || nombre.Length == prefijo.Length ? null : nombre[prefijo.Length..];
    }

    private static IReadOnlyList<TablaNormalizada> AgregarReferenciasForaneas(IReadOnlyList<TablaNormalizada> tablas)
    {
        return tablas.Select(tabla =>
        {
            var referencias = tabla.ClavesForaneas
                .GroupBy(referencia => $"{string.Join('|', referencia.Columnas)}>{referencia.TablaDestino}>{string.Join('|', referencia.ColumnasDestino)}", StringComparer.Ordinal)
                .Select(grupo => grupo.First())
                .ToArray();
            return tabla with { ClavesForaneas = referencias };
        }).ToArray();
    }
}
