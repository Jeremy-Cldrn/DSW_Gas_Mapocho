using System.Globalization;
using GasMapocho.Ui.Services;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Sesion propia del panel: iniciar sesion en la tienda no debe dejar sesion
// abierta aqui, y por eso la cookie tiene otro nombre.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".GasAdmin.Sesion";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromHours(2);
});

// El panel tampoco conoce SQL Server: todo pasa por GasMapocho.Api.
builder.Services.AddHttpClient<ApiGasMapocho>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5000/");
    c.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Ventas/Error");
    app.UseHsts();
}

// Misma cultura chilena que la Tienda: los montos deben verse igual en ambas.
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
    pattern: "{controller=Ventas}/{action=Index}/{id?}");

app.Run();
