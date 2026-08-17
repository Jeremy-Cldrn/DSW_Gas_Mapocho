using System.ComponentModel.DataAnnotations;

namespace GasMapocho.Api.Models;

// ============================================================
// Modelos que la API expone como JSON. No son las entidades de la
// base: PasswordHash y PasswordSalt, por ejemplo, jamas salen de
// aqui hacia un cliente.
// ============================================================

public class Cliente
{
    public int IdCliente { get; set; }
    public int? IdUsuario { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El RUT es obligatorio.")]
    [StringLength(20)]
    public string Documento { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(120)]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La comuna es obligatoria.")]
    [StringLength(80)]
    public string Comuna { get; set; } = "";

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(200)]
    public string Direccion { get; set; } = "";

    public bool Activo { get; set; } = true;
}

public class LineaPedido
{
    public int IdDetalle { get; set; }
    public int IdProducto { get; set; }
    public string Producto { get; set; } = "";
    public string? ImagenUrl { get; set; }
    public int CapacidadKg { get; set; }

    [Range(1, 999, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class Pedido
{
    public int IdPedido { get; set; }
    public int IdCliente { get; set; }
    public string Cliente { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public decimal Subtotal { get; set; }
    public decimal Despacho { get; set; }
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = "";
    public string NombreEntrega { get; set; } = "";
    public string TelefonoEntrega { get; set; } = "";
    public string ComunaEntrega { get; set; } = "";
    public string DireccionEntrega { get; set; } = "";
    public string? ReferenciaEntrega { get; set; }

    /// <summary>Comuna registrada en la ficha del cliente (distinta de la de entrega).</summary>
    public string Comuna { get; set; } = "";

    /// <summary>Unidades del pedido, sumadas en SQL: el listado no trae el detalle.</summary>
    public int TotalItems { get; set; }

    public List<LineaPedido> Detalles { get; set; } = new();
}

/// <summary>Lo que envia un frontend para registrar un pedido.</summary>
public class PedidoNuevo
{
    [Range(1, int.MaxValue, ErrorMessage = "Falta el cliente.")]
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "Falta el método de pago.")]
    public string MetodoPago { get; set; } = "";

    [Required] public string NombreEntrega { get; set; } = "";
    [Required] public string TelefonoEntrega { get; set; } = "";
    [Required] public string ComunaEntrega { get; set; } = "";
    [Required] public string DireccionEntrega { get; set; } = "";
    public string? ReferenciaEntrega { get; set; }

    public decimal Despacho { get; set; }

    [MinLength(1, ErrorMessage = "El pedido no tiene productos.")]
    public List<LineaNueva> Lineas { get; set; } = new();
}

public class LineaNueva
{
    public int IdProducto { get; set; }

    [Range(1, 999, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Cantidad { get; set; }
}

/// <summary>Lo que envía el panel Admin para asignar el estado de un pedido.</summary>
public class CambiarEstadoPedido
{
    [Required(ErrorMessage = "Falta el estado.")]
    [AllowedValues("Pendiente", "Aprobado", "Rechazado", "En Proceso", "Entregado", "Finalizado", "Cancelado",
                   ErrorMessage = "El estado no es válido.")]
    public string Estado { get; set; } = "";
}

// ------------------------------------------------------------ Autenticacion

public class LoginRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = "";
}

/// <summary>Recuperación de contraseña, exclusiva para cuentas de Administrador.</summary>
public class RecuperarPasswordRequest
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string NuevaPassword { get; set; } = "";
}

/// <summary>
/// Identidad del usuario autenticado. No lleva token: cada frontend guarda
/// estos datos en su propia sesion de servidor.
/// </summary>
public class UsuarioAutenticado
{
    public int IdUsuario { get; set; }
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
    /// <summary>Null si el usuario no tiene ficha de cliente (por ejemplo un administrador).</summary>
    public int? IdCliente { get; set; }
}

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Rol { get; set; } = "";
    public bool Activo { get; set; }
}

// ------------------------------------------------------------ Reportes

public class FilaInventario
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

// ------------------------------------------------------------ Paginacion

/// <summary>Envoltorio generico de un listado paginado.</summary>
public class Pagina<T>
{
    public List<T> Items { get; set; } = new();
    public int PaginaActual { get; set; }
    public int CantidadPorPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas =>
        TotalRegistros == 0 ? 1 : (int)Math.Ceiling(TotalRegistros / (double)CantidadPorPagina);

    /// <summary>Totalizador opcional, usado por los reportes.</summary>
    public decimal Total { get; set; }
}
