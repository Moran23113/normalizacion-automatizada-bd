using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pages;

public class PaginaInicioModel : PageModel
{
    private readonly IValidadorArchivoExcel validadorArchivoExcel;
    private readonly IAnalizadorPlantillaExcel analizadorPlantillaExcel;
    private readonly IAlmacenEntradaNormalizacion almacenEntradaNormalizacion;
    private readonly NormalizadorTerceraFormaNormal normalizadorTerceraFormaNormal = new();
    private readonly CompletadorReferenciasForaneas completadorReferenciasForaneas = new();
    private readonly EvaluadorCumplimientoNormalizacion evaluadorCumplimientoNormalizacion = new();
    private readonly GeneradorSqlNormalizacion generadorSqlNormalizacion = new();

    public PaginaInicioModel(IValidadorArchivoExcel validadorArchivoExcel, IAnalizadorPlantillaExcel analizadorPlantillaExcel, IAlmacenEntradaNormalizacion almacenEntradaNormalizacion)
    {
        this.validadorArchivoExcel = validadorArchivoExcel;
        this.analizadorPlantillaExcel = analizadorPlantillaExcel;
        this.almacenEntradaNormalizacion = almacenEntradaNormalizacion;
    }

    [BindProperty]
    public IFormFile? ArchivoPlantilla { get; set; }

    public ResultadoValidacionArchivo? ResultadoValidacion { get; private set; }

    public ResultadoAnalisisLibro? ResultadoAnalisis { get; private set; }

    public EntradaNormalizacion? EntradaCargada { get; private set; }

    public IReadOnlyList<ResultadoTablaNormalizada> ResultadosNormalizacion { get; private set; } = [];

    public bool PuedeExportarSql => ResultadosNormalizacion.Count > 0 && ResultadosNormalizacion.All(resultado => resultado.Evaluacion.PuedeExportarSql);

    public void OnGet()
    {
        EntradaCargada = almacenEntradaNormalizacion.Obtener();
        ResultadosNormalizacion = Normalizar(EntradaCargada);
    }

    public void OnPost()
    {
        EntradaCargada = almacenEntradaNormalizacion.Obtener();
        ResultadoValidacion = validadorArchivoExcel.Validar(ArchivoPlantilla);

        if (!ResultadoValidacion.EsValido)
        {
            return;
        }

        using var contenido = ArchivoPlantilla!.OpenReadStream();
        ResultadoAnalisis = analizadorPlantillaExcel.Analizar(contenido);

        if (ResultadoAnalisis is { EsValido: true, Entrada: not null } analisis)
        {
            almacenEntradaNormalizacion.Guardar(analisis.Entrada);
            EntradaCargada = analisis.Entrada;
            ResultadosNormalizacion = Normalizar(EntradaCargada);
        }
    }

    public IActionResult OnGetSql()
    {
        var entrada = almacenEntradaNormalizacion.Obtener();
        if (entrada is null)
        {
            return RedirectToPage();
        }

        var resultados = Normalizar(entrada);
        if (resultados.Any(resultado => !resultado.Evaluacion.PuedeExportarSql))
        {
            return BadRequest("No se puede exportar SQL mientras existan criterios de normalizacion pendientes.");
        }

        var tablas = resultados.SelectMany(resultado => resultado.TablasTerceraFormaNormal).ToArray();
        var sql = generadorSqlNormalizacion.Generar(tablas);
        return File(Encoding.UTF8.GetBytes(sql), "text/sql", "tablas-normalizadas.sql");
    }

    private IReadOnlyList<ResultadoTablaNormalizada> Normalizar(EntradaNormalizacion? entrada)
    {
        if (entrada is null)
        {
            return [];
        }

        var resultados = entrada.Tablas.Select(tabla => normalizadorTerceraFormaNormal.Normalizar(tabla, entrada.Tablas)).ToArray();
        var resultadosConReferencias = completadorReferenciasForaneas.Completar(entrada, resultados);
        return evaluadorCumplimientoNormalizacion.Evaluar(entrada, resultadosConReferencias);
    }
}
