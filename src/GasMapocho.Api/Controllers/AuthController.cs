using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Api.Controllers;

/// <summary>
///     POST /api/auth/login   valida credenciales y devuelve la identidad
///
/// No emite token: el plan descarta JWT. Cada frontend guarda el resultado en
/// su propia sesion de servidor, con cookie HttpOnly.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioRepositorio _usuarios;

    public AuthController(IUsuarioRepositorio usuarios) => _usuarios = usuarios;

    [HttpPost("login")]
    public ActionResult<UsuarioAutenticado> Login([FromBody] LoginRequest credenciales)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var usuario = _usuarios.Autenticar(credenciales.Email, credenciales.Password);

        // 401 con un mensaje unico: no se revela si fallo el correo, la
        // contrasena o si la cuenta esta desactivada.
        if (usuario is null)
            return Unauthorized(new { mensaje = "El correo o la contraseña no son correctos." });

        return Ok(usuario);
    }

    /// <summary>
    ///     POST /api/auth/recuperar-password   cambia la clave de una cuenta de Administrador
    ///
    /// Solo funciona para cuentas con rol Administrador (la recuperacion de
    /// clientes no esta contemplada). El mensaje de error del SP no distingue
    /// si el correo no existe, no es admin o esta desactivado.
    /// </summary>
    [HttpPost("recuperar-password")]
    public ActionResult<string> RecuperarPassword([FromBody] RecuperarPasswordRequest datos)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var mensaje = _usuarios.ActualizarPassword(datos.Email, datos.NuevaPassword);

        if (mensaje.StartsWith("No fue posible", StringComparison.Ordinal))
            return BadRequest(new { mensaje });

        return Ok(new { mensaje });
    }
}
