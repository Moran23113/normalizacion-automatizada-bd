var constructor = WebApplication.CreateBuilder(args);

constructor.Services.AddRazorPages();
constructor.Services.AddDistributedMemoryCache();
constructor.Services.AddSession(opciones =>
{
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
});
constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<NormalizacionAutomatizada.Web.Servicios.IValidadorArchivoExcel, NormalizacionAutomatizada.Web.Servicios.ValidadorArchivoExcel>();
constructor.Services.AddScoped<NormalizacionAutomatizada.Web.Servicios.IAnalizadorPlantillaExcel, NormalizacionAutomatizada.Web.Servicios.AnalizadorPlantillaExcel>();
// El siguiente modulo recupera los registros con IAlmacenEntradaNormalizacion.Obtener().
constructor.Services.AddScoped<NormalizacionAutomatizada.Web.Servicios.IAlmacenEntradaNormalizacion, NormalizacionAutomatizada.Web.Servicios.AlmacenEntradaNormalizacionEnSesion>();

var aplicacion = constructor.Build();

if (!aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseExceptionHandler("/Error");
    aplicacion.UseHsts();
}

aplicacion.UseHttpsRedirection();

aplicacion.UseRouting();

aplicacion.UseSession();

aplicacion.UseAuthorization();

aplicacion.MapStaticAssets();
aplicacion.MapRazorPages()
   .WithStaticAssets();

aplicacion.Run();

public partial class Program;
