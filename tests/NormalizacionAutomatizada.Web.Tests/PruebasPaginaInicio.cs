using System.Net;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc.Testing;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasPaginaInicio(WebApplicationFactory<global::Program> fabrica) : IClassFixture<WebApplicationFactory<global::Program>>
{
    private static readonly Regex PatronTokenAntifalsificacion = new("""name="__RequestVerificationToken" type="hidden" value="(?<token>[^"]+)""", RegexOptions.CultureInvariant);
    private readonly HttpClient cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    [Fact]
    public async Task ObtenerInicio_DevuelveElEnlaceDeDescargaYElFormularioDeCarga()
    {
        var respuesta = await cliente.GetAsync("/");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("href=\"/plantillas/Plantilla_Maestra_Normalizacion_Simplificada_v2.xlsx\"", html);
        Assert.Contains("enctype=\"multipart/form-data\"", html);
        Assert.Contains("name=\"ArchivoPlantilla\"", html);
    }

    [Fact]
    public async Task ObtenerPlantillaMaestra_DevuelveUnArchivoExcel()
    {
        var respuesta = await cliente.GetAsync("/plantillas/Plantilla_Maestra_Normalizacion_Simplificada_v2.xlsx");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await respuesta.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ObtenerPlantillaMaestra_AlAnalizarla_SoloRequiereRegistrosParaContinuar()
    {
        var respuesta = await cliente.GetAsync("/plantillas/Plantilla_Maestra_Normalizacion_Simplificada_v2.xlsx");
        var contenido = await respuesta.Content.ReadAsByteArrayAsync();
        using var flujo = new MemoryStream(contenido);
        var analizador = new AnalizadorPlantillaExcel();

        var resultado = analizador.Analizar(flujo);

        Assert.False(resultado.EsValido);
        Assert.Equal(["La hoja 'Datos_01' debe contener al menos un registro para analizar."], resultado.Errores);
    }

    [Fact]
    public async Task ObtenerPlantillaDePrueba_DevuelveUnLibroConTresTablasParaDepuracion()
    {
        var respuesta = await cliente.GetAsync("/plantillas/Plantilla_Prueba_Depuracion_3_Tablas.xlsx");
        var contenido = await respuesta.Content.ReadAsByteArrayAsync();
        using var flujo = new MemoryStream(contenido);
        var analizador = new AnalizadorPlantillaExcel();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var resultado = analizador.Analizar(flujo);

        Assert.True(resultado.EsValido, string.Join(" ", resultado.Errores));
        Assert.Equal(3, resultado.Tablas.Count);
        Assert.All(resultado.Tablas, tabla => Assert.True(tabla.CantidadRegistros >= 3));
        Assert.Equal("88.50", resultado.Entrada!.Tablas[2].Registros[0].Valores["nota_final"]);
    }

    [Fact]
    public async Task EnviarInicio_SinArchivo_MuestraElMensajeCorrespondiente()
    {
        var respuesta = await EnviarArchivoAsync();
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Selecciona un archivo Excel antes de continuar.", html);
    }

    [Fact]
    public async Task EnviarInicio_ConArchivoQueNoEsXlsx_MuestraElMensajeDeExtension()
    {
        var respuesta = await EnviarArchivoAsync("datos.txt");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("El archivo debe tener extension .xlsx.", html);
    }

    [Fact]
    public async Task EnviarInicio_ConArchivoXlsxIlegible_MuestraElMensajeDelLibro()
    {
        var respuesta = await EnviarArchivoAsync("datos.xlsx");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("No se pudo leer el archivo como un libro Excel valido.", html);
    }

    [Fact]
    public async Task EnviarInicio_ConLibroCompatible_MuestraElResumenDeLaTabla()
    {
        var respuesta = await EnviarArchivoAsync("datos.xlsx", CrearLibroCompatible());
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Plantilla v&aacute;lida.", html);
        Assert.Contains("Se importaron 1 tabla y 1 registro.", html);
        Assert.Contains("Tabla_01", html);
        Assert.Contains("2 columnas y 1 registro importado.", html);
        Assert.Contains("Los datos originales se guardaron temporalmente para el siguiente paso.", html);
        Assert.Contains("Vista previa de datos importados", html);
        Assert.Contains("id_estudiante", html);
        Assert.Contains("Ana", html);
        Assert.Contains("IAlmacenEntradaNormalizacion.Obtener()", html);
    }

    [Fact]
    public async Task ObtenerInicio_DespuesDeUnaCargaValida_MuestraLosDatosGuardadosEnSesion()
    {
        await EnviarArchivoAsync("datos.xlsx", CrearLibroCompatible());

        var respuesta = await cliente.GetAsync("/");
        var html = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Vista previa de datos importados", html);
        Assert.Contains("Ana", html);
        Assert.Contains("IAlmacenEntradaNormalizacion.Obtener()", html);
    }

    [Fact]
    public async Task ObtenerSql_DespuesDeUnaCargaValida_DevuelveElScriptDeTablasNormalizadas()
    {
        await EnviarArchivoAsync("datos.xlsx", CrearLibroCompatible());

        var respuesta = await cliente.GetAsync("/?handler=Sql");
        var sql = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("text/sql", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("CREATE TABLE [Tabla_01_01]", sql);
        Assert.Contains("CONSTRAINT [PK_Tabla_01_01] PRIMARY KEY ([id_estudiante])", sql);
    }

    private async Task<HttpResponseMessage> EnviarArchivoAsync(string? nombreArchivo = null, byte[]? contenidoArchivo = null)
    {
        var inicio = await cliente.GetStringAsync("/");
        var coincidencia = PatronTokenAntifalsificacion.Match(inicio);
        Assert.True(coincidencia.Success, "La pagina debe incluir un token antiforgery para aceptar cargas.");

        using var contenido = new MultipartFormDataContent();
        contenido.Add(new StringContent(WebUtility.HtmlDecode(coincidencia.Groups["token"].Value)), "__RequestVerificationToken");

        if (nombreArchivo is not null)
        {
            contenido.Add(new ByteArrayContent(contenidoArchivo ?? [1]), "ArchivoPlantilla", nombreArchivo);
        }

        return await cliente.PostAsync("/", contenido);
    }

    private static byte[] CrearLibroCompatible()
    {
        using var libro = new XLWorkbook();
        var estructura = libro.Worksheets.Add("Tabla_01");
        estructura.Cell(1, 1).Value = "tabla";
        estructura.Cell(1, 2).Value = "columna";
        estructura.Cell(1, 3).Value = "tipo";
        estructura.Cell(1, 4).Value = "Primary Key";
        estructura.Cell(1, 5).Value = "Foreign Key";
        estructura.Cell(2, 1).Value = "Tabla";
        estructura.Cell(2, 2).Value = "id_estudiante";
        estructura.Cell(2, 3).Value = "INT";
        estructura.Cell(2, 4).Value = "No";
        estructura.Cell(2, 5).Value = "No";
        estructura.Cell(3, 1).Value = "Tabla";
        estructura.Cell(3, 2).Value = "nombre_estudiante";
        estructura.Cell(3, 3).Value = "VARCHAR(100)";
        estructura.Cell(3, 4).Value = "No";
        estructura.Cell(3, 5).Value = "No";

        var datos = libro.Worksheets.Add("Datos_01");
        datos.Cell(1, 1).Value = "id_estudiante";
        datos.Cell(1, 2).Value = "nombre_estudiante";
        datos.Cell(2, 1).Value = "1";
        datos.Cell(2, 2).Value = "Ana";

        using var flujo = new MemoryStream();
        libro.SaveAs(flujo);
        return flujo.ToArray();
    }
}
