using System.Data;
using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.Data.SqlClient;

namespace GasMapocho.Api.Repositories;

public class ProductoRepositorio : IProductoRepositorio
{
    private readonly ConexionBD _conexion;

    public ProductoRepositorio(ConexionBD conexion) => _conexion = conexion;

    public PaginaDeProductos Listar(string? busqueda, int pagina, int cantidadPorPagina)
    {
        var resultado = new PaginaDeProductos
        {
            Pagina = pagina,
            CantidadPorPagina = cantidadPorPagina
        };

        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Producto_Listar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@Busqueda", SqlDbType.NVarChar, 120).Value =
            string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim();
        comando.Parameters.Add("@Pagina", SqlDbType.Int).Value = pagina;
        comando.Parameters.Add("@CantidadPorPagina", SqlDbType.Int).Value = cantidadPorPagina;
        comando.Parameters.Add("@IncluirInactivos", SqlDbType.Bit).Value = false;

        conexion.Open();
        using var lector = comando.ExecuteReader();

        if (lector.Read())
            resultado.TotalRegistros = lector.GetInt32(0);

        lector.NextResult();
        while (lector.Read())
            resultado.Items.Add(Mapear(lector));

        return resultado;
    }

    public Producto? Obtener(int idProducto)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Producto_Obtener", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };
        comando.Parameters.Add("@IdProducto", SqlDbType.Int).Value = idProducto;

        conexion.Open();
        using var lector = comando.ExecuteReader();
        return lector.Read() ? Mapear(lector) : null;
    }

    public int Registrar(Producto producto)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Producto_Registrar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@Codigo", SqlDbType.NVarChar, 20).Value = producto.Codigo;
        comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 120).Value = producto.Nombre;
        comando.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 300).Value =
            (object?)producto.Descripcion ?? DBNull.Value;
        comando.Parameters.Add("@CapacidadKg", SqlDbType.Int).Value = producto.CapacidadKg;
        comando.Parameters.Add("@Precio", SqlDbType.Decimal).Value = producto.Precio;
        comando.Parameters.Add("@Stock", SqlDbType.Int).Value = producto.Stock;

        var salida = comando.Parameters.Add("@IdProducto", SqlDbType.Int);
        salida.Direction = ParameterDirection.Output;

        conexion.Open();
        comando.ExecuteNonQuery();

        return (int)salida.Value;
    }

    public void Actualizar(Producto producto)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Producto_Actualizar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@IdProducto", SqlDbType.Int).Value = producto.IdProducto;
        comando.Parameters.Add("@Codigo", SqlDbType.NVarChar, 20).Value = producto.Codigo;
        comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 120).Value = producto.Nombre;
        comando.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 300).Value =
            (object?)producto.Descripcion ?? DBNull.Value;
        comando.Parameters.Add("@CapacidadKg", SqlDbType.Int).Value = producto.CapacidadKg;
        comando.Parameters.Add("@Precio", SqlDbType.Decimal).Value = producto.Precio;
        comando.Parameters.Add("@Stock", SqlDbType.Int).Value = producto.Stock;
        comando.Parameters.Add("@Activo", SqlDbType.Bit).Value = producto.Activo;

        conexion.Open();
        comando.ExecuteNonQuery();
    }

    public void Eliminar(int idProducto)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Producto_Eliminar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };
        comando.Parameters.Add("@IdProducto", SqlDbType.Int).Value = idProducto;

        conexion.Open();
        comando.ExecuteNonQuery();
    }

    private static Producto Mapear(SqlDataReader lector) => new()
    {
        IdProducto  = lector.GetInt32(lector.GetOrdinal("IdProducto")),
        Codigo      = lector.GetString(lector.GetOrdinal("Codigo")),
        Nombre      = lector.GetString(lector.GetOrdinal("Nombre")),
        Descripcion = lector.IsDBNull(lector.GetOrdinal("Descripcion"))
                        ? null : lector.GetString(lector.GetOrdinal("Descripcion")),
        CapacidadKg = lector.GetInt32(lector.GetOrdinal("CapacidadKg")),
        Precio      = lector.GetDecimal(lector.GetOrdinal("Precio")),
        Stock       = lector.GetInt32(lector.GetOrdinal("Stock")),
        Activo      = lector.GetBoolean(lector.GetOrdinal("Activo"))
    };
}
