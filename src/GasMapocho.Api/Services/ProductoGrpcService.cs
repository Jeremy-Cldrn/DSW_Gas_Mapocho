using GasMapocho.Api.Grpc;
using GasMapocho.Api.Interfaces;
using Grpc.Core;

namespace GasMapocho.Api.Services;

/// <summary>
/// Implementacion del servicio gRPC. Reutiliza el mismo repositorio ADO.NET
/// que usa la Web API: el flujo completo es
///
///     ClienteGrpc -> ProductoGrpcService -> ProductoRepositorio -> SQL Server
/// </summary>
public class ProductoGrpcService : ProductoGrpc.ProductoGrpcBase
{
    private readonly IProductoRepositorio _productos;
    private readonly ILogger<ProductoGrpcService> _log;

    public ProductoGrpcService(IProductoRepositorio productos, ILogger<ProductoGrpcService> log)
    {
        _productos = productos;
        _log = log;
    }

    public override Task<ProductoReply> ConsultarProducto(ProductoRequest request, ServerCallContext context)
    {
        _log.LogInformation("gRPC ConsultarProducto: {Id}", request.IdProducto);

        var producto = _productos.Obtener(request.IdProducto);

        if (producto is null)
        {
            // Se responde con encontrado = false en vez de lanzar: para el
            // cliente "no existe" es una respuesta valida, no un fallo.
            return Task.FromResult(new ProductoReply { Encontrado = false });
        }

        return Task.FromResult(new ProductoReply
        {
            IdProducto = producto.IdProducto,
            Codigo     = producto.Codigo,
            Nombre     = producto.Nombre,
            Precio     = (double)producto.Precio,
            Stock      = producto.Stock,
            Encontrado = true
        });
    }
}
