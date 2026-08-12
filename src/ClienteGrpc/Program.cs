using ClienteGrpc.Grpc;
using Grpc.Net.Client;

// ============================================================
// Cliente de prueba del servicio gRPC de GasMapocho.Api.
//
//   dotnet run --project src\ClienteGrpc            consulta los ids 1..6
//   dotnet run --project src\ClienteGrpc -- 3       consulta solo el id 3
//
// Requiere que GasMapocho.Api este corriendo.
// ============================================================

var direccion = Environment.GetEnvironmentVariable("GRPC_URL") ?? "http://localhost:5005";

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Sin esto los montos saldrian con formato de otra cultura (14,500 en vez de
// 14.500), que es justo al reves de como se escribe un precio en Chile.
System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("es-CL");

Console.WriteLine($"Conectando al servicio gRPC en {direccion}…");
Console.WriteLine();

// El servidor expone gRPC sobre HTTP/2 sin TLS (h2c), que es lo razonable en
// un entorno local. Con https habria que confiar el certificado de desarrollo.
using var canal = GrpcChannel.ForAddress(direccion);
var cliente = new ProductoGrpc.ProductoGrpcClient(canal);

// Ids a consultar: los de la linea de comandos, o 1..6 por omision.
var ids = args.Length > 0
    ? args.Select(a => int.TryParse(a, out var n) ? n : 0).Where(n => n > 0).ToArray()
    : Enumerable.Range(1, 6).ToArray();

try
{
    Console.WriteLine($"{"Id",-4} {"Código",-10} {"Producto",-26} {"Precio",12} {"Stock",7}");
    Console.WriteLine(new string('-', 64));

    foreach (var id in ids)
    {
        var respuesta = await cliente.ConsultarProductoAsync(new ProductoRequest { IdProducto = id });

        if (!respuesta.Encontrado)
        {
            Console.WriteLine($"{id,-4} (no existe)");
            continue;
        }

        Console.WriteLine($"{respuesta.IdProducto,-4} {respuesta.Codigo,-10} {respuesta.Nombre,-26} " +
                          $"{respuesta.Precio,12:N0} {respuesta.Stock,7}");
    }

    Console.WriteLine();
    Console.WriteLine("Consulta completada.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("No se pudo consultar el servicio gRPC.");
    Console.Error.WriteLine($"  {ex.Message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Verifica que GasMapocho.Api esté corriendo:");
    Console.Error.WriteLine(@"  dotnet run --project src\GasMapocho.Api");
    return 1;
}
