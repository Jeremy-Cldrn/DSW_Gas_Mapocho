using GasMapocho.Tienda.Models;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Tienda.Controllers;

[RequiereSesion]
public class CarritoController : Controller
{
    private readonly ApiGasMapocho _api;

    public CarritoController(ApiGasMapocho api) => _api = api;

    public IActionResult Index()
    {
        ViewData["Title"] = "Carrito";
        return View(Carrito.Leer(HttpContext.Session));
    }

    /// <summary>Agrega por Ajax. Devuelve los totales ya formateados en CLP.</summary>
    [HttpPost]
    public async Task<IActionResult> Agregar(int idProducto, int cantidad = 1)
    {
        // El stock se consulta a la API en cada intento: el que trae la vista
        // pudo quedar viejo si alguien compro mientras tanto.
        var producto = await _api.ProductoAsync(idProducto);
        if (producto is null)
            return Json(new { ok = false, mensaje = "El producto no existe." });

        // Respuesta de negocio, no error HTTP: el frontend la muestra como aviso.
        if (!Carrito.Agregar(HttpContext.Session, producto, cantidad, out var mensaje))
            return Json(new { ok = false, mensaje });

        return Json(Estado(mensaje));
    }

    [HttpPost]
    public async Task<IActionResult> Actualizar(int idProducto, int cantidad)
    {
        var lineas = Carrito.Leer(HttpContext.Session);
        var linea = lineas.FirstOrDefault(l => l.IdProducto == idProducto);
        if (linea is null) return Json(new { ok = false, mensaje = "La línea no existe." });

        if (cantidad <= 0)
        {
            lineas.Remove(linea);
        }
        else
        {
            var producto = await _api.ProductoAsync(idProducto);
            if (producto is null)
                return Json(new { ok = false, mensaje = "El producto ya no está disponible." });

            if (cantidad > producto.Stock)
                return Json(new { ok = false, mensaje = $"Solo quedan {producto.Stock} unidades disponibles." });

            linea.Cantidad = cantidad;
        }

        Carrito.Guardar(HttpContext.Session, lineas);
        return Json(Estado("Carrito actualizado."));
    }

    [HttpPost]
    public IActionResult Quitar(int idProducto)
    {
        var lineas = Carrito.Leer(HttpContext.Session);
        lineas.RemoveAll(l => l.IdProducto == idProducto);
        Carrito.Guardar(HttpContext.Session, lineas);
        return Json(Estado("Producto quitado del carrito."));
    }

    private object Estado(string mensaje)
    {
        var lineas = Carrito.Leer(HttpContext.Session);
        var subtotal = Carrito.Subtotal(lineas);
        var despacho = Carrito.Despacho(lineas);
        var total = Carrito.Total(lineas);

        // Se devuelve el número (para calcular) y el texto ya formateado (para mostrar),
        // así el formato CLP no se duplica entre servidor y navegador.
        return new
        {
            ok = true,
            mensaje,
            items = lineas.Sum(l => l.Cantidad),
            vacio = lineas.Count == 0,
            subtotal,
            despacho,
            total,
            subtotalTexto = subtotal.ToString("C0"),
            despachoTexto = despacho.ToString("C0"),
            totalTexto = total.ToString("C0"),
            lineas = lineas.Select(l => new
            {
                l.IdProducto,
                l.Cantidad,
                subtotalTexto = l.Subtotal.ToString("C0")
            })
        };
    }
}
