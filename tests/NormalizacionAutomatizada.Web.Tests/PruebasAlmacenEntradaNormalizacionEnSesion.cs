using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NormalizacionAutomatizada.Web.Modelos;
using NormalizacionAutomatizada.Web.Servicios;

namespace NormalizacionAutomatizada.Web.Pruebas;

public class PruebasAlmacenEntradaNormalizacionEnSesion
{
    [Fact]
    public void Guardar_YLuegoObtener_DevuelveLaEntradaOriginal()
    {
        var sesion = new SesionEnMemoria();
        var almacen = CrearAlmacen(sesion);
        var entrada = new EntradaNormalizacion(
        [
            new TablaNormalizacion(
                "01",
                [new ColumnaNormalizacion("id_estudiante", "INT", true, false)],
                [new RegistroOriginal(new Dictionary<string, string?> { ["id_estudiante"] = "1" })])
        ]);

        almacen.Guardar(entrada);
        var recuperada = almacen.Obtener();

        var tabla = Assert.Single(recuperada!.Tablas);
        Assert.Equal("01", tabla.Numero);
        Assert.Equal(new ColumnaNormalizacion("id_estudiante", "INT", true, false), Assert.Single(tabla.Columnas));
        Assert.Equal("1", Assert.Single(tabla.Registros).Valores["id_estudiante"]);
    }

    [Fact]
    public void Obtener_CuandoLaSesionNoTieneEntrada_DevuelveNulo()
    {
        var almacen = CrearAlmacen(new SesionEnMemoria());

        var recuperada = almacen.Obtener();

        Assert.Null(recuperada);
    }

    [Fact]
    public void Guardar_CuandoNoHayContextoHttp_LanzaUnErrorInformativo()
    {
        var almacen = new AlmacenEntradaNormalizacionEnSesion(new HttpContextAccessor());
        var entrada = new EntradaNormalizacion([]);

        var error = Assert.Throws<InvalidOperationException>(() => almacen.Guardar(entrada));

        Assert.Equal("No hay una sesion HTTP disponible para almacenar la entrada de normalizacion.", error.Message);
    }

    private static AlmacenEntradaNormalizacionEnSesion CrearAlmacen(ISession sesion)
    {
        var contexto = new DefaultHttpContext();
        contexto.Features.Set<ISessionFeature>(new CaracteristicaSesionEnMemoria { Session = sesion });
        var accesoContexto = new HttpContextAccessor { HttpContext = contexto };
        return new AlmacenEntradaNormalizacionEnSesion(accesoContexto);
    }

    private sealed class CaracteristicaSesionEnMemoria : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    private sealed class SesionEnMemoria : ISession
    {
        private readonly Dictionary<string, byte[]> valores = new(StringComparer.Ordinal);

        public bool IsAvailable => true;

        public string Id => "test-session";

        public IEnumerable<string> Keys => valores.Keys;

        public void Clear()
        {
            valores.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            valores.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            valores[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return valores.TryGetValue(key, out value!);
        }
    }
}
