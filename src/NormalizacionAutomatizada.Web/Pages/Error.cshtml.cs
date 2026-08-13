using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NormalizacionAutomatizada.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ModeloError : PageModel
{
    public string? IdSolicitud { get; set; }

    public bool MostrarIdSolicitud => !string.IsNullOrEmpty(IdSolicitud);

    public void OnGet()
    {
        IdSolicitud = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
