using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GasMapocho.Api.Controllers;

/// <summary>
///     GET    /api/clientes        listado con busqueda y paginacion
///     GET    /api/clientes/{id}   uno
///     POST   /api/clientes        alta   -> 201
///     PUT    /api/clientes/{id}   cambio -> 204
///     DELETE /api/clientes/{id}   baja logica -> 204
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly IClienteRepositorio _clientes;

    public ClientesController(IClienteRepositorio clientes) => _clientes = clientes;

    [HttpGet]
    public ActionResult<Pagina<Cliente>> Listar(
        [FromQuery] string? busqueda = null,
        [FromQuery] int pagina = 1,
        [FromQuery] int cantidadPorPagina = 10)
    {
        if (pagina < 1) pagina = 1;
        if (cantidadPorPagina is < 1 or > 200) cantidadPorPagina = 10;

        return Ok(_clientes.Listar(busqueda, pagina, cantidadPorPagina));
    }

    [HttpGet("{id:int}")]
    public ActionResult<Cliente> Obtener(int id)
    {
        var cliente = _clientes.Obtener(id);
        return cliente is null
            ? NotFound(new { mensaje = "El cliente no existe." })
            : Ok(cliente);
    }

    [HttpPost]
    public ActionResult<Cliente> Registrar([FromBody] Cliente cliente)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var id = _clientes.Registrar(cliente);
            cliente.IdCliente = id;
            return CreatedAtAction(nameof(Obtener), new { id }, cliente);
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public IActionResult Actualizar(int id, [FromBody] Cliente cliente)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (id != cliente.IdCliente)
            return BadRequest(new { mensaje = "El id de la ruta no coincide con el del cuerpo." });

        if (_clientes.Obtener(id) is null)
            return NotFound(new { mensaje = "El cliente no existe." });

        try
        {
            _clientes.Actualizar(cliente);
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
        if (_clientes.Obtener(id) is null)
            return NotFound(new { mensaje = "El cliente no existe." });

        try
        {
            _clientes.Eliminar(id);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            return Conflict(new { mensaje = ex.Message });
        }
    }
}
