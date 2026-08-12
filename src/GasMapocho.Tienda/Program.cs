using System.Globalization;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// El carrito y la sesion del cliente viven en el servidor.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".GasTienda.Sesion";
    o.Cookie.HttpOnly = true;   // ningun script del navegador puede leerla
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromHours(2);
});

// Unico canal de datos: la Tienda no conoce SQL Server ni su cadena de
// conexion. Todo lo que muestra se lo pide a GasMapocho.Api.
builder.Services.AddHttpClient<ApiGasMapocho>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000/");
    c.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Catalogo/Error");
    app.UseHsts();
}

// Cultura chilena: el peso no tiene decimales y el punto separa miles.
var culturaCL = new CultureInfo("es-CL");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaCL),
    SupportedCultures = new[] { culturaCL },
    SupportedUICultures = new[] { culturaCL }
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalogo}/{action=Index}/{id?}");

app.Run();
