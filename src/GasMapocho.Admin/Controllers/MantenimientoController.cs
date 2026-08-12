using GasMapocho.Admin.Models;
using GasMapocho.Ui.Models;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Mvc;

namespace GasMapocho.Admin.Controllers;

/// <summary>
/// Los tres mantenimientos son la misma tabla con distinto contenido.
/// Cada acción solo describe columnas y filas; la vista es siempre _Tabla.
/// Los datos vienen de GasMapocho.Api, que a su vez los resuelve con
/// procedimientos almacenados.
/// </summary>
[RequiereSesion]
public class MantenimientoController : Controller
{
    private readonly ApiGasMapocho _api;

    public MantenimientoController(ApiGasMapocho api) => _api = api;

    // ---------------------------------------------------------
    // Listados
    // ---------------------------------------------------------

    public async Task<IActionResult> Productos(string? busqueda)
    {
        ViewData["Title"] = "Productos";

        var productos = await _api.ProductosAsync(busqueda);
        ViewData["ApiCaida"] = _api.HuboError;

        // Los 3 indicadores de inventario vivían en Reportes/Inventario, que ya
        // no existe como pantalla aparte: el encargo pide mantenerlos, pero
        // junto al mantenimiento de Productos (§7.2).
        ViewData["Kpis"] = true;
        ViewData["TotalProductos"] = productos.Count;
        ViewData["UnidadesStock"] = productos.Sum(p => p.Stock);
        ViewData["ValorInventario"] = productos.Sum(p => p.Stock * p.Precio);

        return View("Listado", new TablaVm
        {
            Titulo = "Productos",
            Subtitulo = "Administra el catálogo, los precios y el stock",
            PlaceholderBuscar = "Buscar producto",
            Busqueda = busqueda,
            BuscarEnServidor = true,
            TextoNuevo = "Nuevo producto",
            UrlNuevo = "/Mantenimiento/FormProducto",
            UrlBase = "/Mantenimiento/Productos",
            UrlEditar = "/Mantenimiento/FormProducto?id={0}",
            AccionEliminar = "/Mantenimiento/EliminarProducto",
            ConfirmacionEliminar = "¿Estás seguro que deseas eliminar el producto?",
            Columnas = new()
            {
                new("Código", ancho: "110px"),
                new("Producto"),
                new("Precio", numerica: true, ancho: "120px"),
                new("Stock", numerica: true, ancho: "100px"),
            },
            Filas = productos.Select(p => new FilaVm
            {
                Id = p.Id,
                Celdas = new()
                {
                    new(p.Codigo, fuerte: true),
                    new(p.Nombre),
                    new(p.Precio.ToString("C0"), numerica: true, fuerte: true),
                    new(p.Stock.ToString(), numerica: true, secundario: p.TextoStock),
                }
            }).ToList(),
            Vacio = new EstadoVacioVm("inventory_2", "No hay productos",
                                      "Registra el primer producto del catálogo.",
                                      "Nuevo producto", "/Mantenimiento/FormProducto")
        });
    }

    public async Task<IActionResult> Clientes(string? busqueda)
    {
        ViewData["Title"] = "Clientes";

        var clientes = await _api.ClientesAsync(busqueda);
        ViewData["ApiCaida"] = _api.HuboError;

        return View("Listado", new TablaVm
        {
            Titulo = "Clientes",
            Subtitulo = "Administra la información de los clientes",
            PlaceholderBuscar = "Buscar cliente",
            Busqueda = busqueda,
            BuscarEnServidor = true,
            TextoNuevo = "Nuevo cliente",
            UrlNuevo = "/Mantenimiento/FormCliente",
            UrlBase = "/Mantenimiento/Clientes",
            UrlEditar = "/Mantenimiento/FormCliente?id={0}",
            AccionEliminar = "/Mantenimiento/EliminarCliente",
            ConfirmacionEliminar = "¿Estás seguro que deseas eliminar el cliente?",
            Columnas = new()
            {
                new("Nombre"),
                new("RUT", ancho: "130px"),
                new("Correo"),
                new("Teléfono", ancho: "150px"),
                new("Comuna", ancho: "130px"),
                new("Dirección"),
            },
            Filas = clientes.Select(c => new FilaVm
            {
                Id = c.Id,
                Celdas = new()
                {
                    new(c.Nombre, fuerte: true),
                    new(c.Documento),
                    new(c.Email),
                    new(c.Telefono),
                    new(c.Comuna),
                    new(c.Direccion),
                }
            }).ToList(),
            Vacio = new EstadoVacioVm("group", "No hay clientes", "Registra el primer cliente.",
                                      "Nuevo cliente", "/Mantenimiento/FormCliente")
        });
    }

