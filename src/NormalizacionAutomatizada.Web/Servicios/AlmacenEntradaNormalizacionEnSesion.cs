using System.Text.Json;
using Microsoft.AspNetCore.Http;
using NormalizacionAutomatizada.Web.Modelos;

namespace NormalizacionAutomatizada.Web.Servicios;

public sealed class AlmacenEntradaNormalizacionEnSesion : IAlmacenEntradaNormalizacion
{
    private const string ClaveSesion = "entrada-normalizacion";
    private readonly IHttpContextAccessor accesoContextoHttp;

    public AlmacenEntradaNormalizacionEnSesion(IHttpContextAccessor accesoContextoHttp)
    {
        this.accesoContextoHttp = accesoContextoHttp;
    }

    public void Guardar(EntradaNormalizacion entrada)
    {
        ObtenerSesion().SetString(ClaveSesion, JsonSerializer.Serialize(entrada));
    }

    public EntradaNormalizacion? Obtener()
    {
        var entradaSerializada = ObtenerSesion().GetString(ClaveSesion);
        return string.IsNullOrWhiteSpace(entradaSerializada)
            ? null
            : JsonSerializer.Deserialize<EntradaNormalizacion>(entradaSerializada);
    }

    private ISession ObtenerSesion()
    {
        return accesoContextoHttp.HttpContext?.Session
            ?? throw new InvalidOperationException("No hay una sesion HTTP disponible para almacenar la entrada de normalizacion.");
    }
}
