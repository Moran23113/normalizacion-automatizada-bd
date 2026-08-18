using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasNormalizadorTerceraFormaNormal
{
    [Fact]
    public void Normalizar_CuandoExisteUnaDependenciaTransitiva_SeparaLaRelacionEnTerceraFormaNormal()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("id_estudiante", "INT", false, false),
                new("nombre_estudiante", "VARCHAR(100)", false, false),
                new("id_carrera", "INT", false, false),
                new("nombre_carrera", "VARCHAR(100)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1001", ["nombre_estudiante"] = "Ana", ["id_carrera"] = "1", ["nombre_carrera"] = "Ingenieria" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1002", ["nombre_estudiante"] = "Luis", ["id_carrera"] = "2", ["nombre_carrera"] = "Derecho" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1003", ["nombre_estudiante"] = "Maria", ["id_carrera"] = "1", ["nombre_carrera"] = "Ingenieria" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        Assert.Equal(["id_estudiante"], Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_01").ClavePrimaria);
        Assert.Equal(["id_carrera", "nombre_carrera"], Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_02").Columnas.Select(columna => columna.Nombre));
        Assert.Contains(Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_01").ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_carrera"]) && referencia.TablaDestino == "Tabla_01_02" && referencia.ColumnasDestino.SequenceEqual(["id_carrera"]));
    }

    [Fact]
    public void Normalizar_CuandoLaTablaYaDeclaraUnaClaveForanea_NoCreaEntidadesPorUnaMuestraPequena()
    {
        var tabla = new TablaNormalizacion(
            "03",
            [new("id_matricula", "INT", true, false), new("id_estudiante", "INT", false, true), new("periodo", "VARCHAR(20)", false, false)],
            [
                new(new Dictionary<string, string?> { ["id_matricula"] = "1", ["id_estudiante"] = "1001", ["periodo"] = "2026-02" }),
                new(new Dictionary<string, string?> { ["id_matricula"] = "2", ["id_estudiante"] = "1002", ["periodo"] = "2026-02" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        Assert.Single(resultado.TablasTerceraFormaNormal);
    }

    [Fact]
    public void Normalizar_CuandoLaClaveForaneaIncluyeUnDescriptorDelMismoContexto_SeparaLaTablaRelacionada()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("id_estudiante", "INT", true, false),
                new("nombre_estudiante", "VARCHAR(100)", false, false),
                new("id_curso", "INT", false, true),
                new("nombre_curso", "VARCHAR(100)", false, false),
                new("nota_final", "DECIMAL(5,2)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["nombre_estudiante"] = "Ana", ["id_curso"] = "101", ["nombre_curso"] = "Base de Datos II", ["nota_final"] = "95.50" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["nombre_estudiante"] = "Luis", ["id_curso"] = "102", ["nombre_curso"] = "Programacion I", ["nota_final"] = "88.00" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        var tablaMatricula = Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_01");
        var tablaCurso = Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_02");
        Assert.Equal(["id_curso", "nombre_curso"], tablaCurso.Columnas.Select(columna => columna.Nombre));
        Assert.Equal(new ColumnaNormalizacion("id_curso", "INT", true, false), tablaCurso.Columnas[0]);
        Assert.DoesNotContain(tablaMatricula.Columnas, columna => columna.Nombre == "nombre_curso");
        Assert.Contains(tablaMatricula.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_curso"]) && referencia.TablaDestino == "Tabla_01_02");
    }
}
