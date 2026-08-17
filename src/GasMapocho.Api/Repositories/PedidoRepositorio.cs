using System.Data;
using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.Data.SqlClient;

namespace GasMapocho.Api.Repositories;

public class PedidoRepositorio : IPedidoRepositorio
{
    private readonly ConexionBD _conexion;
    private readonly IProductoRepositorio _productos;

    public PedidoRepositorio(ConexionBD conexion, IProductoRepositorio productos)
    {
        _conexion = conexion;
        _productos = productos;
    }

    /// <summary>
    /// Cabecera, detalle y confirmación de venta en UNA sola transaccion:
    /// BeginTransaction -> cabecera -> detalles (foreach, cada uno descuenta
    /// su propio stock) -> confirmar venta -> Commit, y Rollback ante
    /// cualquier error. El pedido queda Aprobado y con el stock descontado
    /// desde el momento en que el cliente compra — no hay una aprobación
    /// manual posterior del administrador.
    /// </summary>
    public int Registrar(PedidoNuevo pedido)
    {
        var lineas = pedido.Lineas.Where(l => l.Cantidad > 0).ToList();
        if (lineas.Count == 0)
            throw new InvalidOperationException("El pedido no tiene productos.");

        // El precio se resuelve aqui, contra la base. Si viniera del cliente
        // HTTP, cualquiera podria comprar un balon a un peso.
        var precios = new Dictionary<int, decimal>();
        foreach (var linea in lineas)
        {
            var producto = _productos.Obtener(linea.IdProducto)
                ?? throw new InvalidOperationException("Uno de los productos no existe.");
            precios[linea.IdProducto] = producto.Precio;
        }

        var subtotal = lineas.Sum(l => l.Cantidad * precios[l.IdProducto]);

        using var conexion = _conexion.ObtenerConexion();
        conexion.Open();

        using SqlTransaction transaccion = conexion.BeginTransaction();

        try
        {
            int idPedido;
            using (var comando = new SqlCommand("dbo.sp_Pedido_RegistrarCabecera", conexion, transaccion))
            {
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.Add("@IdCliente", SqlDbType.Int).Value = pedido.IdCliente;
                comando.Parameters.Add("@Subtotal", SqlDbType.Decimal).Value = subtotal;
                comando.Parameters.Add("@Despacho", SqlDbType.Decimal).Value = pedido.Despacho;
                comando.Parameters.Add("@MetodoPago", SqlDbType.NVarChar, 30).Value = pedido.MetodoPago;
                comando.Parameters.Add("@NombreEntrega", SqlDbType.NVarChar, 120).Value = pedido.NombreEntrega;
                comando.Parameters.Add("@TelefonoEntrega", SqlDbType.NVarChar, 20).Value = pedido.TelefonoEntrega;
                comando.Parameters.Add("@ComunaEntrega", SqlDbType.NVarChar, 80).Value = pedido.ComunaEntrega;
                comando.Parameters.Add("@DireccionEntrega", SqlDbType.NVarChar, 200).Value = pedido.DireccionEntrega;
                comando.Parameters.Add("@ReferenciaEntrega", SqlDbType.NVarChar, 200).Value =
                    (object?)pedido.ReferenciaEntrega ?? DBNull.Value;

                var salida = comando.Parameters.Add("@IdPedido", SqlDbType.Int);
                salida.Direction = ParameterDirection.Output;

                comando.ExecuteNonQuery();
                idPedido = (int)salida.Value;
            }

            foreach (var linea in lineas)
            {
                using var comando = new SqlCommand("dbo.sp_Pedido_RegistrarDetalle", conexion, transaccion);
                comando.CommandType = CommandType.StoredProcedure;

                comando.Parameters.Add("@IdPedido", SqlDbType.Int).Value = idPedido;
                comando.Parameters.Add("@IdProducto", SqlDbType.Int).Value = linea.IdProducto;
                comando.Parameters.Add("@Cantidad", SqlDbType.Int).Value = linea.Cantidad;
                comando.Parameters.Add("@PrecioUnitario", SqlDbType.Decimal).Value = precios[linea.IdProducto];

                comando.ExecuteNonQuery();
            }

            using (var comando = new SqlCommand("dbo.sp_Pedido_ConfirmarVenta", conexion, transaccion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add("@IdPedido", SqlDbType.Int).Value = idPedido;
                comando.ExecuteNonQuery();
            }

            transaccion.Commit();
            return idPedido;
        }
        catch
        {
            transaccion.Rollback();
            throw;
        }
    }

    public Pagina<Pedido> Listar(int? idCliente, string? estado, DateTime? fechaInicial,
                                 DateTime? fechaFinal, string? busqueda, int pagina, int cantidadPorPagina)
    {
        var resultado = new Pagina<Pedido>
        {
            PaginaActual = pagina,
            CantidadPorPagina = cantidadPorPagina
        };

        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Pedido_Listar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@IdCliente", SqlDbType.Int).Value = (object?)idCliente ?? DBNull.Value;
        comando.Parameters.Add("@Estado", SqlDbType.VarChar, 15).Value =
            string.IsNullOrWhiteSpace(estado) ? DBNull.Value : estado;
        comando.Parameters.Add("@FechaInicial", SqlDbType.Date).Value = (object?)fechaInicial ?? DBNull.Value;
        comando.Parameters.Add("@FechaFinal", SqlDbType.Date).Value = (object?)fechaFinal ?? DBNull.Value;
        comando.Parameters.Add("@Busqueda", SqlDbType.NVarChar, 120).Value =
            string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim();
        comando.Parameters.Add("@Pagina", SqlDbType.Int).Value = pagina;
        comando.Parameters.Add("@CantidadPorPagina", SqlDbType.Int).Value = cantidadPorPagina;

        conexion.Open();
        using var lector = comando.ExecuteReader();

        if (lector.Read()) resultado.TotalRegistros = lector.GetInt32(0);

        lector.NextResult();
        while (lector.Read()) resultado.Items.Add(MapearCabecera(lector));

        return resultado;
    }

    public Pedido? Obtener(int idPedido)
    {
        using var conexion = _conexion.ObtenerConexion();
        conexion.Open();

        Pedido? pedido;

        using (var comando = new SqlCommand("dbo.sp_Pedido_Obtener", conexion))
        {
            comando.CommandType = CommandType.StoredProcedure;
            comando.Parameters.Add("@IdPedido", SqlDbType.Int).Value = idPedido;

            using var lector = comando.ExecuteReader();
            pedido = lector.Read() ? MapearCabecera(lector) : null;
        }

        if (pedido is null) return null;

        using (var comando = new SqlCommand("dbo.sp_Pedido_ObtenerDetalle", conexion))
        {
            comando.CommandType = CommandType.StoredProcedure;
            comando.Parameters.Add("@IdPedido", SqlDbType.Int).Value = idPedido;

            using var lector = comando.ExecuteReader();
            while (lector.Read())
            {
                pedido.Detalles.Add(new LineaPedido
                {
                    IdDetalle      = lector.GetInt32(lector.GetOrdinal("IdDetalle")),
                    IdProducto     = lector.GetInt32(lector.GetOrdinal("IdProducto")),
                    Producto       = lector.GetString(lector.GetOrdinal("Producto")),
                    ImagenUrl      = lector.IsDBNull(lector.GetOrdinal("ImagenUrl"))
                                        ? null : lector.GetString(lector.GetOrdinal("ImagenUrl")),
                    CapacidadKg    = lector.GetInt32(lector.GetOrdinal("CapacidadKg")),
                    Cantidad       = lector.GetInt32(lector.GetOrdinal("Cantidad")),
                    PrecioUnitario = lector.GetDecimal(lector.GetOrdinal("PrecioUnitario")),
                    Subtotal       = lector.GetDecimal(lector.GetOrdinal("Subtotal"))
                });
            }
        }

        return pedido;
    }

    public void CambiarEstado(int idPedido, string estado)
    {
        using var conexion = _conexion.ObtenerConexion();
        conexion.Open();

        using var comando = new SqlCommand("dbo.sp_Pedido_CambiarEstado", conexion);
        comando.CommandType = CommandType.StoredProcedure;
        comando.Parameters.Add("@IdPedido", SqlDbType.Int).Value = idPedido;
        comando.Parameters.Add("@Estado", SqlDbType.VarChar, 15).Value = estado;

        comando.ExecuteNonQuery();
    }

    private static Pedido MapearCabecera(SqlDataReader lector) => new()
    {
        IdPedido          = lector.GetInt32(lector.GetOrdinal("IdPedido")),
        IdCliente         = lector.GetInt32(lector.GetOrdinal("IdCliente")),
        Cliente           = lector.GetString(lector.GetOrdinal("Cliente")),
        Fecha             = lector.GetDateTime(lector.GetOrdinal("Fecha")),
        Estado            = lector.GetString(lector.GetOrdinal("Estado")),
        Subtotal          = lector.GetDecimal(lector.GetOrdinal("Subtotal")),
        Despacho          = lector.GetDecimal(lector.GetOrdinal("Despacho")),
        Total             = lector.GetDecimal(lector.GetOrdinal("Total")),
        MetodoPago        = lector.GetString(lector.GetOrdinal("MetodoPago")),
        NombreEntrega     = lector.GetString(lector.GetOrdinal("NombreEntrega")),
        TelefonoEntrega   = lector.GetString(lector.GetOrdinal("TelefonoEntrega")),
        ComunaEntrega     = lector.GetString(lector.GetOrdinal("ComunaEntrega")),
        DireccionEntrega  = lector.GetString(lector.GetOrdinal("DireccionEntrega")),
        ReferenciaEntrega = lector.IsDBNull(lector.GetOrdinal("ReferenciaEntrega"))
                              ? null : lector.GetString(lector.GetOrdinal("ReferenciaEntrega")),
        Comuna            = lector.GetString(lector.GetOrdinal("Comuna")),
        TotalItems        = lector.GetInt32(lector.GetOrdinal("TotalItems"))
    };
}
