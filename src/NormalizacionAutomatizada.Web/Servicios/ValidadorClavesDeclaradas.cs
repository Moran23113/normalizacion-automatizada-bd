using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class ValidadorClavesDeclaradas
{
    public IReadOnlyList<string> Validar(TablaNormalizacion tabla)
    {
        var columnasClavePrimaria = tabla.Columnas.Where(columna => columna.EsClavePrimaria).ToArray();

        if (columnasClavePrimaria.Length == 0)
        {
            return [];
        }

        var columnasConNulos = columnasClavePrimaria
            .Where(columna => tabla.Registros.Any(registro => ObtenerValor(registro, columna.Nombre) is null))
            .Select(columna => columna.Nombre)
            .ToArray();

        if (columnasConNulos.Length > 0)
        {
            return [$"La clave primaria de 'Tabla_{tabla.Numero}' no puede contener valores nulos: {string.Join(", ", columnasConNulos)}."];
        }

        var grupoDuplicado = tabla.Registros
            .GroupBy(registro => CrearIdentificadorClave(registro, columnasClavePrimaria), StringComparer.Ordinal)
            .FirstOrDefault(grupo => grupo.Count() > 1);

        if (grupoDuplicado is null)
        {
            return [];
        }

        var valorDuplicado = grupoDuplicado.First();
        var descripcionValores = string.Join(", ", columnasClavePrimaria.Select(columna => $"{columna.Nombre} = '{ObtenerValor(valorDuplicado, columna.Nombre)}'"));
        return [$"La clave primaria de 'Tabla_{tabla.Numero}' contiene un valor duplicado: {descripcionValores}."];
    }

    private static string CrearIdentificadorClave(RegistroOriginal registro, IReadOnlyList<ColumnaNormalizacion> columnasClavePrimaria)
    {
        return string.Join('\u001F', columnasClavePrimaria.Select(columna => ObtenerValor(registro, columna.Nombre) ?? "<NULL>"));
    }

    private static string? ObtenerValor(RegistroOriginal registro, string nombreColumna)
    {
        return registro.Valores.TryGetValue(nombreColumna, out var valor) ? valor : null;
    }
}
