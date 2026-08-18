using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasDetectorDependenciasFuncionales
{
    [Fact]
    public void Detectar_CuandoUnCodigoSiempreTieneElMismoNombre_DevuelveLaDependenciaFuncionalObservada()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("codigo_carrera", "INT", false, false),
                new("nombre_carrera", "VARCHAR(100)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["codigo_carrera"] = "1", ["nombre_carrera"] = "Ingenieria" }),
                new(new Dictionary<string, string?> { ["codigo_carrera"] = "1", ["nombre_carrera"] = "Ingenieria" }),
                new(new Dictionary<string, string?> { ["codigo_carrera"] = "2", ["nombre_carrera"] = "Derecho" })
            ]);

        var dependencias = new DetectorDependenciasFuncionales().Detectar(tabla);

        Assert.Contains(dependencias, dependencia => dependencia.Determinante.SequenceEqual(["codigo_carrera"]) && dependencia.Dependiente == "nombre_carrera");
    }

    [Fact]
    public void Detectar_CuandoDosColumnasDeterminanLaNota_DevuelveLaDependenciaFuncionalCompuesta()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("id_estudiante", "INT", false, false),
                new("id_curso", "INT", false, false),
                new("periodo", "VARCHAR(10)", false, false),
                new("nota_final", "DECIMAL(5,2)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "10", ["periodo"] = "2025-01", ["nota_final"] = "90" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "10", ["periodo"] = "2025-02", ["nota_final"] = "90" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "20", ["periodo"] = "2025-01", ["nota_final"] = "80" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["id_curso"] = "10", ["periodo"] = "2025-01", ["nota_final"] = "85" })
            ]);

        var dependencias = new DetectorDependenciasFuncionales().Detectar(tabla);

        Assert.Contains(dependencias, dependencia => dependencia.Determinante.SequenceEqual(["id_estudiante", "id_curso"]) && dependencia.Dependiente == "nota_final");
    }
}
