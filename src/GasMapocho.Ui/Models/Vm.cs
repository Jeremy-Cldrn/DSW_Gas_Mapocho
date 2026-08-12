using System.ComponentModel.DataAnnotations;

namespace GasMapocho.Ui.Models;

// ============================================================
// Modelos de vista compartidos por la Tienda y el Admin.
// Cuando exista el backend, estos se reemplazan por los DTO de
// GasMapocho.Contracts. Las vistas no cambian.
// ============================================================

public class ProductoVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingresa el código.")]
    [StringLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el nombre.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    public string Nombre { get; set; } = "";

    // Desde 0: el catálogo incluye accesorios (regulador, manguera, kit) que
    // no tienen capacidad en kilos. Con mínimo 1 no se podían editar.
    [Range(0, 100, ErrorMessage = "La capacidad debe estar entre 0 y 100 kg.")]
    [Display(Name = "Capacidad (kg)")]
    public int CapacidadKg { get; set; }

    /// <summary>Pesos chilenos, sin decimales. En BD: DECIMAL(12,0).</summary>
    [Range(1, 99999999, ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }

    [Range(0, 999999, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    public string EstadoStock => Stock == 0 ? "cero" : Stock <= 10 ? "bajo" : "ok";

    public string TextoStock => Stock switch
    {
        0                => "Sin stock",
        var s when s <= 10 => $"Últimas {s} unidades",
        _                => "Disponible"
    };
}

public class LineaVm
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public int CapacidadKg { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
}

public class PedidoVm
{
    public int Id { get; set; }
    public string Codigo => $"PED-{Id:D5}";
    public int IdCliente { get; set; }
    public string Cliente { get; set; } = "";
    public DateTime Fecha { get; set; }
    /// <summary>Pendiente · Aprobado · Rechazado</summary>
    public string Estado { get; set; } = "Pendiente";
    public decimal Despacho { get; set; }
    public string MetodoPago { get; set; } = "";
    public string Comuna { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Telefono { get; set; } = "";
    public List<LineaVm> Lineas { get; set; } = new();

    // ------------------------------------------------------------
    // Los totales se pueden fijar o dejar que se calculen.
    //
    // El listado que devuelve la API NO trae las lineas del pedido (pedirlas
    // por cada fila seria una consulta por pedido), pero si trae los totales
    // ya sumados en SQL. En la vista de detalle, en cambio, las lineas estan
    // y el calculo cuadra solo. Por eso el valor recibido manda, y el calculo
    // queda como respaldo.
    // ------------------------------------------------------------
    private decimal? _subtotal;
    public decimal Subtotal
    {
        get => _subtotal ?? Lineas.Sum(l => l.Subtotal);
        set => _subtotal = value;
    }

    private decimal? _total;
    public decimal Total
    {
        get => _total ?? Subtotal + Despacho;
        set => _total = value;
    }

    private int? _totalItems;
    public int TotalItems
    {
        get => _totalItems ?? Lineas.Sum(l => l.Cantidad);
        set => _totalItems = value;
    }

    // ------------------------------------------------------------
    // El cliente y el administrador miran el MISMO estado, pero no
    // les significa lo mismo. "Aprobado" es vocabulario de la caja,
    // no de quien está esperando su balón de gas.
    // El panel usa Estado tal cual; la tienda usa estos textos.
    // ------------------------------------------------------------

    public string TituloCliente => Estado switch
    {
        "Aprobado"  => "Pedido confirmado",
        "Rechazado" => "No pudimos cobrar el pago",
        _           => "Pedido pendiente"
    };

    public string DetalleCliente => Estado switch
    {
        "Aprobado"  => "Tu pedido está en preparación para el despacho.",
        "Rechazado" => "El pedido no se procesó y no se te cobró nada.",
        _           => "Te avisaremos por correo apenas quede confirmado."
    };

    public string IconoCliente => Estado switch
    {
        "Aprobado"  => "local_shipping",
        "Rechazado" => "cancel",
        _           => "schedule"
    };

    /// <summary>Sufijo de la clase CSS: seguimiento-{aprobado|rechazado|pendiente}</summary>
    public string ClaseCliente => Estado.ToLowerInvariant();
}

public class ClienteVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre.")]
    [StringLength(120, ErrorMessage = "El nombre no puede superar los 120 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el RUT.")]
    [StringLength(20, ErrorMessage = "El RUT no puede superar los 20 caracteres.")]
    [Display(Name = "RUT")]
    public string Documento { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el teléfono.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la comuna.")]
    public string Comuna { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la dirección.")]
    [Display(Name = "Dirección")]
    public string Direccion { get; set; } = "";
}

/// <summary>Identidad del usuario autenticado, tal como la devuelve la API.</summary>
public class IdentidadVm
{
    public int IdUsuario { get; set; }
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
    /// <summary>Null para un administrador: no tiene ficha de cliente.</summary>
    public int? IdCliente { get; set; }
}

/// <summary>Fila del reporte de inventario.</summary>
public class InventarioVm
{
    public int IdProducto { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int CapacidadKg { get; set; }
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public decimal ValorStock { get; set; }
    public string EstadoStock { get; set; } = "";
    public int Vendidos { get; set; }
}

public class UsuarioVm
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "Cliente";
    public bool Activo { get; set; } = true;
}
