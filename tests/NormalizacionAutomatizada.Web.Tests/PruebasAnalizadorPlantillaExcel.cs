using ClosedXML.Excel;
using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasAnalizadorPlantillaExcel
{
    [Fact]
    public void Analyze_WhenWorkbookHasOneValidPair_ReturnsItsSummary()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"],
                ["Tabla", "nombre_estudiante", "VARCHAR(100)", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante", "nombre_estudiante"],
                ["1", "Ana"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.True(result.EsValido);
        var table = Assert.Single(result.Tablas);
        Assert.Equal("01", table.Numero);
        Assert.Equal(2, table.CantidadColumnas);
        Assert.Equal(1, table.CantidadRegistros);
    }

    [Fact]
    public void Analyze_WhenWorkbookIsValid_ExtractsColumnsAndOriginalRows()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "Si", "No"],
                ["Tabla", "nombre_estudiante", "VARCHAR(100)", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante", "nombre_estudiante"],
                ["1", "Ana"],
                ["2", "Luis"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.True(result.EsValido);
        var input = Assert.IsType<EntradaNormalizacion>(result.Entrada);
        var table = Assert.Single(input.Tablas);
        Assert.Collection(
            table.Columnas,
            first => Assert.Equal(new ColumnaNormalizacion("id_estudiante", "INT", true, false), first),
            second => Assert.Equal(new ColumnaNormalizacion("nombre_estudiante", "VARCHAR(100)", false, false), second));
        Assert.Equal("1", table.Registros[0].Valores["id_estudiante"]);
        Assert.Equal("Ana", table.Registros[0].Valores["nombre_estudiante"]);
        Assert.Equal("Luis", table.Registros[1].Valores["nombre_estudiante"]);
    }

    [Fact]
    public void Analyze_WhenDataRowContainsAnEmptyCell_ExtractsItAsNull()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"],
                ["Tabla", "telefono", "VARCHAR(20)", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante", "telefono"],
                ["1", ""]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.True(result.EsValido);
        var table = Assert.Single(result.Entrada!.Tablas);
        Assert.Null(Assert.Single(table.Registros).Valores["telefono"]);
    }

    [Fact]
    public void Analyze_WhenDataSheetIsMissing_ReportsTheIncompletePair()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("Falta la hoja 'Datos_01' para completar el par de 'Tabla_01'.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenDataHeadersDoNotMatchStructure_ReportsTheMismatch()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"]),
            Sheet("Datos_01", ["identificador"],
                ["1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("Las columnas de 'Datos_01' deben coincidir, en el mismo orden, con las de 'Tabla_01'.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenDataSheetHasNoRecords_ReportsThatItCannotBeAnalyzed()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("La hoja 'Datos_01' debe contener al menos un registro para analizar.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenWorkbookHasTwoPairsAndExampleSheets_ReturnsBothSummaries()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante"],
                ["1"]),
            Sheet("Tabla_02", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_curso", "INT", "No", "No"],
                ["Tabla", "nombre_curso", "VARCHAR(100)", "No", "No"]),
            Sheet("Datos_02", ["id_curso", "nombre_curso"],
                ["1", "Bases de datos"]),
            Sheet("Ejemplo_Tabla_01", ["cualquier", "encabezado"]),
            Sheet("Ejemplo_Datos_01", ["dato"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.True(result.EsValido);
        Assert.Collection(
            result.Tablas,
            first => Assert.Equal(new ResumenTablaAnalizada("01", 1, 1), first),
            second => Assert.Equal(new ResumenTablaAnalizada("02", 2, 1), second));
    }

    [Fact]
    public void Analyze_WhenDataSheetHasNoStructureSheet_ReportsTheOrphanSheet()
    {
        using var workbook = CreateWorkbook(
            Sheet("Datos_01", ["id_estudiante"],
                ["1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("La hoja 'Datos_01' no tiene una hoja 'Tabla_01' asociada.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenPairNumbersSkipANumber_ReportsTheNumberingError()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante"],
                ["1"]),
            Sheet("Tabla_03", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_curso", "INT", "No", "No"]),
            Sheet("Datos_03", ["id_curso"],
                ["1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("Las hojas 'Tabla_XX' deben iniciar en 01 y numerarse sin saltos.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenStructureHasAnInvalidKeyValue_ReportsTheInvalidMetadata()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "Tal vez", "No"]),
            Sheet("Datos_01", ["id_estudiante"],
                ["1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("Cada fila usada de 'Tabla_01' debe indicar Tabla, columna, tipo y valores Si o No para Primary Key y Foreign Key.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenStructureHeadersAreIncorrect_ReportsTheExpectedHeaders()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["nombre", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante"],
                ["1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("La hoja 'Tabla_01' debe tener los encabezados: tabla, columna, tipo, Primary Key y Foreign Key.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenStructureRepeatsAColumnName_ReportsTheDuplicate()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "No", "No"],
                ["Tabla", "id_estudiante", "VARCHAR(100)", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante", "id_estudiante"],
                ["1", "1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("La hoja 'Tabla_01' no puede repetir nombres de columna.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenStructureUsesATableNameInsteadOfTabla_ReportsTheRequiredLiteral()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Matricula", "id_estudiante", "INT", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante"],
                ["1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("Cada fila usada de 'Tabla_01' debe indicar Tabla, columna, tipo y valores Si o No para Primary Key y Foreign Key.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenStructureUsesSiAndSiWithAccentForKeys_AcceptsBothValues()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "Si", "No"],
                ["Tabla", "id_carrera", "INT", "No", "Sí"]),
            Sheet("Datos_01", ["id_estudiante", "id_carrera"],
                ["1", "2"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.True(result.EsValido);
    }

    [Fact]
    public void Analyze_WhenDeclaredPrimaryKeyContainsDuplicatedValues_ReportsTheInvalidKey()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                ["Tabla", "id_estudiante", "INT", "Si", "No"],
                ["Tabla", "nombre_estudiante", "VARCHAR(100)", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante", "nombre_estudiante"],
                ["1", "Ana"],
                ["1", "Luis"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Null(result.Entrada);
        Assert.Contains("La clave primaria de 'Tabla_01' contiene un valor duplicado: id_estudiante = '1'.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenStructureContainsBlankRows_IgnoresThem()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"],
                [" ", " ", " ", " ", " "],
                ["Tabla", "id_estudiante", "INT", "No", "No"]),
            Sheet("Datos_01", ["id_estudiante"],
                ["1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.True(result.EsValido);
        Assert.Equal(new ResumenTablaAnalizada("01", 1, 1), Assert.Single(result.Tablas));
    }

    [Fact]
    public void Analyze_WhenStructureDoesNotDefineColumns_ReportsTheMissingDefinition()
    {
        using var workbook = CreateWorkbook(
            Sheet("Tabla_01", ["tabla", "columna", "tipo", "Primary Key", "Foreign Key"]),
            Sheet("Datos_01", ["id_estudiante"],
                ["1"]));
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(workbook);

        Assert.False(result.EsValido);
        Assert.Contains("La hoja 'Tabla_01' debe definir al menos una columna.", result.Errores);
    }

    [Fact]
    public void Analyze_WhenContentIsNotAnExcelWorkbook_ReportsTheReadError()
    {
        using var content = new MemoryStream([1, 2, 3]);
        var analyzer = new AnalizadorPlantillaExcel();

        var result = analyzer.Analizar(content);

        Assert.False(result.EsValido);
        Assert.Equal(["No se pudo leer el archivo como un libro Excel valido."], result.Errores);
    }

    private static MemoryStream CreateWorkbook(params (string SheetName, string[] Headers, string[][] Registros)[] sheets)
    {
        using var excel = new XLWorkbook();

        foreach (var sheet in sheets)
        {
            var worksheet = excel.Worksheets.Add(sheet.SheetName);

            for (var column = 0; column < sheet.Headers.Length; column++)
            {
                worksheet.Cell(1, column + 1).Value = sheet.Headers[column];
            }

            for (var row = 0; row < sheet.Registros.Length; row++)
            {
                for (var column = 0; column < sheet.Registros[row].Length; column++)
                {
                    worksheet.Cell(row + 2, column + 1).Value = sheet.Registros[row][column];
                }
            }
        }

        var stream = new MemoryStream();
        excel.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static (string SheetName, string[] Headers, string[][] Rows) Sheet(string sheetName, string[] headers, params string[][] rows)
    {
        return (sheetName, headers, rows);
    }
}
