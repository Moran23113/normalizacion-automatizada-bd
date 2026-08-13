using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Pages;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasPaginaInicioModel
{
    [Fact]
    public void OnPost_CuandoElLibroEsValido_GuardaLaEntradaExtraida()
    {
        var almacen = new AlmacenEntradaRegistro();
        var pagina = CrearPagina(almacen);
        pagina.ArchivoPlantilla = CrearArchivoLibroCompatible();

        pagina.OnPost();

        var tabla = Assert.Single(almacen.Entrada!.Tablas);
        Assert.Equal("01", tabla.Numero);
        Assert.Equal(new ColumnaNormalizacion("id_estudiante", "INT", true, false), tabla.Columnas[0]);
        Assert.Equal("Ana", Assert.Single(tabla.Registros).Valores["nombre_estudiante"]);
    }

    [Fact]
    public void OnPost_CuandoElLibroEsInvalido_NoGuardaLaEntrada()
    {
        var almacen = new AlmacenEntradaRegistro();
        var pagina = CrearPagina(almacen);
        pagina.ArchivoPlantilla = CrearArchivo("datos.xlsx", [1]);

        pagina.OnPost();

        Assert.Null(almacen.Entrada);
    }

    private static PaginaInicioModel CrearPagina(IAlmacenEntradaNormalizacion almacen)
    {
        return new PaginaInicioModel(new ValidadorArchivoExcel(), new AnalizadorPlantillaExcel(), almacen);
    }

    private static IFormFile CrearArchivoLibroCompatible()
    {
        using var workbook = new XLWorkbook();
        var structure = workbook.Worksheets.Add("Tabla_01");
        structure.Cell(1, 1).Value = "tabla";
        structure.Cell(1, 2).Value = "columna";
        structure.Cell(1, 3).Value = "tipo";
        structure.Cell(1, 4).Value = "Primary Key";
        structure.Cell(1, 5).Value = "Foreign Key";
        structure.Cell(2, 1).Value = "Tabla";
        structure.Cell(2, 2).Value = "id_estudiante";
        structure.Cell(2, 3).Value = "INT";
        structure.Cell(2, 4).Value = "Si";
        structure.Cell(2, 5).Value = "No";
        structure.Cell(3, 1).Value = "Tabla";
        structure.Cell(3, 2).Value = "nombre_estudiante";
        structure.Cell(3, 3).Value = "VARCHAR(100)";
        structure.Cell(3, 4).Value = "No";
        structure.Cell(3, 5).Value = "No";

        var data = workbook.Worksheets.Add("Datos_01");
        data.Cell(1, 1).Value = "id_estudiante";
        data.Cell(1, 2).Value = "nombre_estudiante";
        data.Cell(2, 1).Value = "1";
        data.Cell(2, 2).Value = "Ana";

        using var contenido = new MemoryStream();
        workbook.SaveAs(contenido);
        return CrearArchivo("datos.xlsx", contenido.ToArray());
    }

    private static IFormFile CrearArchivo(string nombreArchivo, byte[] contenido)
    {
        var contenidoArchivo = new MemoryStream(contenido);
        return new FormFile(contenidoArchivo, 0, contenidoArchivo.Length, "ArchivoPlantilla", nombreArchivo);
    }

    private sealed class AlmacenEntradaRegistro : IAlmacenEntradaNormalizacion
    {
        public EntradaNormalizacion? Entrada { get; private set; }

        public void Guardar(EntradaNormalizacion entrada)
        {
            Entrada = entrada;
        }

        public EntradaNormalizacion? Obtener()
        {
            return Entrada;
        }
    }
}
