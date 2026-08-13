using Microsoft.AspNetCore.Http;
using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public interface IValidadorArchivoExcel
{
    ResultadoValidacionArchivo Validar(IFormFile? archivo);
}
