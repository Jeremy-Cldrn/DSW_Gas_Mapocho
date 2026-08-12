using GasMapocho.Admin.Models;
using GasMapocho.Ui.Models;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Admin.Controllers;

/// <summary>
/// Fusiona lo que antes eran dos pantallas separadas: Pedidos y
/// Reportes/Ventas (indicadores + reporte). El panel solo debe tener
/// Productos, Clientes y Ventas (§6 del encargo). El pedido queda
/// confirmado (stock descontado, venta registrada) al momento de la
/// compra — ya no hay un paso de aprobación manual del administrador, así
/// que el detalle (en un modal) es solo informativo.
/// </summary>
[RequiereSesion]
public class VentasController : Controller
{
    private readonly ApiGasMapocho _api;

    public VentasController(ApiGasMapocho api) => _api = api;

    public async Task<IActionResult> Index(string? busqueda)
    {
        ViewData["Title"] = "Ventas";

        // Tabla: todos los pedidos que calcen con la búsqueda, sin filtrar
        // por estado ni por fecha (esos filtros del reporte viejo se
        // eliminaron de la UI, §9). El detalle de cada uno viaja completo
        // porque el modal necesita las líneas.
        var pedidos = await _api.PedidosAsync(busqueda: busqueda);
        var completos = new List<PedidoVm>();
        foreach (var p in pedidos)
            completos.Add(await _api.PedidoAsync(p.Id) ?? p);

        // Indicadores: ventas (Aprobadas) del mes en curso, sin depender de
        // lo que haya tecleado el usuario en el buscador — cambian solo con
        // el calendario, no con el filtro de la tabla.
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var ventasDelMes = await _api.PedidosAsync(estado: "Aprobado", desde: inicioMes, hasta: hoy);

        ViewData["VentasDelMes"] = ventasDelMes.Count;
        ViewData["MontoTotal"] = ventasDelMes.Sum(v => v.Total);
        ViewData["ApiCaida"] = _api.HuboError;

        var tabla = new TablaVm
        {
            Titulo = "Ventas",
            Subtitulo = "Revisa los pedidos entrantes y valida los pagos",
            PlaceholderBuscar = "Buscar por cliente o pedido",
            Busqueda = busqueda,
            BuscarEnServidor = true,
            DetalleModal = true,
            PorPagina = 6,
            UrlBase = "/Ventas",
            DetalleVista = "_DetallePedido",
            Columnas = new()
            {
                new("Pedido", ancho: "120px"),
                new("Cliente"),
                new("Comuna", ancho: "130px"),
                new("Dirección"),
                new("Fecha", ancho: "120px"),
                new("Total", numerica: true, ancho: "130px"),
            },
            Filas = completos.Select(p => new FilaVm
            {
                Id = p.Id,
                DetalleModel = p,
                Celdas = new()
                {
                    new(p.Codigo, fuerte: true),
                    new(p.Cliente),
                    new(p.Comuna),
                    new(p.Direccion),
                    new(p.Fecha.ToString("dd/MM/yyyy")),
                    new(p.Total.ToString("C0"), numerica: true, fuerte: true),
                }
            }).ToList(),
            Vacio = new EstadoVacioVm("receipt_long", "Todavía no hay ventas",
                                      "Los pedidos aparecerán aquí en cuanto entren.")
        };

        return View(tabla);
    }

    public IActionResult Error()
    {
        ViewData["Title"] = "Error";
        return View("~/Views/Shared/Error.cshtml");
    }
}
