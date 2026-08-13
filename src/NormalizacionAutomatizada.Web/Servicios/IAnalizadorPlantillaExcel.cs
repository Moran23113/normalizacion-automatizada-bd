using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public interface IAnalizadorPlantillaExcel
{
    ResultadoAnalisisLibro Analizar(Stream contenido);
}
