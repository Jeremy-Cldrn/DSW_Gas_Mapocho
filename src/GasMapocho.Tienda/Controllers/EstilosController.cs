using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Tienda.Controllers;

/// <summary>
/// Página de muestra del design system. Permite revisar todos los componentes
/// en una sola pantalla, antes de que estén repartidos en diez vistas.
/// No forma parte del sistema entregado: es herramienta de desarrollo.
/// </summary>
public class EstilosController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Design system";
        return View();
    }
}
