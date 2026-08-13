using Microsoft.AspNetCore.Http;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasValidadorArchivoExcel
{
    private readonly IValidadorArchivoExcel validador = new ValidadorArchivoExcel();

    [Fact]
    public void Validar_CuandoNoHayArchivo_DevuelveResultadoInvalido()
    {
        var resultado = validador.Validar(null);

        Assert.False(resultado.EsValido);
        Assert.Equal("Selecciona un archivo Excel antes de continuar.", resultado.MensajeError);
    }

    [Theory]
    [InlineData("datos.xls")]
    [InlineData("datos.csv")]
    [InlineData("datos.txt")]
    public void Validar_CuandoLaExtensionNoEsXlsx_DevuelveResultadoInvalido(string nombreArchivo)
    {
        var resultado = validador.Validar(CrearArchivo(nombreArchivo, 1));

        Assert.False(resultado.EsValido);
        Assert.Equal("El archivo debe tener extension .xlsx.", resultado.MensajeError);
    }

    [Fact]
    public void Validar_CuandoElArchivoXlsxEstaVacio_DevuelveResultadoInvalido()
    {
        var resultado = validador.Validar(CrearArchivo("datos.xlsx"));

        Assert.False(resultado.EsValido);
        Assert.Equal("El archivo Excel no puede estar vacio.", resultado.MensajeError);
    }

    [Fact]
    public void Validar_CuandoElArchivoXlsxTieneContenido_DevuelveResultadoValido()
    {
        var resultado = validador.Validar(CrearArchivo("datos.XLSX", 1));

        Assert.True(resultado.EsValido);
        Assert.Null(resultado.MensajeError);
    }

    private static IFormFile CrearArchivo(string nombreArchivo, params byte[] contenido)
    {
        var contenidoArchivo = new MemoryStream(contenido);
        return new FormFile(contenidoArchivo, 0, contenidoArchivo.Length, "ArchivoPlantilla", nombreArchivo);
    }
}
