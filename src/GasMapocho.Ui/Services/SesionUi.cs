using GasMapocho.Ui.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GasMapocho.Ui.Services;

/// <summary>
/// Sesion de servidor compartida por los dos frontends. La identidad se
/// guarda aqui tras validar las credenciales contra la API; el navegador solo
/// recibe una cookie de sesion HttpOnly, nunca los datos del usuario.
/// </summary>
public static class SesionUi
{
    public const string IdUsuario = "IdUsuario";
    public const string IdCliente = "IdCliente";
    public const string Nombre = "Nombre";
    public const string Rol = "Rol";
    public const string Email = "Email";

    public static void Guardar(this ISession sesion, IdentidadVm identidad)
    {
        sesion.SetInt32(IdUsuario, identidad.IdUsuario);
        sesion.SetString(Nombre, identidad.Nombre);
        sesion.SetString(Rol, identidad.Rol);
        sesion.SetString(Email, identidad.Email);

        if (identidad.IdCliente is int idCliente)
            sesion.SetInt32(IdCliente, idCliente);
    }

    public static bool Autenticado(this ISession sesion) => sesion.GetInt32(IdUsuario) is not null;

    public static string NombreUsuario(this ISession sesion) =>
        sesion.GetString(Nombre) ?? "Invitado";

    public static string RolUsuario(this ISession sesion) => sesion.GetString(Rol) ?? "";

    public static int? ClienteActual(this ISession sesion) => sesion.GetInt32(IdCliente);
}

/// <summary>
/// Exige sesion iniciada. Se aplica en el servidor porque ocultar un enlace
/// del menu no impide escribir la URL a mano.
/// </summary>
public class RequiereSesionAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is SinSesionAttribute))
            return;

        if (context.HttpContext.Session.GetInt32(SesionUi.IdUsuario) is null)
        {
            var destino = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { retorno = destino });
        }
    }
}

/// <summary>Marca una accion accesible sin sesion (el login, por ejemplo).</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SinSesionAttribute : Attribute { }
