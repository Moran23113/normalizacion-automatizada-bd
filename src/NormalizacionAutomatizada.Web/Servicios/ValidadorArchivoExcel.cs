using Microsoft.AspNetCore.Http;
using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class ValidadorArchivoExcel : IValidadorArchivoExcel
{
    public ResultadoValidacionArchivo Validar(IFormFile? archivo)
    {
        if (archivo is null)
        {
            return new(false, "Selecciona un archivo Excel antes de continuar.");
        }

        if (!string.Equals(Path.GetExtension(archivo.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return new(false, "El archivo debe tener extension .xlsx.");
        }

        if (archivo.Length == 0)
        {
            return new(false, "El archivo Excel no puede estar vacio.");
        }

        return new(true, null);
    }
}
