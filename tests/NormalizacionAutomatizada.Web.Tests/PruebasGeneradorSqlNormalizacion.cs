using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasGeneradorSqlNormalizacion
{
    [Fact]
    public void Generar_CuandoRecibeTablasNormalizadas_DevuelveLasClavesPrimariasYForaneasEnSql()
    {
        var tablas = new[]
        {
            new TablaNormalizada("Tabla_01_01", [new("id_estudiante", "INT", true, false), new("id_carrera", "INT", false, false)], ["id_estudiante"], [new(["id_carrera"], "Tabla_01_02", ["id_carrera"])]),
            new TablaNormalizada("Tabla_01_02", [new("id_carrera", "INT", true, false), new("nombre_carrera", "VARCHAR(100)", false, false)], ["id_carrera"], [])
        };

        var sql = new GeneradorSqlNormalizacion().Generar(tablas);

        Assert.Contains("CREATE TABLE [Tabla_01_01]", sql);
        Assert.Contains("[id_estudiante] INT NOT NULL", sql);
        Assert.Contains("CONSTRAINT [PK_Tabla_01_01] PRIMARY KEY ([id_estudiante])", sql);
        Assert.Contains("CONSTRAINT [FK_Tabla_01_01_Tabla_01_02_id_carrera] FOREIGN KEY ([id_carrera]) REFERENCES [Tabla_01_02] ([id_carrera])", sql);
    }
}