    public async Task<IActionResult> Usuarios()
    {
        ViewData["Title"] = "Usuarios";

        var usuarios = await _api.UsuariosAsync();
        ViewData["ApiCaida"] = _api.HuboError;

        return View("Listado", new TablaVm
        {
            Titulo = "Usuarios",
            Subtitulo = "Cuentas de acceso y sus roles",
            PlaceholderBuscar = "Buscar usuario",
            // El alta de cuentas no está en el alcance: se muestran las
            // existentes, sin acciones por fila.
            ConAcciones = false,
            Columnas = new()
            {
                new("Nombre"),
                new("Correo"),
                new("Rol", ancho: "160px"),
                new("Estado", ancho: "120px"),
            },
            Filas = usuarios.Select(u => new FilaVm
            {
                Id = u.Id,
                Celdas = new()
                {
                    new(u.Nombre, fuerte: true),
                    new(u.Email),
                    new(u.Rol),
                    new(u.Activo ? "Activo" : "Inactivo"),
                }
            }).ToList(),
            Vacio = new EstadoVacioVm("badge", "No hay usuarios", "No hay cuentas registradas.")
        });
    }

    // ---------------------------------------------------------
    // Producto: alta, edición y baja
    // ---------------------------------------------------------

    public async Task<IActionResult> FormProducto(int? id)
    {
        ViewData["Title"] = id is null ? "Nuevo producto" : "Editar producto";

        var producto = id is null ? null : await _api.ProductoAsync(id.Value);
        if (id is not null && producto is null) return NotFound();

        return View("FormProducto", producto ?? new ProductoVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FormProducto(ProductoVm vm)
    {
        ViewData["Title"] = vm.Id == 0 ? "Nuevo producto" : "Editar producto";

        // Ante datos inválidos se permanece en el formulario mostrando los
        // errores, sin perder lo que el usuario ya escribió.
        if (!ModelState.IsValid) return View("FormProducto", vm);

        var r = vm.Id == 0
            ? await _api.CrearProductoAsync(vm)
            : await _api.ActualizarProductoAsync(vm);

        if (!r.Exito)
        {
            ModelState.AddModelError(string.Empty, r.Mensaje);
            return View("FormProducto", vm);
        }

        TempData["Ok"] = vm.Id == 0
            ? "Producto registrado correctamente."
            : "Producto actualizado correctamente.";

        return RedirectToAction(nameof(Productos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarProducto(int id)
    {
        var r = await _api.EliminarProductoAsync(id);

        // La API responde 409 si el producto tiene pedidos asociados.
        if (r.Exito) TempData["Ok"] = "Producto dado de baja correctamente.";
        else TempData["Error"] = r.Mensaje;

        return RedirectToAction(nameof(Productos));
    }

    // ---------------------------------------------------------
    // Cliente: alta, edición y baja
    // ---------------------------------------------------------

    public async Task<IActionResult> FormCliente(int? id)
    {
        ViewData["Title"] = id is null ? "Nuevo cliente" : "Editar cliente";

        var cliente = id is null ? null : await _api.ClienteAsync(id.Value);
        if (id is not null && cliente is null) return NotFound();

        return View("FormCliente", cliente ?? new ClienteVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FormCliente(ClienteVm vm)
    {
        ViewData["Title"] = vm.Id == 0 ? "Nuevo cliente" : "Editar cliente";

        if (!ModelState.IsValid) return View("FormCliente", vm);

        var r = vm.Id == 0
            ? await _api.CrearClienteAsync(vm)
            : await _api.ActualizarClienteAsync(vm);

        if (!r.Exito)
        {
            ModelState.AddModelError(string.Empty, r.Mensaje);
            return View("FormCliente", vm);
        }

        TempData["Ok"] = vm.Id == 0
            ? "Cliente registrado correctamente."
            : "Cliente actualizado correctamente.";

        return RedirectToAction(nameof(Clientes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCliente(int id)
    {
        var r = await _api.EliminarClienteAsync(id);

        if (r.Exito) TempData["Ok"] = "Cliente dado de baja correctamente.";
        else TempData["Error"] = r.Mensaje;

        return RedirectToAction(nameof(Clientes));
    }
}
