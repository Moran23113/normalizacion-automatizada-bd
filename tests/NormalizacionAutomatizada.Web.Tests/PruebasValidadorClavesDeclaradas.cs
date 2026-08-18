using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasValidadorClavesDeclaradas
{
    [Fact]
    public void Validar_CuandoLaClavePrimariaDeclaradaSeRepite_DevuelveElValorDuplicado()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [new("id_estudiante", "INT", true, false)],
            [
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1001" }),
                new(new Dictionary<string, string?> { ["id_estudiante"] = "1001" })
            ]);

        var errores = new ValidadorClavesDeclaradas().Validar(tabla);

        Assert.Equal(["La clave primaria de 'Tabla_01' contiene un valor duplicado: id_estudiante = '1001'."], errores);
    }

    [Fact]
    public void Validar_CuandoLaClavePrimariaDeclaradaContieneNulo_DevuelveElNombreDeLaColumna()
    {
        var tabla = new TablaNormalizacion(
            "01",
            [new("id_estudiante", "INT", true, false)],
            [new(new Dictionary<string, string?> { ["id_estudiante"] = null })]);

        var errores = new ValidadorClavesDeclaradas().Validar(tabla);

        Assert.Equal(["La clave primaria de 'Tabla_01' no puede contener valores nulos: id_estudiante."], errores);
    }
}
