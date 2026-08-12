using System.Data;
using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using GasMapocho.Api.Services;
using Microsoft.Data.SqlClient;

namespace GasMapocho.Api.Repositories;

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly ConexionBD _conexion;

    public UsuarioRepositorio(ConexionBD conexion) => _conexion = conexion;

    /// <summary>
    /// El hash y la sal se leen, se comparan en memoria y no salen de aqui.
    /// Devuelve null tanto si el correo no existe como si la contrasena es
    /// incorrecta o la cuenta esta desactivada: quien llama no puede
    /// distinguir los tres casos, y por eso el mensaje al usuario es unico.
    /// </summary>
    public UsuarioAutenticado? Autenticar(string email, string password)
    {
        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Usuario_Autenticar", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };
        comando.Parameters.Add("@Email", SqlDbType.NVarChar, 120).Value = email;

        conexion.Open();
        using var lector = comando.ExecuteReader();

        if (!lector.Read()) return null;

        if (!lector.GetBoolean(lector.GetOrdinal("Activo"))) return null;

        var hash = (byte[])lector["PasswordHash"];
        var sal = (byte[])lector["PasswordSalt"];

        if (!Seguridad.Verificar(password, hash, sal)) return null;

        return new UsuarioAutenticado
        {
            IdUsuario = lector.GetInt32(lector.GetOrdinal("IdUsuario")),
            Email     = lector.GetString(lector.GetOrdinal("Email")),
            Nombre    = lector.GetString(lector.GetOrdinal("Nombre")),
            Rol       = lector.GetString(lector.GetOrdinal("Rol")),
            IdCliente = lector.IsDBNull(lector.GetOrdinal("IdCliente"))
                          ? null : lector.GetInt32(lector.GetOrdinal("IdCliente"))
        };
    }

    public Pagina<Usuario> Listar(string? busqueda, int pagina, int cantidadPorPagina)
    {
        var resultado = new Pagina<Usuario>
        {
            PaginaActual = pagina,
            CantidadPorPagina = cantidadPorPagina
        };

        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Usuario_Listar", conexion)
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
        while (lector.Read())
        {
            resultado.Items.Add(new Usuario
            {
                IdUsuario = lector.GetInt32(lector.GetOrdinal("IdUsuario")),
                Email     = lector.GetString(lector.GetOrdinal("Email")),
                Nombre    = lector.GetString(lector.GetOrdinal("Nombre")),
                Rol       = lector.GetString(lector.GetOrdinal("Rol")),
                Activo    = lector.GetBoolean(lector.GetOrdinal("Activo"))
            });
        }

        return resultado;
    }

    public string ActualizarPassword(string email, string nuevaPassword)
    {
        var sal = Seguridad.GenerarSal();
        var hash = Seguridad.Hash(nuevaPassword, sal);

        using var conexion = _conexion.ObtenerConexion();
        using var comando = new SqlCommand("dbo.sp_Usuario_ActualizarPassword", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };

        comando.Parameters.Add("@Email", SqlDbType.NVarChar, 120).Value = email;
        comando.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 64).Value = hash;
        comando.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 32).Value = sal;

        var mensaje = comando.Parameters.Add("@Mensaje", SqlDbType.VarChar, 200);
        mensaje.Direction = ParameterDirection.Output;

        conexion.Open();
        comando.ExecuteNonQuery();

        return (string)mensaje.Value;
    }
}
