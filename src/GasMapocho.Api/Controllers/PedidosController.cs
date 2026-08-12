using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GasMapocho.Api.Controllers;

/// <summary>
///     GET  /api/pedidos                  listado con filtros y paginacion
///     GET  /api/pedidos/{id}             cabecera + detalle
///     POST /api/pedidos                  registra, descuenta stock y confirma la venta,
///                                         todo en una sola transaccion -> 201
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoRepositorio _pedidos;

    public PedidosController(IPedidoRepositorio pedidos) => _pedidos = pedidos;

    [HttpGet]
    public ActionResult<Pagina<Pedido>> Listar(
        [FromQuery] int? idCliente = null,
        [FromQuery] string? estado = null,
        [FromQuery] DateTime? fechaInicial = null,
        [FromQuery] DateTime? fechaFinal = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int cantidadPorPagina = 10)
    {
        if (pagina < 1) pagina = 1;
        if (cantidadPorPagina is < 1 or > 200) cantidadPorPagina = 10;

        return Ok(_pedidos.Listar(idCliente, estado, fechaInicial, fechaFinal, busqueda, pagina, cantidadPorPagina));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Pedido> Obtener(int id)
    {
        var pedido = _pedidos.Obtener(id);
        return pedido is null
            ? NotFound(new { mensaje = "El pedido no existe." })
            : Ok(pedido);
    }

    [HttpPost]
    public ActionResult<Pedido> Registrar([FromBody] PedidoNuevo pedido)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var id = _pedidos.Registrar(pedido);
            return CreatedAtAction(nameof(Obtener), new { id }, _pedidos.Obtener(id));
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            // Stock insuficiente o producto inactivo. La transaccion ya hizo
            // Rollback: no quedo un pedido a medias.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}
