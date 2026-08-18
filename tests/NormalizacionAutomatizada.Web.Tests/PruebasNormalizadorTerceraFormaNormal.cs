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
    public void Normalizar_CuandoUnaClaveForaneaRepetidaPareceDeterminarOtrosCampos_NoCreaUnaEntidadSinContexto()
    {
        var tabla = new TablaNormalizacion(
            "03",
            [
                new("id_matricula", "INT", true, false),
                new("id_estudiante", "INT", false, true),
                new("id_carrera", "INT", false, true),
                new("codigo_curso", "VARCHAR(10)", false, false),
                new("periodo", "VARCHAR(20)", false, false),
                new("nota_final", "DECIMAL(5,2)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_matricula"] = "5001", ["id_estudiante"] = "1001", ["id_carrera"] = "1", ["codigo_curso"] = "BD-201", ["periodo"] = "2026-02", ["nota_final"] = "88.50" }),
                new(new Dictionary<string, string?> { ["id_matricula"] = "5002", ["id_estudiante"] = "1002", ["id_carrera"] = "2", ["codigo_curso"] = "ADM-101", ["periodo"] = "2026-02", ["nota_final"] = "91.00" }),
                new(new Dictionary<string, string?> { ["id_matricula"] = "5003", ["id_estudiante"] = "1003", ["id_carrera"] = "1", ["codigo_curso"] = "BD-201", ["periodo"] = "2026-02", ["nota_final"] = "85.75" }),
                new(new Dictionary<string, string?> { ["id_matricula"] = "5004", ["id_estudiante"] = "1004", ["id_carrera"] = "3", ["codigo_curso"] = "DER-110", ["periodo"] = "2026-02", ["nota_final"] = "79.25" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        Assert.DoesNotContain(resultado.TablasTerceraFormaNormal, tablaNormalizada => tablaNormalizada.Nombre != "Tabla_03_01" && tablaNormalizada.ClavePrimaria.SequenceEqual(["id_carrera"]));
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

    [Fact]
    public void Normalizar_CuandoUnCampoPluralContieneListasSeparadas_CreaUnaTablaAtomicaEnPrimeraFormaNormal()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("id_estudiante", "INT", true, false),
                new("nombre_estudiante", "VARCHAR(100)", false, false),
                new("telefonos", "VARCHAR(30)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["nombre_estudiante"] = "Ana", ["telefonos"] = "9999-0001; 9999-0002" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["nombre_estudiante"] = "Luis", ["telefonos"] = "9999-0003; 9999-0004" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        var tablaEstudiante = Assert.Single(resultado.TablasPrimeraFormaNormal, tabla => tabla.Nombre == "Tabla_01_01");
        var tablaTelefonos = Assert.Single(resultado.TablasPrimeraFormaNormal, tabla => tabla.Nombre == "Tabla_01_02");
        Assert.DoesNotContain(tablaEstudiante.Columnas, columna => columna.Nombre == "telefonos");
        Assert.Equal(["id_estudiante", "telefonos"], tablaTelefonos.Columnas.Select(columna => columna.Nombre));
        Assert.Equal(["id_estudiante", "telefonos"], tablaTelefonos.ClavePrimaria);
        Assert.Contains(tablaTelefonos.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_estudiante"]) && referencia.TablaDestino == "Tabla_01_01");
    }

    [Fact]
    public void Normalizar_CuandoUnaClaveCompuestaTieneDependenciasParciales_CreaRelacionesEnSegundaFormaNormal()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("id_estudiante", "INT", true, false),
                new("id_curso", "INT", true, false),
                new("nombre_estudiante", "VARCHAR(100)", false, false),
                new("nombre_curso", "VARCHAR(100)", false, false),
                new("nota_final", "DECIMAL(5,2)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "10", ["nombre_estudiante"] = "Ana", ["nombre_curso"] = "Base de Datos II", ["nota_final"] = "90" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["id_curso"] = "20", ["nombre_estudiante"] = "Ana", ["nombre_curso"] = "Programacion I", ["nota_final"] = "85" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["id_curso"] = "10", ["nombre_estudiante"] = "Luis", ["nombre_curso"] = "Base de Datos II", ["nota_final"] = "88" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["id_curso"] = "20", ["nombre_estudiante"] = "Luis", ["nombre_curso"] = "Programacion I", ["nota_final"] = "92" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        var tablaMatricula = Assert.Single(resultado.TablasSegundaFormaNormal, tabla => tabla.Nombre == "Tabla_01_01");
        var tablaEstudiante = Assert.Single(resultado.TablasSegundaFormaNormal, tabla => tabla.Nombre == "Tabla_01_02");
        var tablaCurso = Assert.Single(resultado.TablasSegundaFormaNormal, tabla => tabla.Nombre == "Tabla_01_03");
        Assert.Equal(["id_estudiante", "id_curso", "nota_final"], tablaMatricula.Columnas.Select(columna => columna.Nombre));
        Assert.Equal(["id_estudiante", "nombre_estudiante"], tablaEstudiante.Columnas.Select(columna => columna.Nombre));
        Assert.Equal(["id_curso", "nombre_curso"], tablaCurso.Columnas.Select(columna => columna.Nombre));
        Assert.Contains(tablaMatricula.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_estudiante"]) && referencia.TablaDestino == "Tabla_01_02");
        Assert.Contains(tablaMatricula.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_curso"]) && referencia.TablaDestino == "Tabla_01_03");
    }

    [Fact]
    public void Normalizar_CuandoExisteUnaTablaExternaParaLaClaveForanea_ReutilizaLaTablaExistenteEnTerceraFormaNormal()
    {
        var tablaMatricula = new TablaNormalizacion(
            "01",
            [
                new("id_matricula", "INT", true, false),
                new("id_curso", "INT", false, true),
                new("nombre_curso", "VARCHAR(100)", false, false)
            ],
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

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tablaMatricula, [tablaMatricula, tablaCurso]);

        var tablaFinal = Assert.Single(resultado.TablasTerceraFormaNormal);
        Assert.DoesNotContain(tablaFinal.Columnas, columna => columna.Nombre == "nombre_curso");
        Assert.Contains(tablaFinal.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_curso"]) && referencia.TablaDestino == "Tabla_02_01");
    }

    [Fact]
    public void Normalizar_CuandoUnaDependenciaParcialNoUsaPrefijosTecnicos_LaConservaEnLaSalidaDeTerceraFormaNormal()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("estudiante", "INT", true, false),
                new("curso", "INT", true, false),
                new("nombre_estudiante", "VARCHAR(100)", false, false),
                new("nombre_curso", "VARCHAR(100)", false, false),
                new("nota_final", "DECIMAL(5,2)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["estudiante"] = "1", ["curso"] = "10", ["nombre_estudiante"] = "Ana", ["nombre_curso"] = "Base de Datos II", ["nota_final"] = "90" }),
                new(new Dictionary<string, string?> { ["estudiante"] = "1", ["curso"] = "20", ["nombre_estudiante"] = "Ana", ["nombre_curso"] = "Programacion I", ["nota_final"] = "85" }),
                new(new Dictionary<string, string?> { ["estudiante"] = "2", ["curso"] = "10", ["nombre_estudiante"] = "Luis", ["nombre_curso"] = "Base de Datos II", ["nota_final"] = "88" }),
                new(new Dictionary<string, string?> { ["estudiante"] = "2", ["curso"] = "20", ["nombre_estudiante"] = "Luis", ["nombre_curso"] = "Programacion I", ["nota_final"] = "92" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        Assert.Equal(3, resultado.TablasTerceraFormaNormal.Count);
        Assert.Equal(["estudiante", "curso", "nota_final"], Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_01").Columnas.Select(columna => columna.Nombre));
        Assert.Equal(["estudiante", "nombre_estudiante"], Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_02").Columnas.Select(columna => columna.Nombre));
        Assert.Equal(["curso", "nombre_curso"], Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_03").Columnas.Select(columna => columna.Nombre));
    }

    [Fact]
    public void Normalizar_CuandoUnAtributoTieneNulosFrecuentes_CreaUnaRelacionOpcional()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [new("id_estudiante", "INT", true, false), new("telefono_secundario", "VARCHAR(30)", false, false)],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["telefono_secundario"] = null }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["telefono_secundario"] = null }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "3", ["telefono_secundario"] = "9999-0001" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        var tablaEstudiante = Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_01");
        var tablaTelefonoOpcional = Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_02");
        Assert.DoesNotContain(tablaEstudiante.Columnas, columna => columna.Nombre == "telefono_secundario");
        Assert.Equal(["id_estudiante", "telefono_secundario"], tablaTelefonoOpcional.Columnas.Select(columna => columna.Nombre));
        Assert.Equal(["id_estudiante"], tablaTelefonoOpcional.ClavePrimaria);
        Assert.Contains(tablaTelefonoOpcional.ClavesForaneas, referencia => referencia.Columnas.SequenceEqual(["id_estudiante"]) && referencia.TablaDestino == "Tabla_01_01");
        Assert.DoesNotContain(tablaEstudiante.ClavesForaneas, referencia => referencia.TablaDestino == "Tabla_01_03");
    }

    [Fact]
    public void Normalizar_CuandoCombinaUnGrupoRepetidoYUnaRelacionOpcional_NoCreaClavesForaneasInvertidas()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [
                new("id_estudiante", "INT", true, false),
                new("telefonos", "VARCHAR(30)", false, false),
                new("telefono_secundario", "VARCHAR(30)", false, false)
            ],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1", ["telefonos"] = "9999-0001; 9999-0002", ["telefono_secundario"] = null }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "2", ["telefonos"] = "9999-0003; 9999-0004", ["telefono_secundario"] = null }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "3", ["telefonos"] = "9999-0005; 9999-0006", ["telefono_secundario"] = "9999-0010" })
            ]);

        var resultado = new NormalizadorTerceraFormaNormal().Normalizar(tabla);

        var tablaBase = Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_01");
        var tablaTelefonos = Assert.Single(resultado.TablasTerceraFormaNormal, tabla => tabla.Nombre == "Tabla_01_02");
        Assert.DoesNotContain(tablaBase.ClavesForaneas, referencia => referencia.TablaDestino == "Tabla_01_03");
        Assert.DoesNotContain(tablaTelefonos.ClavesForaneas, referencia => referencia.TablaDestino == "Tabla_01_03");
    }
}
