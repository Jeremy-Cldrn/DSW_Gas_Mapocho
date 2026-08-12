using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Tienda.Controllers;

/// <summary>
/// El catalogo es publico: se puede mirar sin sesion, pero agregar al carrito
/// y comprar exigen estar identificado.
/// </summary>
public class CatalogoController : Controller
{
    private readonly ApiGasMapocho _api;

    public CatalogoController(ApiGasMapocho api) => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Catálogo";

        // La API ya devuelve solo productos activos.
        var productos = await _api.ProductosAsync();

        // Distingue "el catálogo está vacío" de "no pudimos consultarlo".
        ViewData["ApiCaida"] = _api.HuboError;

        return View(productos);
    }

    public IActionResult Error()
    {
        ViewData["Title"] = "Error";
        return View("~/Views/Shared/Error.cshtml");
    }
}
