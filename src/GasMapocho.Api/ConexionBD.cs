using Microsoft.Data.SqlClient;

namespace GasMapocho.Api;

/// <summary>
/// La API tiene su propia clase de conexion y su propio repositorio: el plan
/// decide no crear librerias compartidas entre proyectos, de modo que cada
/// aplicacion es autocontenida y se puede abrir y explicar por separado.
/// </summary>
public class ConexionBD
{
    private readonly string _cadena;

    public ConexionBD(IConfiguration configuration)
    {
        _cadena = configuration.GetConnectionString("GasMapocho")
                  ?? throw new InvalidOperationException(
                      "Falta la cadena de conexion 'GasMapocho' en appsettings.json.");
    }

    public SqlConnection ObtenerConexion() => new SqlConnection(_cadena);
}
