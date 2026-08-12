using System.ComponentModel.DataAnnotations;

namespace GasMapocho.Api.Models;

public class Producto
{
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(20)]
    public string Codigo { get; set; } = "";

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    public string Nombre { get; set; } = "";

    [StringLength(300)]
    public string? Descripcion { get; set; }

    [Range(0, 100)]
    public int CapacidadKg { get; set; }

    [Range(1, 99999999, ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }

    [Range(0, 999999, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>Envoltorio de un listado paginado devuelto como JSON.</summary>
public class PaginaDeProductos
{
    public List<Producto> Items { get; set; } = new();
    public int Pagina { get; set; }
    public int CantidadPorPagina { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas =>
        TotalRegistros == 0 ? 1 : (int)Math.Ceiling(TotalRegistros / (double)CantidadPorPagina);
}
