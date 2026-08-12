using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GasMapocho.Api.Controllers;

/// <summary>
/// Servicio REST de productos. Devuelve JSON, nunca HTML.
///
///     GET    /api/productos           listado con busqueda y paginacion
///     GET    /api/productos/{id}      uno
///     POST   /api/productos           alta      -> 201 Created
///     PUT    /api/productos/{id}      cambio    -> 204 No Content
///     DELETE /api/productos/{id}      baja      -> 204 No Content
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductosController : ControllerBase
{
    private readonly IProductoRepositorio _productos;

    public ProductosController(IProductoRepositorio productos) => _productos = productos;

    [HttpGet]
    public ActionResult<PaginaDeProductos> Listar(
        [FromQuery] string? busqueda = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int cantidadPorPagina = 10)
    {
        if (pagina < 1) pagina = 1;

        // Techo al tamano de pagina: sin el, un cliente podria pedir un millon
        // de filas en una sola llamada.
        if (cantidadPorPagina is < 1 or > 100) cantidadPorPagina = 10;

        return Ok(_productos.Listar(busqueda, pagina, cantidadPorPagina));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Producto> Obtener(int id)
    {
        var producto = _productos.Obtener(id);
        return producto is null
            ? NotFound(new { mensaje = "El producto no existe." })
            : Ok(producto);
    }

    [HttpPost]
    public ActionResult<Producto> Registrar([FromBody] Producto producto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var id = _productos.Registrar(producto);
            producto.IdProducto = id;

            // 201 con la cabecera Location apuntando al recurso creado.
            return CreatedAtAction(nameof(Obtener), new { id }, producto);
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult Actualizar(int id, [FromBody] Producto producto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (id != producto.IdProducto)
            return BadRequest(new { mensaje = "El id de la ruta no coincide con el del cuerpo." });

        if (_productos.Obtener(id) is null)
            return NotFound(new { mensaje = "El producto no existe." });

        try
        {
            _productos.Actualizar(producto);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public IActionResult Eliminar(int id)
    {
        if (_productos.Obtener(id) is null)
            return NotFound(new { mensaje = "El producto no existe." });

        try
        {
            _productos.Eliminar(id);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            // Por ejemplo: el producto tiene pedidos asociados.
            return Conflict(new { mensaje = ex.Message });
        }
    }
}
