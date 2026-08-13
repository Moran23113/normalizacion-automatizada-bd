using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public interface IAlmacenEntradaNormalizacion
{
    void Guardar(EntradaNormalizacion entrada);

    EntradaNormalizacion? Obtener();
}
