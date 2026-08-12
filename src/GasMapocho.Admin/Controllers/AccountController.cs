using System.ComponentModel.DataAnnotations;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Admin.Controllers;

public class LoginVm
{
    [Required(ErrorMessage = "Ingresa tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";
}

public class RecuperarPasswordVm
{
    [Required(ErrorMessage = "Ingresa tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Ingresa la nueva contraseña.")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string NuevaPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirma la nueva contraseña.")]
    [Compare(nameof(NuevaPassword), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmarPassword { get; set; } = "";
}

public class AccountController : Controller
{
    // El panel es exclusivo de esta cuenta: aunque otro usuario tenga rol
    // Administrador en la base, no debe poder entrar aquí (§5.1 del encargo).
    private const string CorreoAdmin = "admin@gasmapocho.cl";

    private readonly ApiGasMapocho _api;

    public AccountController(ApiGasMapocho api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? retorno = null)
    {
        ViewData["Title"] = "Iniciar sesión";
        ViewData["Retorno"] = retorno;
        return View(new LoginVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string? retorno = null)
    {
        ViewData["Title"] = "Iniciar sesión";
        ViewData["Retorno"] = retorno;

        if (!ModelState.IsValid) return View(vm);

        var identidad = await _api.LoginAsync(vm.Email, vm.Password);

        // El panel solo acepta el perfil Administrador y, además, únicamente
        // la cuenta admin@gasmapocho.cl: otra cuenta con ese mismo rol (por
        // ejemplo una de ventas) recibe el mismo mensaje genérico que un
        // correo inexistente o una contraseña incorrecta.
        if (identidad is null || identidad.Rol != "Administrador" ||
            !identidad.Email.Equals(CorreoAdmin, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "El correo o la contraseña no son correctos.");
            return View(vm);
        }

        HttpContext.Session.Guardar(identidad);

        if (!string.IsNullOrEmpty(retorno) && Url.IsLocalUrl(retorno))
            return Redirect(retorno);

        return RedirectToAction("Index", "Ventas");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Ok"] = "Cerraste sesión correctamente.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult RecuperarPassword()
    {
        ViewData["Title"] = "Recuperar contraseña";
        return View(new RecuperarPasswordVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperarPassword(RecuperarPasswordVm vm)
    {
        ViewData["Title"] = "Recuperar contraseña";

        if (!ModelState.IsValid) return View(vm);

        var (exito, _) = await _api.RecuperarPasswordAsync(vm.Email, vm.NuevaPassword);

        // Mismo criterio que el login: el mensaje no distingue si el correo
        // no existe, no es de un administrador o algo falló en el envío.
        if (!exito)
        {
            ModelState.AddModelError(string.Empty,
                "No fue posible actualizar la contraseña con los datos ingresados.");
            return View(vm);
        }

        TempData["CorreoRecuperado"] = vm.Email;
        return RedirectToAction(nameof(RecuperarPasswordListo));
    }

    [HttpGet]
    public IActionResult RecuperarPasswordListo()
    {
        // Si alguien entra directo a esta URL sin haber pasado por el paso
        // anterior, TempData está vacío: se lo manda de vuelta al login.
        if (TempData["CorreoRecuperado"] is not string correo || string.IsNullOrEmpty(correo))
            return RedirectToAction(nameof(Login));

        ViewData["Title"] = "Contraseña actualizada";
        ViewBag.Correo = correo;
        return View();
    }
}
