using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Api.Controllers;

/// <summary>
///     GET /api/reportes/inventario   stock, valorizacion y unidades vendidas
///     GET /api/reportes/usuarios     listado de cuentas y sus roles
///
/// El reporte de ventas no vive aqui: es /api/pedidos?estado=Aprobado, que ya
/// filtra y pagina en SQL. Duplicarlo solo agregaria una consulta que mantener.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportesController : ControllerBase
{
    private readonly IReporteRepositorio _reportes;
    private readonly IUsuarioRepositorio _usuarios;

    public ReportesController(IReporteRepositorio reportes, IUsuarioRepositorio usuarios)
    {
        _reportes = reportes;
        _usuarios = usuarios;
    }

    [HttpGet("inventario")]
    public ActionResult<Pagina<FilaInventario>> Inventario(
        [FromQuery] int pagina = 1,
        [FromQuery] int cantidadPorPagina = 50)
    {
        if (pagina < 1) pagina = 1;
        if (cantidadPorPagina is < 1 or > 200) cantidadPorPagina = 50;

        return Ok(_reportes.Inventario(pagina, cantidadPorPagina));
    }

    [HttpGet("usuarios")]
    public ActionResult<Pagina<Usuario>> Usuarios(
        [FromQuery] string? busqueda = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int cantidadPorPagina = 50)
    {
        if (pagina < 1) pagina = 1;
        if (cantidadPorPagina is < 1 or > 200) cantidadPorPagina = 50;

        return Ok(_usuarios.Listar(busqueda, pagina, cantidadPorPagina));
    }
}
