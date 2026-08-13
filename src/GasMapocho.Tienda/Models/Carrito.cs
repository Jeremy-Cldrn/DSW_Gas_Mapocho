using System.Text.Json;
using GasMapocho.Ui.Models;
using GasMapocho.Ui.Services;

namespace GasMapocho.Tienda.Models;

/// <summary>
/// El carrito vive en la sesión del frontend, no en la base de datos: es estado
/// de una compra en curso. Al confirmar, viaja completo al backend en UNA sola
/// llamada — que es lo que permite grabar cabecera y detalle en una transacción.
/// </summary>
public static class Carrito
{
    private const string Clave = "Carrito";

    public static List<LineaVm> Leer(ISession sesion)
    {
        var json = sesion.GetString(Clave);
        return string.IsNullOrEmpty(json)
            ? new List<LineaVm>()
            : JsonSerializer.Deserialize<List<LineaVm>>(json) ?? new List<LineaVm>();
    }

    public static void Guardar(ISession sesion, List<LineaVm> lineas)
    {
        sesion.SetString(Clave, JsonSerializer.Serialize(lineas));
        sesion.SetInt32("CarritoItems", lineas.Sum(l => l.Cantidad));
    }

    public static void Vaciar(ISession sesion)
    {
        sesion.Remove(Clave);
        sesion.SetInt32("CarritoItems", 0);
    }

    /// <summary>Agrega respetando el stock disponible. Devuelve false si no alcanza.</summary>
    public static bool Agregar(ISession sesion, ProductoVm producto, int cantidad, out string mensaje)
    {
        var lineas = Leer(sesion);
        var existente = lineas.FirstOrDefault(l => l.IdProducto == producto.Id);
        var yaEnCarrito = existente?.Cantidad ?? 0;

        if (yaEnCarrito + cantidad > producto.Stock)
        {
            var quedan = producto.Stock - yaEnCarrito;
            mensaje = quedan <= 0
                ? "Ya tienes en el carrito todo el stock disponible de este producto."
                : $"Solo quedan {quedan} unidades disponibles.";
            return false;
        }

        if (existente is not null)
        {
            existente.Cantidad += cantidad;
        }
        else
        {
            lineas.Add(new LineaVm
            {
                IdProducto = producto.Id,
                Nombre = producto.Nombre,
                ImagenUrl = producto.ImagenUrl,
                CapacidadKg = producto.CapacidadKg,
                Cantidad = cantidad,
                PrecioUnitario = producto.Precio
            });
        }

        Guardar(sesion, lineas);
        mensaje = "Producto agregado al carrito.";
        return true;
    }

    public static decimal Subtotal(List<LineaVm> lineas) => lineas.Sum(l => l.Subtotal);

    /// <summary>Sin productos no se cobra despacho.</summary>
    public static decimal Despacho(List<LineaVm> lineas) =>
        lineas.Count == 0 ? 0 : ApiGasMapocho.CostoDespacho;

    public static decimal Total(List<LineaVm> lineas) =>
        Subtotal(lineas) + Despacho(lineas);
}
