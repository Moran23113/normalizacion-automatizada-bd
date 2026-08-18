using System.Text;
using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class GeneradorSqlNormalizacion
{
    public string Generar(IReadOnlyList<TablaNormalizada> tablas)
    {
        var sql = new StringBuilder();

        foreach (var tabla in tablas)
        {
            sql.AppendLine($"CREATE TABLE [{tabla.Nombre}] (");
            var definiciones = tabla.Columnas.Select(columna => $"    [{columna.Nombre}] {columna.TipoDato} {(tabla.ClavePrimaria.Contains(columna.Nombre, StringComparer.Ordinal) ? "NOT NULL" : "NULL")}").ToList();

            if (tabla.ClavePrimaria.Count > 0)
            {
                definiciones.Add($"    CONSTRAINT [PK_{tabla.Nombre}] PRIMARY KEY ({string.Join(", ", tabla.ClavePrimaria.Select(columna => $"[{columna}]"))})");
            }

            sql.AppendLine(string.Join(",\n", definiciones));
            sql.AppendLine(");");
            sql.AppendLine();
        }

        foreach (var tabla in tablas)
        {
            foreach (var referencia in tabla.ClavesForaneas)
            {
                var nombre = $"FK_{tabla.Nombre}_{referencia.TablaDestino}_{string.Join("_", referencia.Columnas)}";
                sql.AppendLine($"ALTER TABLE [{tabla.Nombre}] ADD CONSTRAINT [{nombre}] FOREIGN KEY ({string.Join(", ", referencia.Columnas.Select(columna => $"[{columna}]"))}) REFERENCES [{referencia.TablaDestino}] ({string.Join(", ", referencia.ColumnasDestino.Select(columna => $"[{columna}]"))});");
            }
        }

        return sql.ToString();
    }
}
