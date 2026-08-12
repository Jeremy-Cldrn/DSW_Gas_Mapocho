using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GasMapocho.Ui.Models;
using Microsoft.Extensions.Logging;

namespace GasMapocho.Ui.Services;

/// <summary>
/// Acceso a GasMapocho.Api desde los dos frontends.
///
/// Sustituye a la antigua clase <c>Datos</c> de datos falsos: es el punto
/// unico donde se decide de donde vienen los datos, tal como anticipaban los
/// comentarios de las vistas. Ninguna vista cambio al conectar el backend.
///
/// Vive en la RCL compartida (que ya comparten Tienda y Admin) y no en un
/// proyecto nuevo: el plan descarta crear una libreria GasMapocho.ApiClient,
/// pero duplicar este archivo en las dos aplicaciones seria peor.
///
/// Traduce las respuestas JSON a los ViewModels que las vistas ya usaban.
/// </summary>
public class ApiGasMapocho
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiGasMapocho> _log;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public ApiGasMapocho(HttpClient http, ILogger<ApiGasMapocho> log)
    {
        _http = http;
        _log = log;
    }

    /// <summary>Costo de despacho. Es una regla del negocio, no un dato de la base.</summary>
    public const decimal CostoDespacho = 2500;

    /// <summary>
    /// Queda en true si alguna lectura falló durante esta petición.
    ///
    /// Existe porque una lectura fallida devuelve una lista vacía, y sin esta
    /// marca la pantalla diría "no hay productos" cuando lo cierto es que el
    /// backend no respondió. Para una distribuidora de gas, esas dos frases
    /// no significan lo mismo.
    /// </summary>
    public bool HuboError { get; private set; }

    // ============================================================
    // Autenticacion
    // ============================================================

    /// <summary>
    /// Devuelve la identidad si las credenciales son validas, o null en
    /// cualquier otro caso. Quien llama no puede distinguir por que fallo.
    /// </summary>
    public async Task<IdentidadVm?> LoginAsync(string email, string password)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("api/auth/login", new { email, password });
            if (!r.IsSuccessStatusCode) return null;

            return await r.Content.ReadFromJsonAsync<IdentidadVm>(Json);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fallo el login contra la API");
            return null;
        }
    }

    /// <summary>
    /// Recuperación de contraseña, exclusiva para cuentas de Administrador.
    /// Devuelve el mensaje de la API tal cual (éxito o el mismo mensaje
    /// genérico de rechazo) para mostrarlo directamente en la vista.
    /// </summary>
    public async Task<(bool Exito, string Mensaje)> RecuperarPasswordAsync(string email, string nuevaPassword)
    {
        try
        {
            var r = await _http.PostAsJsonAsync("api/auth/recuperar-password",
                new { email, nuevaPassword });

            using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
            var mensaje = doc.RootElement.TryGetProperty("mensaje", out var m)
                ? m.GetString() ?? ""
                : "";

            return (r.IsSuccessStatusCode, mensaje);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fallo la recuperación de contraseña contra la API");
            return (false, "No fue posible actualizar la contraseña con los datos ingresados.");
        }
    }

    // ============================================================
    // Productos
    // ============================================================

    public async Task<List<ProductoVm>> ProductosAsync(string? busqueda = null, int cantidad = 100)
    {
        var url = $"api/productos?cantidadPorPagina={cantidad}";
        if (!string.IsNullOrWhiteSpace(busqueda))
            url += $"&busqueda={Uri.EscapeDataString(busqueda)}";

        var pagina = await LeerAsync<PaginaApi<ProductoApi>>(url);
        return pagina?.Items.Select(MapProducto).ToList() ?? new List<ProductoVm>();
    }

    public async Task<ProductoVm?> ProductoAsync(int id)
    {
        var p = await LeerAsync<ProductoApi>($"api/productos/{id}");
        return p is null ? null : MapProducto(p);
    }

    public Task<Resultado> CrearProductoAsync(ProductoVm vm) =>
        EnviarAsync(HttpMethod.Post, "api/productos", CuerpoProducto(vm));

    public Task<Resultado> ActualizarProductoAsync(ProductoVm vm) =>
        EnviarAsync(HttpMethod.Put, $"api/productos/{vm.Id}", CuerpoProducto(vm));

    public Task<Resultado> EliminarProductoAsync(int id) =>
        EnviarAsync(HttpMethod.Delete, $"api/productos/{id}", null);

    private static object CuerpoProducto(ProductoVm vm) => new
    {
        idProducto = vm.Id,
        codigo = vm.Codigo,
        nombre = vm.Nombre,
        descripcion = (string?)null,
        capacidadKg = vm.CapacidadKg,
        precio = vm.Precio,
        stock = vm.Stock,
        activo = vm.Activo
    };

    // ============================================================
    // Clientes y usuarios
    // ============================================================

    public async Task<List<ClienteVm>> ClientesAsync(string? busqueda = null, int cantidad = 200)
    {
        var url = $"api/clientes?cantidadPorPagina={cantidad}";
        if (!string.IsNullOrWhiteSpace(busqueda))
            url += $"&busqueda={Uri.EscapeDataString(busqueda)}";

        var pagina = await LeerAsync<PaginaApi<ClienteApi>>(url);
        return pagina?.Items.Select(MapCliente).ToList() ?? new List<ClienteVm>();
    }

    public async Task<ClienteVm?> ClienteAsync(int id)
    {
        var c = await LeerAsync<ClienteApi>($"api/clientes/{id}");
        return c is null ? null : MapCliente(c);
    }

    public Task<Resultado> CrearClienteAsync(ClienteVm vm) =>
        EnviarAsync(HttpMethod.Post, "api/clientes", CuerpoCliente(vm));

    public Task<Resultado> ActualizarClienteAsync(ClienteVm vm) =>
        EnviarAsync(HttpMethod.Put, $"api/clientes/{vm.Id}", CuerpoCliente(vm));

    public Task<Resultado> EliminarClienteAsync(int id) =>
        EnviarAsync(HttpMethod.Delete, $"api/clientes/{id}", null);

    private static object CuerpoCliente(ClienteVm vm) => new
    {
        idCliente = vm.Id,
        nombre = vm.Nombre,
        documento = string.IsNullOrWhiteSpace(vm.Documento) ? "sin-rut" : vm.Documento,
        telefono = vm.Telefono,
        correo = vm.Email,
        comuna = vm.Comuna,
        direccion = vm.Direccion,
        activo = true
    };

    public async Task<List<UsuarioVm>> UsuariosAsync()
    {
        var pagina = await LeerAsync<PaginaApi<UsuarioApi>>("api/reportes/usuarios?cantidadPorPagina=200");
        return pagina?.Items.Select(u => new UsuarioVm
        {
            Id = u.IdUsuario,
            Email = u.Email,
            Nombre = u.Nombre,
            Rol = u.Rol,
            Activo = u.Activo
        }).ToList() ?? new List<UsuarioVm>();
    }

    // ============================================================
    // Pedidos
    // ============================================================

    public async Task<List<PedidoVm>> PedidosAsync(int? idCliente = null, string? estado = null,
                                                   DateTime? desde = null, DateTime? hasta = null,
                                                   string? busqueda = null, int cantidad = 200)
    {
        var url = $"api/pedidos?cantidadPorPagina={cantidad}";
        if (idCliente is not null) url += $"&idCliente={idCliente}";
        if (!string.IsNullOrWhiteSpace(estado)) url += $"&estado={Uri.EscapeDataString(estado)}";
        if (desde is not null) url += $"&fechaInicial={desde:yyyy-MM-dd}";
        if (hasta is not null) url += $"&fechaFinal={hasta:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(busqueda)) url += $"&busqueda={Uri.EscapeDataString(busqueda)}";

        var pagina = await LeerAsync<PaginaApi<PedidoApi>>(url);
        return pagina?.Items.Select(MapPedido).ToList() ?? new List<PedidoVm>();
    }

    public async Task<PedidoVm?> PedidoAsync(int id)
    {
        var p = await LeerAsync<PedidoApi>($"api/pedidos/{id}");
        return p is null ? null : MapPedido(p);
    }

    /// <summary>
    /// Registra el pedido. El backend graba cabecera y detalle en una sola
    /// transaccion y resuelve los precios contra la base, ignorando los que
    /// pudiera enviar el navegador.
    /// </summary>
    public async Task<Resultado> CrearPedidoAsync(int idCliente, string metodoPago,
                                                  string nombre, string telefono, string comuna,
                                                  string direccion, string? referencia,
                                                  decimal despacho, IEnumerable<LineaVm> lineas)
    {
        var cuerpo = new
        {
            idCliente,
            metodoPago,
            nombreEntrega = nombre,
            telefonoEntrega = telefono,
            comunaEntrega = comuna,
            direccionEntrega = direccion,
            referenciaEntrega = referencia,
            despacho,
            lineas = lineas.Select(l => new { idProducto = l.IdProducto, cantidad = l.Cantidad })
        };

        return await EnviarAsync(HttpMethod.Post, "api/pedidos", cuerpo, leerId: true);
    }

    // ============================================================
    // Reportes
    // ============================================================

    public async Task<List<InventarioVm>> InventarioAsync()
    {
        var pagina = await LeerAsync<PaginaApi<InventarioApi>>("api/reportes/inventario?cantidadPorPagina=200");
        return pagina?.Items.Select(i => new InventarioVm
        {
            IdProducto = i.IdProducto,
            Codigo = i.Codigo,
            Nombre = i.Nombre,
            CapacidadKg = i.CapacidadKg,
            Precio = i.Precio,
            Stock = i.Stock,
            ValorStock = i.ValorStock,
            EstadoStock = i.EstadoStock,
            Vendidos = i.Vendidos
        }).ToList() ?? new List<InventarioVm>();
    }

    // ============================================================
    // Fontaneria
    // ============================================================

    private async Task<T?> LeerAsync<T>(string url)
    {
        try
        {
            var r = await _http.GetAsync(url);
            if (r.StatusCode == HttpStatusCode.NotFound) return default;

            r.EnsureSuccessStatusCode();
            return await r.Content.ReadFromJsonAsync<T>(Json);
        }
        catch (Exception ex)
        {
            // La pantalla no se cae: devuelve vacio y marca HuboError, para
            // que la vista pueda decir "no pudimos conectar" en vez de
            // "no hay datos".
            _log.LogError(ex, "Fallo GET {Url}", url);
            HuboError = true;
            return default;
        }
    }

    private async Task<Resultado> EnviarAsync(HttpMethod metodo, string url, object? cuerpo,
                                              bool leerId = false)
    {
        try
        {
            using var peticion = new HttpRequestMessage(metodo, url);
            if (cuerpo is not null) peticion.Content = JsonContent.Create(cuerpo);

            var r = await _http.SendAsync(peticion);
            var texto = await r.Content.ReadAsStringAsync();

            if (r.IsSuccessStatusCode)
            {
                var id = 0;
                if (leerId && !string.IsNullOrWhiteSpace(texto))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(texto);
                        if (doc.RootElement.TryGetProperty("idPedido", out var v)) id = v.GetInt32();
                    }
                    catch (JsonException) { /* la respuesta no traia id; no es un error */ }
                }
                return Resultado.Ok(id);
            }

            return Resultado.Error(MensajeDeError(texto));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fallo {Metodo} {Url}", metodo, url);
            return Resultado.Error("No se pudo conectar con el servicio. Intenta nuevamente.");
        }
    }

    /// <summary>
    /// La API devuelve {"mensaje": "..."} para reglas de negocio y el formato
    /// ProblemDetails para errores de validacion. Se extrae algo legible de
    /// cualquiera de los dos.
    /// </summary>
    private static string MensajeDeError(string cuerpo)
    {
        if (string.IsNullOrWhiteSpace(cuerpo))
            return "No se pudo completar la operación.";

        try
        {
            using var doc = JsonDocument.Parse(cuerpo);
            var raiz = doc.RootElement;

            if (raiz.TryGetProperty("mensaje", out var m) && m.GetString() is { } texto)
                return texto;

            if (raiz.TryGetProperty("errors", out var errores) &&
                errores.ValueKind == JsonValueKind.Object)
            {
                foreach (var campo in errores.EnumerateObject())
                    foreach (var e in campo.Value.EnumerateArray())
                        if (e.GetString() is { } primero) return primero;
            }

            if (raiz.TryGetProperty("title", out var t) && t.GetString() is { } titulo)
                return titulo;
        }
        catch (JsonException) { }

        return "No se pudo completar la operación.";
    }

    // ------------------------------------------------------------ Mapeo

    private static ProductoVm MapProducto(ProductoApi p) => new()
    {
        Id = p.IdProducto,
        Codigo = p.Codigo,
        Nombre = p.Nombre,
        CapacidadKg = p.CapacidadKg,
        Precio = p.Precio,
        Stock = p.Stock,
        Activo = p.Activo
    };

    private static ClienteVm MapCliente(ClienteApi c) => new()
    {
        Id = c.IdCliente,
        Nombre = c.Nombre,
        Documento = c.Documento,
        Email = c.Correo,
        Telefono = c.Telefono,
        Comuna = c.Comuna,
        Direccion = c.Direccion
    };

    private static PedidoVm MapPedido(PedidoApi p)
    {
        var vm = new PedidoVm
        {
            Id = p.IdPedido,
            IdCliente = p.IdCliente,
            Cliente = p.Cliente,
            Fecha = p.Fecha,
            Estado = p.Estado,
            Despacho = p.Despacho,
            MetodoPago = p.MetodoPago,
            Comuna = p.ComunaEntrega,
            Direccion = p.DireccionEntrega,
            Telefono = p.TelefonoEntrega,
            Subtotal = p.Subtotal,
            Total = p.Total,
            TotalItems = p.TotalItems
        };

        foreach (var d in p.Detalles)
        {
            vm.Lineas.Add(new LineaVm
            {
                IdProducto = d.IdProducto,
                Nombre = d.Producto,
                CapacidadKg = d.CapacidadKg,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario
            });
        }

        return vm;
    }

    // ------------------------------------------------------------ Formas del JSON

    private class PaginaApi<T> { public List<T> Items { get; set; } = new(); }

    private class ProductoApi
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int CapacidadKg { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public bool Activo { get; set; }
    }

    private class ClienteApi
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; } = "";
        public string Documento { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string Correo { get; set; } = "";
        public string Comuna { get; set; } = "";
        public string Direccion { get; set; } = "";
    }

    private class UsuarioApi
    {
        public int IdUsuario { get; set; }
        public string Email { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Rol { get; set; } = "";
        public bool Activo { get; set; }
    }

    private class PedidoApi
    {
        public int IdPedido { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal Despacho { get; set; }
        public decimal Total { get; set; }
        public int TotalItems { get; set; }
        public string MetodoPago { get; set; } = "";
        public string ComunaEntrega { get; set; } = "";
        public string DireccionEntrega { get; set; } = "";
        public string TelefonoEntrega { get; set; } = "";
        public List<LineaApi> Detalles { get; set; } = new();
    }

    private class LineaApi
    {
        public int IdProducto { get; set; }
        public string Producto { get; set; } = "";
        public int CapacidadKg { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    private class InventarioApi
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int CapacidadKg { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public decimal ValorStock { get; set; }
        public string EstadoStock { get; set; } = "";
        public int Vendidos { get; set; }
    }
}

/// <summary>Resultado de una operacion de escritura.</summary>
public class Resultado
{
    public bool Exito { get; init; }
    public string Mensaje { get; init; } = "";
    public int Id { get; init; }

    public static Resultado Ok(int id = 0) => new() { Exito = true, Id = id };
    public static Resultado Error(string mensaje) => new() { Exito = false, Mensaje = mensaje };
}
