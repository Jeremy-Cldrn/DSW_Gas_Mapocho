using System.ComponentModel.DataAnnotations;
using GasMapocho.Tienda.Models;
using GasMapocho.Ui.Models;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Tienda.Controllers;

public class CheckoutVm
{
    [Required(ErrorMessage = "Ingresa tu nombre.")]
    [Display(Name = "Nombre completo")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu teléfono.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu comuna.")]
    [Display(Name = "Comuna")]
    public string Comuna { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu dirección.")]
    [Display(Name = "Dirección")]
    public string Direccion { get; set; } = "";

    [Display(Name = "Referencia")]
    public string? Referencia { get; set; }

    [Required(ErrorMessage = "Elige un método de pago.")]
    [Display(Name = "Método de pago")]
    public string MetodoPago { get; set; } = "";

    public List<LineaVm> Lineas { get; set; } = new();
    public decimal Subtotal => Lineas.Sum(l => l.Subtotal);
    public decimal Despacho => Lineas.Count == 0 ? 0 : ApiGasMapocho.CostoDespacho;
    public decimal Total => Subtotal + Despacho;
}

/// <summary>
/// Formulario de pago simulado de Webpay (§13.1). Solo valida formato: no hay
/// ninguna pasarela real detrás, así que no importa si el número "pasa" un
/// Luhn de verdad, importa que el cliente no pueda enviar campos vacíos.
/// </summary>
public class PagoWebpayVm
{
    [Required(ErrorMessage = "Ingresa el número de tarjeta.")]
    [RegularExpression(@"^[\d ]{13,19}$", ErrorMessage = "El número de tarjeta no es válido.")]
    [Display(Name = "Número de tarjeta")]
    public string NumeroTarjeta { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el nombre del titular.")]
    [Display(Name = "Nombre del titular")]
    public string Titular { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la fecha de vencimiento.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Usa el formato MM/AA.")]
    [Display(Name = "Vencimiento (MM/AA)")]
    public string Vencimiento { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el CVV.")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "El CVV debe tener 3 o 4 dígitos.")]
    [Display(Name = "CVV")]
    public string Cvv { get; set; } = "";
}

[RequiereSesion]
public class PedidoController : Controller
{
    private readonly ApiGasMapocho _api;

    public PedidoController(ApiGasMapocho api) => _api = api;

    public async Task<IActionResult> Checkout()
    {
        var lineas = Carrito.Leer(HttpContext.Session);
        if (lineas.Count == 0) return RedirectToAction("Index", "Carrito");

        ViewData["Title"] = "Confirmar pedido";

        var vm = new CheckoutVm { Lineas = lineas };

        // Precarga los datos del perfil: pedir gas la segunda vez toma menos
        // tiempo que la primera.
        if (HttpContext.Session.ClienteActual() is int idCliente)
        {
            var cliente = await _api.ClienteAsync(idCliente);
            if (cliente is not null)
            {
                vm.Nombre = cliente.Nombre;
                vm.Email = cliente.Email;
                vm.Telefono = cliente.Telefono;
                vm.Comuna = cliente.Comuna;
                vm.Direccion = cliente.Direccion;
            }
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutVm vm)
    {
        vm.Lineas = Carrito.Leer(HttpContext.Session);
        if (vm.Lineas.Count == 0) return RedirectToAction("Index", "Carrito");

        ViewData["Title"] = "Confirmar pedido";

        // Si algo no valida, se vuelve al formulario conservando lo escrito.
        if (!ModelState.IsValid) return View(vm);

        var idCliente = HttpContext.Session.ClienteActual();
        if (idCliente is null)
        {
            ModelState.AddModelError(string.Empty,
                "Tu cuenta no tiene una ficha de cliente asociada. Contacta al distribuidor.");
            return View(vm);
        }

        // Webpay es una simulación educativa (§13.1): el pedido no se crea
        // todavía, primero pasa por una pantalla de pago propia. Los demás
        // métodos siguen creando el pedido de inmediato, como siempre.
        if (vm.MetodoPago == "Webpay")
        {
            PedidoPendiente.Guardar(HttpContext.Session, new PedidoPendienteWebpay
            {
                Nombre = vm.Nombre,
                Telefono = vm.Telefono,
                Comuna = vm.Comuna,
                Direccion = vm.Direccion,
                Referencia = vm.Referencia
            });
            return RedirectToAction(nameof(PagoWebpay));
        }

        var resultado = await RegistrarPedidoAsync(idCliente.Value, vm.MetodoPago, vm.Nombre,
            vm.Telefono, vm.Comuna, vm.Direccion, vm.Referencia, vm.Lineas);

        if (!resultado.Exito)
        {
            // Stock insuficiente llega por aqui. La transaccion ya hizo
            // Rollback: no quedo un pedido a medias.
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return View(vm);
        }

        // POST-Redirect-Get: si el cliente recarga la confirmación, el pedido
        // NO se vuelve a registrar.
        return RedirectToAction(nameof(Confirmacion), new { id = resultado.Id });
    }

    public IActionResult PagoWebpay()
    {
        // Sin datos de entrega pendientes no hay nada que pagar: puede pasar
        // si alguien entra directo a esta URL o si ya se completó el pago.
        if (PedidoPendiente.Leer(HttpContext.Session) is null)
            return RedirectToAction(nameof(Checkout));

        var lineas = Carrito.Leer(HttpContext.Session);
        if (lineas.Count == 0) return RedirectToAction("Index", "Carrito");

        ViewData["Title"] = "Pago con Webpay";
        return View(new PagoWebpayVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoWebpay(PagoWebpayVm vm)
    {
        ViewData["Title"] = "Pago con Webpay";

        var pendiente = PedidoPendiente.Leer(HttpContext.Session);
        if (pendiente is null) return RedirectToAction(nameof(Checkout));

        if (!ModelState.IsValid) return View(vm);

        var idCliente = HttpContext.Session.ClienteActual();
        if (idCliente is null)
        {
            ModelState.AddModelError(string.Empty,
                "Tu cuenta no tiene una ficha de cliente asociada. Contacta al distribuidor.");
            return View(vm);
        }

        // "Aprobado" simulado: nunca se contacta a Webpay de verdad (§13.1).
        // A partir de acá es el mismo registro que cualquier otro método.
        var resultado = await RegistrarPedidoAsync(idCliente.Value, "Webpay", pendiente.Nombre,
            pendiente.Telefono, pendiente.Comuna, pendiente.Direccion, pendiente.Referencia,
            Carrito.Leer(HttpContext.Session));

        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return View(vm);
        }

        return RedirectToAction(nameof(Confirmacion), new { id = resultado.Id });
    }

    /// <summary>
    /// Único punto donde se llama a la API para crear el pedido: lo usan
    /// tanto el Checkout normal como PagoWebpay, así el registro (stock,
    /// transacción, limpieza de sesión) queda igual sin importar el método.
    /// </summary>
    private async Task<Resultado> RegistrarPedidoAsync(int idCliente, string metodoPago,
        string nombre, string telefono, string comuna, string direccion, string? referencia,
        List<LineaVm> lineas)
    {
        // El backend graba cabecera y detalle en UNA transaccion y resuelve
        // los precios contra la base: los del carrito son solo referenciales.
        var resultado = await _api.CrearPedidoAsync(idCliente, metodoPago, nombre, telefono,
            comuna, direccion, referencia, ApiGasMapocho.CostoDespacho, lineas);

        if (resultado.Exito)
        {
            Carrito.Vaciar(HttpContext.Session);
            PedidoPendiente.Limpiar(HttpContext.Session);
        }

        return resultado;
    }

    public async Task<IActionResult> Confirmacion(int id)
    {
        ViewData["Title"] = "Pedido registrado";
        ViewData["IdPedido"] = id;

        var pedido = await _api.PedidoAsync(id);

        // Un cliente no puede ver la confirmacion de un pedido ajeno cambiando
        // el id de la URL.
        if (pedido is not null && pedido.IdCliente != HttpContext.Session.ClienteActual())
            return NotFound();

        return View(pedido);
    }
}
