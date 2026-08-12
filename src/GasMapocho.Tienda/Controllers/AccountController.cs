using System.ComponentModel.DataAnnotations;
using GasMapocho.Ui.Models;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Tienda.Controllers;

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

public class AccountController : Controller
{
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

        // Un unico mensaje para todos los casos: correo inexistente,
        // contrasena incorrecta, cuenta desactivada o rol equivocado. Decir
        // cual fallo le confirmaria a un atacante que ese correo existe.
        //
        // La tienda solo acepta el perfil Cliente: las credenciales de un
        // administrador reciben exactamente el mismo mensaje.
        if (identidad is null || identidad.Rol != "Cliente")
        {
            ModelState.AddModelError(string.Empty, "El correo o la contraseña no son correctos.");
            return View(vm);
        }

        HttpContext.Session.Guardar(identidad);

        // Solo destinos locales: un retorno con URL absoluta convertiria el
        // login en un trampolin hacia otro sitio.
        if (!string.IsNullOrEmpty(retorno) && Url.IsLocalUrl(retorno))
            return Redirect(retorno);

        return RedirectToAction("Index", "Catalogo");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["Ok"] = "Cerraste sesión correctamente.";
        return RedirectToAction(nameof(Login));
    }
}
