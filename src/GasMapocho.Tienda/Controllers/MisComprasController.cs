using GasMapocho.Ui.Models;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Tienda.Controllers;

[RequiereSesion]
public class MisComprasController : Controller
{
    private readonly ApiGasMapocho _api;

    public MisComprasController(ApiGasMapocho api) => _api = api;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Mis compras";

        var idCliente = HttpContext.Session.ClienteActual();
        if (idCliente is null) return View(new List<PedidoVm>());

        // El filtro por cliente lo aplica la API en SQL. Pedir todos los
        // pedidos y filtrar aqui dejaria ver los ajenos por el camino.
        var pedidos = await _api.PedidosAsync(idCliente: idCliente);

        // El detalle se pide por pedido: la vista lo despliega al abrir cada
        // fila y son pocas compras por cliente.
        var completos = new List<PedidoVm>();
        foreach (var p in pedidos.OrderByDescending(x => x.Fecha))
            completos.Add(await _api.PedidoAsync(p.Id) ?? p);

        ViewData["ApiCaida"] = _api.HuboError;

        return View(completos);
    }
}
