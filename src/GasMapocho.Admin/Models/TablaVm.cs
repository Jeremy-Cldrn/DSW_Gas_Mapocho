namespace GasMapocho.Admin.Models;

// ============================================================
// Plantilla genérica de listado.
// La escriben una vez y la usan los 3 mantenimientos (productos,
// clientes, usuarios) y los 2 reportes (ventas, inventario).
// Son la misma tabla con distinto contenido: programarlas por
// separado sextuplicaría el trabajo.
// ============================================================

public class ColumnaVm
{
    public string Titulo { get; set; } = "";
    /// <summary>Alinea a la derecha con cifras tabulares (montos y cantidades).</summary>
    public bool Numerica { get; set; }
    public string? Ancho { get; set; }

    public ColumnaVm() { }
    public ColumnaVm(string titulo, bool numerica = false, string? ancho = null)
    {
        Titulo = titulo; Numerica = numerica; Ancho = ancho;
    }
}

public class CeldaVm
{
    public string Texto { get; set; } = "";
    public bool Numerica { get; set; }
    public bool Fuerte { get; set; }
    /// <summary>Si tiene valor, se renderiza como badge de estado en vez de texto.</summary>
    public string? Estado { get; set; }
    public string? Secundario { get; set; }

    public CeldaVm() { }
    public CeldaVm(string texto, bool numerica = false, bool fuerte = false, string? secundario = null)
    {
        Texto = texto; Numerica = numerica; Fuerte = fuerte; Secundario = secundario;
    }

    public static CeldaVm Badge(string estado) => new() { Estado = estado };
}

public class FilaVm
{
    public int Id { get; set; }
    public List<CeldaVm> Celdas { get; set; } = new();

    /// <summary>
    /// Modelo del panel desplegable. Si es null, la fila no se despliega.
    /// Se renderiza con la vista parcial indicada en <see cref="TablaVm.DetalleVista"/>.
    /// Es un modelo tipado y NO una cadena de HTML: los datos del pedido vienen
    /// del usuario y con Html.Raw se abriría una vía de XSS (RNF-05).
    /// </summary>
    public object? DetalleModel { get; set; }
}

public class TablaVm
{
    public string Titulo { get; set; } = "";
    public string Subtitulo { get; set; } = "";
    public string PlaceholderBuscar { get; set; } = "Buscar";
    public string? TextoNuevo { get; set; }
    public string? UrlNuevo { get; set; }
    public int PorPagina { get; set; } = 5;
    public bool ConAcciones { get; set; } = true;

    /// <summary>Termino actual de busqueda, para que el input lo conserve tras recargar.</summary>
    public string? Busqueda { get; set; }

    /// <summary>
    /// Si es true, el buscador navega al servidor (la API filtra con SQL) en vez
    /// de filtrar solo las filas ya cargadas en el DOM. Se activa en las
    /// pantallas cuya API ya soporta @Busqueda; sin esto, el comportamiento es
    /// el filtrado local de siempre.
    /// </summary>
    public bool BuscarEnServidor { get; set; }

    /// <summary>
    /// Si es true, el detalle de la fila se abre en un modal (con su propia X
    /// para cerrar) en vez de como panel desplegable dentro de la tabla.
    /// </summary>
    public bool DetalleModal { get; set; }

    public List<ColumnaVm> Columnas { get; set; } = new();
    public List<FilaVm> Filas { get; set; } = new();

    /// <summary>Nombre de la vista parcial que renderiza el detalle (desplegable o modal).</summary>
    public string? DetalleVista { get; set; }

    public GasMapocho.Ui.Models.EstadoVacioVm Vacio { get; set; } =
        new("inbox", "No hay registros", "Todavía no hay nada que mostrar aquí.");

    /// <summary>URL base informativa del listado.</summary>
    public string UrlBase { get; set; } = "";

    /// <summary>
    /// Plantilla del enlace de edición, con {0} en el lugar del id.
    /// Ejemplo: "/Mantenimiento/FormProducto?id={0}". Si es null, no se
    /// dibuja el botón de editar.
    /// </summary>
    public string? UrlEditar { get; set; }

    /// <summary>
    /// Acción que da de baja el registro. Se envía por POST con token
    /// antiforgery: una baja nunca puede colgar de un enlace GET, porque
    /// bastaría con que alguien lo cargara para ejecutarla.
    /// </summary>
    public string? AccionEliminar { get; set; }

    /// <summary>Texto de la confirmación previa a la baja.</summary>
    public string ConfirmacionEliminar { get; set; } =
        "¿Dar de baja este registro?";
}
