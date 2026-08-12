using System.Data;
using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.Data.SqlClient;

namespace GasMapocho.Api.Repositories;

public class ReporteRepositorio : IReporteRepositorio
{
    private readonly ConexionBD _conexion;

    public ReporteRepositorio(ConexionBD conexion) => _conexion = conexion;

    /// <summary>
    /// El reporte de ventas no necesita repositorio propio: es el listado de
    /// pedidos filtrado por estado Aprobado, que ya resuelve sp_Pedido_Listar.
    /// Este es el de inventario, con su valorizacion calculada en SQL.
    /// </summary>
    public Pagina<FilaInventario> Inventario(int pagina, int cantidadPorPagina)
    {
        var resultado = new Pagina<FilaInventario>
        {
            PaginaActual = pagina,
            CantidadPorPagina = cantidadPorPagina
        };

        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Reporte_Inventario", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@Pagina", SqlDbType.Int).Value = pagina;
        comando.Parameters.Add("@CantidadPorPagina", SqlDbType.Int).Value = cantidadPorPagina;

        conexion.Open();
        using var lector = comando.ExecuteReader();

        if (lector.Read())
        {
            resultado.TotalRegistros = lector.GetInt32(0);
            resultado.Total = lector.GetDecimal(1);   // valorizacion del inventario
        }

        lector.NextResult();
        while (lector.Read())
        {
            resultado.Items.Add(new FilaInventario
            {
                IdProducto  = lector.GetInt32(lector.GetOrdinal("IdProducto")),
                Codigo      = lector.GetString(lector.GetOrdinal("Codigo")),
                Nombre      = lector.GetString(lector.GetOrdinal("Nombre")),
                CapacidadKg = lector.GetInt32(lector.GetOrdinal("CapacidadKg")),
                Precio      = lector.GetDecimal(lector.GetOrdinal("Precio")),
                Stock       = lector.GetInt32(lector.GetOrdinal("Stock")),
                ValorStock  = lector.GetDecimal(lector.GetOrdinal("ValorStock")),
                EstadoStock = lector.GetString(lector.GetOrdinal("EstadoStock")),
                Vendidos    = lector.GetInt32(lector.GetOrdinal("Vendidos"))
            });
        }

        return resultado;
    }
}
