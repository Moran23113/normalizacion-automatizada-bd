using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasEvaluadorCumplimientoNormalizacion
{
    [Fact]
    public void Evaluar_CuandoUnaColumnaTieneNulosFrecuentes_BloqueaLaExportacionSql()
    {
        var tablaOrigen = new TablaNormalizacion(
            "01",
            [new("id_estudiante", "INT", true, false), new("telefono_secundario", "VARCHAR(30)", false, false)],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["telefono_secundario"] = null }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["telefono_secundario"] = null }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "3", ["telefono_secundario"] = "9999-0001" })
            ]);
        var tablaFinal = new TablaNormalizada("Tabla_01_01", tablaOrigen.Columnas, ["id_estudiante"], []);
        var resultado = new ResultadoTablaNormalizada("01", [], [], [tablaFinal], [tablaFinal], [tablaFinal], []);

        var resultadoEvaluado = new EvaluadorCumplimientoNormalizacion().Evaluar(new EntradaNormalizacion([tablaOrigen]), [resultado]);

        var evaluacion = Assert.Single(resultadoEvaluado).Evaluacion;
        Assert.NotNull(evaluacion);
        Assert.False(evaluacion.PuedeExportarSql);
        Assert.Contains(evaluacion.Directrices, directriz => directriz.Nombre == "Directriz 3" && directriz.Estado == "Requiere correccion");
    }

    [Fact]
    public void Evaluar_CuandoLasTablasTienenClavesYNoHayHallazgosBloqueantes_AutorizaLaExportacionSql()
    {
        var tablaOrigen = new TablaNormalizacion(
            "01",
            [new("id_estudiante", "INT", true, false), new("nombre_estudiante", "VARCHAR(100)", false, false)],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["nombre_estudiante"] = "Ana" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["nombre_estudiante"] = "Luis" })
            ]);
        var tablaFinal = new TablaNormalizada("Tabla_01_01", tablaOrigen.Columnas, ["id_estudiante"], []);
        var resultado = new ResultadoTablaNormalizada("01", [], [], [tablaFinal], [tablaFinal], [tablaFinal], []);

        var resultadoEvaluado = new EvaluadorCumplimientoNormalizacion().Evaluar(new EntradaNormalizacion([tablaOrigen]), [resultado]);

        var evaluacion = Assert.Single(resultadoEvaluado).Evaluacion;
        Assert.NotNull(evaluacion);
        Assert.True(evaluacion.PuedeExportarSql);
        Assert.Equal(3, evaluacion.FormasNormales.Count);
        Assert.Equal(4, evaluacion.Directrices.Count);
    }

    [Fact]
    public void Evaluar_CuandoLosNulosFrecuentesSeSeparanEnRelacionOpcional_AutorizaLaExportacionSql()
    {
        var tablaOrigen = new TablaNormalizacion(
            "01",
            [new("id_estudiante", "INT", true, false), new("telefono_secundario", "VARCHAR(30)", false, false)],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["telefono_secundario"] = null }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["telefono_secundario"] = null }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "3", ["telefono_secundario"] = "9999-0001" })
            ]);
        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tablaOrigen);

        var resultadoEvaluado = new EvaluadorCumplimientoNormalizacion().Evaluar(new EntradaNormalizacion([tablaOrigen]), [resultado]);

        var evaluacion = Assert.Single(resultadoEvaluado).Evaluacion;
        Assert.True(evaluacion.PuedeExportarSql);
        Assert.Contains(evaluacion.Directrices, directriz => directriz.Nombre == "Directriz 1" && directriz.Estado == "Cumple");
        Assert.Contains(evaluacion.Directrices, directriz => directriz.Nombre == "Directriz 2" && directriz.Estado == "Cumple");
        Assert.Contains(evaluacion.Directrices, directriz => directriz.Nombre == "Directriz 3" && directriz.Estado == "Corregida automaticamente");
    }

    [Fact]
    public void Evaluar_CuandoUnaRelacionExternaConservaElDescriptor_NoReportaPerdidaDeColumnas()
    {
        var tablaMatricula = new TablaNormalizacion(
            "01",
            [new("id_matricula", "INT", true, false), new("id_curso", "INT", false, true), new("nombre_curso", "VARCHAR(100)", false, false)],
            [
                new(new Dictionary<string, string?> { ["id_matricula"] = "1", ["id_curso"] = "10", ["nombre_curso"] = "Base de Datos II" }),
                new(new Dictionary<string, string?> { ["id_matricula"] = "2", ["id_curso"] = "20", ["nombre_curso"] = "Programacion I" })
            ]);
        var tablaCurso = new TablaNormalizacion(
            "02",
            [new("id_curso", "INT", true, false), new("nombre_curso", "VARCHAR(100)", false, false)],
            [
                new(new Dictionary<string, string?> { ["id_curso"] = "10", ["nombre_curso"] = "Base de Datos II" }),
                new(new Dictionary<string, string?> { ["id_curso"] = "20", ["nombre_curso"] = "Programacion I" })
            ]);
        var entrada = new EntradaNormalizacion([tablaMatricula, tablaCurso]);
        var normalizador = new NormalizadorTerceraFormaNormal();
        var resultados = entrada.Tablas.Select(tabla => normalizador.Normalizar(tabla, entrada.Tablas)).ToArray();
        var resultadosConReferencias = new CompletadorReferenciasForaneas().Completar(entrada, resultados);

        var resultadoEvaluado = new EvaluadorCumplimientoNormalizacion().Evaluar(entrada, resultadosConReferencias);

        Assert.True(Assert.Single(resultadoEvaluado, resultado => resultado.Numero == "01").Evaluacion.PuedeExportarSql);
    }
}
