using GasMapocho.Ui.Models;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Tienda.Controllers;

/// <summary>
/// Vista informativa del cliente logueado (§15.1). A propósito no permite
/// editar nada desde acá: el encargo la pide "únicamente informativa".
/// </summary>
[RequiereSesion]
public class PerfilController : Controller
{
    private readonly ApiGasMapocho _api;

    public PerfilController(ApiGasMapocho api) => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Mi perfil";

        var idCliente = HttpContext.Session.ClienteActual();
        var cliente = idCliente is null ? null : await _api.ClienteAsync(idCliente.Value);

        ViewData["ApiCaida"] = _api.HuboError;
        return View(cliente ?? new ClienteVm());
    }
}
