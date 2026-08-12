using GasMapocho.Api.Models;

namespace GasMapocho.Api.Interfaces;

public interface IClienteRepositorio
{
    Pagina<Cliente> Listar(string? busqueda, int pagina, int cantidadPorPagina);
    Cliente? Obtener(int idCliente);
    int Registrar(Cliente cliente);
    void Actualizar(Cliente cliente);
    void Eliminar(int idCliente);
}

public interface IPedidoRepositorio
{
    Pagina<Pedido> Listar(int? idCliente, string? estado, DateTime? fechaInicial,
                          DateTime? fechaFinal, string? busqueda, int pagina, int cantidadPorPagina);
    Pedido? Obtener(int idPedido);

    /// <summary>
    /// Cabecera + detalle + descuento de stock + venta confirmada, todo en
    /// una sola transaccion. Devuelve el id creado.
    /// </summary>
    int Registrar(PedidoNuevo pedido);
}

public interface IUsuarioRepositorio
{
    /// <summary>Valida las credenciales. Null si no coinciden o la cuenta esta inactiva.</summary>
    UsuarioAutenticado? Autenticar(string email, string password);

    Pagina<Usuario> Listar(string? busqueda, int pagina, int cantidadPorPagina);

    /// <summary>
    /// Cambia la contrasena de una cuenta de Administrador activa. Devuelve el
    /// mensaje del procedimiento (exito o el mismo mensaje generico de error
    /// para cualquier motivo de rechazo).
    /// </summary>
    string ActualizarPassword(string email, string nuevaPassword);
}

public interface IReporteRepositorio
{
    Pagina<FilaInventario> Inventario(int pagina, int cantidadPorPagina);
}
