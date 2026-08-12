using System.Data;
using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.Data.SqlClient;

namespace GasMapocho.Api.Repositories;

public class ClienteRepositorio : IClienteRepositorio
{
    private readonly ConexionBD _conexion;

    public ClienteRepositorio(ConexionBD conexion) => _conexion = conexion;

    public Pagina<Cliente> Listar(string? busqueda, int pagina, int cantidadPorPagina)
    {
        var resultado = new Pagina<Cliente>
        {
            PaginaActual = pagina,
            CantidadPorPagina = cantidadPorPagina
        };

        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Cliente_Listar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@Busqueda", SqlDbType.NVarChar, 120).Value =
            string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim();
        comando.Parameters.Add("@Pagina", SqlDbType.Int).Value = pagina;
        comando.Parameters.Add("@CantidadPorPagina", SqlDbType.Int).Value = cantidadPorPagina;

        conexion.Open();
        using var lector = comando.ExecuteReader();

        if (lector.Read()) resultado.TotalRegistros = lector.GetInt32(0);

        lector.NextResult();
        while (lector.Read()) resultado.Items.Add(Mapear(lector));

        return resultado;
    }

    public Cliente? Obtener(int idCliente)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Cliente_Obtener", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };
        comando.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

        conexion.Open();
        using var lector = comando.ExecuteReader();
        return lector.Read() ? Mapear(lector) : null;
    }

    public int Registrar(Cliente cliente)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Cliente_Registrar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 120).Value = cliente.Nombre;
        comando.Parameters.Add("@Documento", SqlDbType.NVarChar, 20).Value = cliente.Documento;
        comando.Parameters.Add("@Telefono", SqlDbType.NVarChar, 20).Value = cliente.Telefono;
        comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 120).Value = cliente.Correo;
        comando.Parameters.Add("@Comuna", SqlDbType.NVarChar, 80).Value = cliente.Comuna;
        comando.Parameters.Add("@Direccion", SqlDbType.NVarChar, 200).Value = cliente.Direccion;

        var salida = comando.Parameters.Add("@IdCliente", SqlDbType.Int);
        salida.Direction = ParameterDirection.Output;

        conexion.Open();
        comando.ExecuteNonQuery();

        return (int)salida.Value;
    }

    public void Actualizar(Cliente cliente)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Cliente_Actualizar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@IdCliente", SqlDbType.Int).Value = cliente.IdCliente;
        comando.Parameters.Add("@Nombre", SqlDbType.NVarChar, 120).Value = cliente.Nombre;
        comando.Parameters.Add("@Documento", SqlDbType.NVarChar, 20).Value = cliente.Documento;
        comando.Parameters.Add("@Telefono", SqlDbType.NVarChar, 20).Value = cliente.Telefono;
        comando.Parameters.Add("@Correo", SqlDbType.NVarChar, 120).Value = cliente.Correo;
        comando.Parameters.Add("@Comuna", SqlDbType.NVarChar, 80).Value = cliente.Comuna;
        comando.Parameters.Add("@Direccion", SqlDbType.NVarChar, 200).Value = cliente.Direccion;

        conexion.Open();
        comando.ExecuteNonQuery();
    }

    public void Eliminar(int idCliente)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Cliente_Eliminar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };
        comando.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

        conexion.Open();
        comando.ExecuteNonQuery();
    }

    private static Cliente Mapear(SqlDataReader lector) => new()
    {
        IdCliente = lector.GetInt32(lector.GetOrdinal("IdCliente")),
        IdUsuario = lector.IsDBNull(lector.GetOrdinal("IdUsuario"))
                      ? null : lector.GetInt32(lector.GetOrdinal("IdUsuario")),
        Nombre    = lector.GetString(lector.GetOrdinal("Nombre")),
        Documento = lector.GetString(lector.GetOrdinal("Documento")),
        Telefono  = lector.GetString(lector.GetOrdinal("Telefono")),
        Correo    = lector.GetString(lector.GetOrdinal("Correo")),
        Comuna    = lector.GetString(lector.GetOrdinal("Comuna")),
        Direccion = lector.GetString(lector.GetOrdinal("Direccion")),
        Activo    = lector.GetBoolean(lector.GetOrdinal("Activo"))
    };
}
