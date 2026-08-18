using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasCompletadorReferenciasForaneas
{
    [Fact]
    public void Completar_CuandoLaClaveForaneaCoincideConUnaClavePrimariaUnica_AgregaLaReferencia()
    {
        var entrada = new EntradaNormalizacion(
        [
            new("01", [new("id_carrera", "INT", true, false)], []),
            new("02", [new("id_estudiante", "INT", true, false), new("id_carrera", "INT", false, true)], []),
            new("03", [new("id_matricula", "INT", true, false), new("id_estudiante", "INT", false, true), new("id_carrera", "INT", false, true)], [])
        ]);
        var resultados = new[]
        {
            Resultado("01", "Tabla_01_01", [new("id_carrera", "INT", true, false)], ["id_carrera"]),
            Resultado("02", "Tabla_02_01", [new("id_estudiante", "INT", true, false), new("id_carrera", "INT", false, true)], ["id_estudiante"]),
            Resultado("03", "Tabla_03_01", [new("id_matricula", "INT", true, false), new("id_estudiante", "INT", false, true), new("id_carrera", "INT", false, true)], ["id_matricula"])
        };

        var resultado = new CompletadorReferenciasForaneas().Completar(entrada, resultados);

        var tablaEstudiante = Tabla(resultado, "02", "Tabla_02_01");
        var tablaMatricula = Tabla(resultado, "03", "Tabla_03_01");
        Assert.Contains(tablaEstudiante.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_carrera"]) && referencia.TablaDestino == "Tabla_01_01");
        Assert.Contains(tablaMatricula.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_estudiante"]) && referencia.TablaDestino == "Tabla_02_01");
        Assert.Contains(tablaMatricula.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_carrera"]) && referencia.TablaDestino == "Tabla_01_01");
    }

    [Fact]
    public void Completar_CuandoLaClaveForaneaTieneDosDestinosPosibles_NoInventaLaReferencia()
    {
        var entrada = new EntradaNormalizacion(
        [
            new("01", [new("id_codigo", "INT", true, false)], []),
            new("02", [new("id_codigo", "INT", true, false)], []),
            new("03", [new("id_registro", "INT", true, false), new("id_codigo", "INT", false, true)], [])
        ]);
        var resultados = new[]
        {
            Resultado("01", "Tabla_01_01", [new("id_codigo", "INT", true, false)], ["id_codigo"]),
            Resultado("02", "Tabla_02_01", [new("id_codigo", "INT", true, false)], ["id_codigo"]),
            Resultado("03", "Tabla_03_01", [new("id_registro", "INT", true, false), new("id_codigo", "INT", false, true)], ["id_registro"])
        };

        var resultado = new CompletadorReferenciasForaneas().Completar(entrada, resultados);

        var tablaRegistro = Tabla(resultado, "03", "Tabla_03_01");
        var resultadoRegistro = Assert.Single(resultado, tabla => tabla.Numero == "03");
        Assert.Empty(tablaRegistro.ClavesForaneas);
        Assert.Contains("La clave foranea 'id_codigo' de Tabla_03 tiene mas de un posible destino y no se genero una referencia.", resultadoRegistro.Advertencias);
    }

    [Fact]
    public void Completar_CuandoLaTablaYaTieneLaReferenciaInterna_NoAgregaUnaAdvertencia()
    {
        var entrada = new EntradaNormalizacion(
        [
            new("01", [new("id_estudiante", "INT", true, false), new("id_curso", "INT", false, true)], [])
        ]);
        var tablaMatricula = new TablaNormalizada(
            "Tabla_01_01",
            [new("id_estudiante", "INT", true, false), new("id_curso", "INT", false, true)],
            ["id_estudiante"],
            [new(["id_curso"], "Tabla_01_02", ["id_curso"])]);
        var tablaCurso = new TablaNormalizada("Tabla_01_02", [new("id_curso", "INT", true, false), new("nombre_curso", "VARCHAR(100)", false, false)], ["id_curso"], []);
        var resultados = new[] { new ResultadoTablaNormalizada("01", [], [], [tablaMatricula], [tablaMatricula], [tablaMatricula, tablaCurso], []) };

        var resultado = new CompletadorReferenciasForaneas().Completar(entrada, resultados);

        var resultadoMatricula = Assert.Single(resultado);
        Assert.DoesNotContain(resultadoMatricula.Advertencias, advertencia => advertencia.Contains("id_curso", StringComparison.Ordinal));
    }

    private static ResultadoTablaNormalizada Resultado(string numero, string nombre, IReadOnlyList<ColumnaNormalizacion> columnas, IReadOnlyList<string> clavePrimaria)
    {
        var tabla = new TablaNormalizada(nombre, columnas, clavePrimaria, []);
        return new(numero, [], [], [tabla], [tabla], [tabla], []);
    }

    private static TablaNormalizada Tabla(IReadOnlyList<ResultadoTablaNormalizada> resultados, string numero, string nombre)
    {
        return Assert.Single(Assert.Single(resultados, resultado => resultado.Numero == numero).TablasTerceraFormaNormal, tabla => tabla.Nombre == nombre);
    }
}
