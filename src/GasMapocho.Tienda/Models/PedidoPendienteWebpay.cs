using System.Text.Json;

namespace GasMapocho.Tienda.Models;

/// <summary>
/// Datos de entrega ya validados en Checkout, a la espera de que el cliente
/// termine el pago simulado de Webpay (§13.1). El pedido recién se crea
/// cuando el pago "se aprueba" en PagoWebpay — antes de eso no existe en la
/// base de datos, igual que con el resto de los métodos de pago no se crea
/// hasta que el POST de Checkout es válido.
/// </summary>
public class PedidoPendienteWebpay
{
    public string Nombre { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Comuna { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string? Referencia { get; set; }
}

public static class PedidoPendiente
{
    private const string Clave = "PedidoPendienteWebpay";

    public static void Guardar(ISession sesion, PedidoPendienteWebpay datos) =>
        sesion.SetString(Clave, JsonSerializer.Serialize(datos));

    public static PedidoPendienteWebpay? Leer(ISession sesion)
    {
        var json = sesion.GetString(Clave);
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PedidoPendienteWebpay>(json);
    }

    public static void Limpiar(ISession sesion) => sesion.Remove(Clave);
}
