using GasMapocho.Api;
using GasMapocho.Api.Interfaces;
using GasMapocho.Api.Repositories;
using GasMapocho.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddGrpc();

// Inyeccion de dependencias, igual que en la aplicacion MVC.
builder.Services.AddScoped<ConexionBD>();
builder.Services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IReporteRepositorio, ReporteRepositorio>();

// ------------------------------------------------------------
// CORS: hace falta porque el catalogo de la aplicacion MVC llama a
// esta API desde el navegador, y son origenes distintos.
// Se nombran los origenes permitidos uno por uno; AllowAnyOrigin
// abriria la API a cualquier sitio de Internet.
// ------------------------------------------------------------
var origenesPermitidos = builder.Configuration
    .GetSection("Cors:Origenes").Get<string[]>()
    ?? new[] { "http://localhost:5003" };

builder.Services.AddCors(o => o.AddPolicy("Frontends", p => p
    .WithOrigins(origenesPermitidos)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors("Frontends");

app.MapControllers();
app.MapGrpcService<ProductoGrpcService>();

// Mensaje util al abrir la raiz en el navegador: gRPC no se puede consultar
// desde ahi, y sin esto la respuesta seria un 404 desconcertante.
app.MapGet("/", () => Results.Ok(new
{
    servicio = "GasMapocho.Api",
    rest = new[]
    {
        "POST /api/auth/login",
        "GET/POST/PUT/DELETE /api/productos",
        "GET/POST/PUT/DELETE /api/clientes",
        "GET/POST /api/pedidos  ·  POST /api/pedidos/{id}/aprobar  ·  /rechazar",
        "GET /api/reportes/inventario  ·  GET /api/reportes/usuarios"
    },
    grpc = "ProductoGrpc.ConsultarProducto (requiere un cliente gRPC)"
}));

app.Run();
