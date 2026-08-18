using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasCalculadorClavesCandidatas
{
    [Fact]
    public void Calcular_CuandoUnaColumnaNoDeclaradaEsUnica_DevuelveLaClaveCandidataSimple()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("codigo_estudiante", "INT", false, false),
                new("nombre_estudiante", "VARCHAR(100)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["codigo_estudiante"] = "1001", ["nombre_estudiante"] = "Ana" }),
                new(new Dictionary<string, string?> { ["codigo_estudiante"] = "1002", ["nombre_estudiante"] = "Ana" })
            ]);

        var claves = new CalculadorClavesCandidatas().Calcular(tabla);

        var clave = Assert.Single(claves);
        Assert.Equal(["codigo_estudiante"], clave.Columnas);
    }

    [Fact]
    public void Calcular_CuandoSoloLaCombinacionDeDosColumnasEsUnica_DevuelveLaClaveCandidataCompuesta()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("id_estudiante", "INT", false, false),
                new("id_curso", "INT", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "10" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "20" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["id_curso"] = "10" })
            ]);

        var claves = new CalculadorClavesCandidatas().Calcular(tabla);

        var clave = Assert.Single(claves);
        Assert.Equal(["id_estudiante", "id_curso"], clave.Columnas);
    }

    [Fact]
    public void Calcular_CuandoSoloLaCombinacionDeTresColumnasEsUnica_DevuelveLaClaveCandidataCompuesta()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("id_estudiante", "INT", false, false),
                new("id_curso", "INT", false, false),
                new("periodo", "VARCHAR(10)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "10", ["periodo"] = "2025-01" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "10", ["periodo"] = "2025-02" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "20", ["periodo"] = "2025-01" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["id_curso"] = "10", ["periodo"] = "2025-01" })
            ]);

        var claves = new CalculadorClavesCandidatas().Calcular(tabla);

        var clave = Assert.Single(claves);
        Assert.Equal(["id_estudiante", "id_curso", "periodo"], clave.Columnas);
    }
}
