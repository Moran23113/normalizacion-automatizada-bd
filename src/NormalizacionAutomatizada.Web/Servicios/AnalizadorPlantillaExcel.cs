using System.Text.RegularExpressions;
using ClosedXML.Excel;
using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class AnalizadorPlantillaExcel : IAnalizadorPlantillaExcel
{
    private static readonly string[] EncabezadosEstructura = ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"];
    private static readonly Regex PatronHojaTabla = new("^Tabla_(?<numero>\\d{2})$", RegexOptions.CultureInvariant);
    private static readonly Regex PatronHojaDatos = new("^Datos_(?<numero>\\d{2})$", RegexOptions.CultureInvariant);

    public ResultadoAnalisisLibro Analizar(Stream contenido)
    {
        try
        {
            using var libro = new XLWorkbook(contenido);
            return AnalizarLibro(libro);
        }
        catch (Exception)
        {
            return new([], ["No se pudo leer el archivo como un libro Excel valido."]);
        }
    }

    private static ResultadoAnalisisLibro AnalizarLibro(XLWorkbook libro)
    {
        var hojasTablas = BuscarHojas(libro, PatronHojaTabla);
        var hojasDatos = BuscarHojas(libro, PatronHojaDatos);
        var errores = new List<string>();
        var resumenes = new List<ResumenTablaAnalizada>();
        var tablasImportadas = new List<TablaNormalizacion>();

        if (hojasTablas.Count == 0)
        {
            errores.Add("No se encontro ninguna hoja con el formato 'Tabla_XX'.");
        }

        ValidarNumerosConsecutivos(hojasTablas.Keys, "Tabla", errores);

        foreach (var numero in hojasTablas.Keys.OrderBy(numero => numero, StringComparer.Ordinal))
        {
            if (!hojasDatos.ContainsKey(numero))
            {
                errores.Add($"Falta la hoja 'Datos_{numero}' para completar el par de 'Tabla_{numero}'.");
            }
        }

        foreach (var numero in hojasDatos.Keys.OrderBy(numero => numero, StringComparer.Ordinal))
        {
            if (!hojasTablas.ContainsKey(numero))
            {
                errores.Add($"La hoja 'Datos_{numero}' no tiene una hoja 'Tabla_{numero}' asociada.");
            }
        }

        foreach (var numero in hojasTablas.Keys.Intersect(hojasDatos.Keys).OrderBy(numero => numero, StringComparer.Ordinal))
        {
            var hojaTabla = hojasTablas[numero];
            var hojaDatos = hojasDatos[numero];
            var columnasEstructura = ValidarEstructura(hojaTabla, errores);

            if (columnasEstructura is null)
            {
                continue;
            }

            var encabezadosDatos = LeerEncabezados(hojaDatos);
            if (!columnasEstructura.Select(columna => columna.Nombre).SequenceEqual(encabezadosDatos, StringComparer.Ordinal))
            {
                errores.Add($"Las columnas de 'Datos_{numero}' deben coincidir, en el mismo orden, con las de 'Tabla_{numero}'.");
                continue;
            }

            var registros = LeerRegistros(hojaDatos, columnasEstructura);
            if (registros.Count == 0)
            {
                errores.Add($"La hoja 'Datos_{numero}' debe contener al menos un registro para analizar.");
                continue;
            }

            var tablaImportada = new TablaNormalizacion(numero, columnasEstructura, registros);
            var erroresClavePrimaria = new ValidadorClavesDeclaradas().Validar(tablaImportada);
            if (erroresClavePrimaria.Count > 0)
            {
                errores.AddRange(erroresClavePrimaria);
                continue;
            }

            resumenes.Add(new(numero, columnasEstructura.Count, registros.Count));
            tablasImportadas.Add(tablaImportada);
        }

        return new(resumenes, errores)
        {
            Entrada = errores.Count == 0 ? new EntradaNormalizacion(tablasImportadas) : null
        };
    }

    private static Dictionary<string, IXLWorksheet> BuscarHojas(XLWorkbook libro, Regex patron)
    {
        return libro.Worksheets
            .Select(hoja => (Hoja: hoja, Coincidencia: patron.Match(hoja.Name)))
            .Where(elemento => elemento.Coincidencia.Success)
            .ToDictionary(elemento => elemento.Coincidencia.Groups["numero"].Value, elemento => elemento.Hoja, StringComparer.Ordinal);
    }

    private static void ValidarNumerosConsecutivos(IEnumerable<string> numeros, string prefijoHoja, ICollection<string> errores)
    {
        var numerosOrdenados = numeros.OrderBy(numero => numero, StringComparer.Ordinal).ToArray();

        if (numerosOrdenados.Length == 0)
        {
            return;
        }

        var numerosEsperados = Enumerable.Range(1, numerosOrdenados.Length).Select(numero => numero.ToString("00"));
        if (!numerosOrdenados.SequenceEqual(numerosEsperados, StringComparer.Ordinal))
        {
            errores.Add($"Las hojas '{prefijoHoja}_XX' deben iniciar en 01 y numerarse sin saltos.");
        }
    }

    private static IReadOnlyList<ColumnaNormalizacion>? ValidarEstructura(IXLWorksheet hoja, ICollection<string> errores)
    {
        var numero = hoja.Name["Tabla_".Length..];
        var encabezados = LeerEncabezados(hoja);

        if (!EncabezadosEstructura.SequenceEqual(encabezados, StringComparer.Ordinal))
        {
            errores.Add($"La hoja 'Tabla_{numero}' debe tener los encabezados: tabla, columna, tipo, Primary Key y Foreign Key.");
            return null;
        }

        var columnas = new List<ColumnaNormalizacion>();
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;

        for (var fila = 2; fila <= ultimaFila; fila++)
        {
            var valores = EncabezadosEstructura.Select((_, columna) => hoja.Cell(fila, columna + 1).GetText().Trim()).ToArray();

            if (valores.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (!string.Equals(valores[0], "Tabla", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(valores[1]) || string.IsNullOrWhiteSpace(valores[2]) || !EsValorBooleano(valores[3]) || !EsValorBooleano(valores[4]))
            {
                errores.Add($"Cada fila usada de 'Tabla_{numero}' debe indicar Tabla, columna, tipo y valores Si o No para Primary Key y Foreign Key.");
                return null;
            }

            columnas.Add(new(valores[1], valores[2], EsValorAfirmativo(valores[3]), EsValorAfirmativo(valores[4])));
        }

        if (columnas.Count == 0)
        {
            errores.Add($"La hoja 'Tabla_{numero}' debe definir al menos una columna.");
            return null;
        }

        if (columnas.Select(columna => columna.Nombre).Distinct(StringComparer.Ordinal).Count() != columnas.Count)
        {
            errores.Add($"La hoja 'Tabla_{numero}' no puede repetir nombres de columna.");
            return null;
        }

        return columnas;
    }

    private static bool EsValorBooleano(string valor)
    {
        return string.Equals(valor, "Si", StringComparison.OrdinalIgnoreCase)
            || string.Equals(valor, "S\u00ED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(valor, "No", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EsValorAfirmativo(string valor)
    {
        return string.Equals(valor, "Si", StringComparison.OrdinalIgnoreCase)
            || string.Equals(valor, "S\u00ED", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> LeerEncabezados(IXLWorksheet hoja)
    {
        var ultimaColumna = hoja.LastColumnUsed()?.ColumnNumber() ?? 0;
        return Enumerable.Range(1, ultimaColumna).Select(columna => hoja.Cell(1, columna).GetText().Trim()).ToArray();
    }

    private static IReadOnlyList<RegistroOriginal> LeerRegistros(IXLWorksheet hoja, IReadOnlyList<ColumnaNormalizacion> columnas)
    {
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 1;
        var registros = new List<RegistroOriginal>();

        foreach (var fila in Enumerable.Range(2, Math.Max(0, ultimaFila - 1)))
        {
            var valores = columnas
                .Select((columna, indice) => (columna.Nombre, Valor: hoja.Cell(fila, indice + 1).GetFormattedString()))
                .ToDictionary(elemento => elemento.Nombre, elemento => ConvertirValorNulo(elemento.Valor), StringComparer.Ordinal);

            if (valores.Values.All(valor => valor is null))
            {
                continue;
            }

            registros.Add(new(valores));
        }

        return registros;
    }

    private static string? ConvertirValorNulo(string valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            || string.Equals(valor.Trim(), "NULL", StringComparison.OrdinalIgnoreCase)
            ? null
            : valor;
    }
}
