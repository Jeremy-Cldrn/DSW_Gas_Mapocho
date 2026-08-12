using GasMapocho.Api.Models;

namespace GasMapocho.Api.Interfaces;

public interface IProductoRepositorio
{
    PaginaDeProductos Listar(string? busqueda, int pagina, int cantidadPorPagina);
    Producto? Obtener(int idProducto);
    int Registrar(Producto producto);
    void Actualizar(Producto producto);
    void Eliminar(int idProducto);
}
