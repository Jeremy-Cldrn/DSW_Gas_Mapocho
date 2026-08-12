namespace GasMapocho.Ui.Models;

/// <summary>
/// Un listado sin resultados nunca muestra una tabla vacía: muestra un icono,
/// una frase que explica qué pasa y un botón que lleva a la acción que corresponde.
/// </summary>
public class EstadoVacioVm
{
    public string Icono { get; set; } = "inbox";
    public string Titulo { get; set; } = "No hay nada por aquí";
    public string Mensaje { get; set; } = "";
    public string? TextoBoton { get; set; }
    public string? UrlBoton { get; set; }

    public EstadoVacioVm() { }

    public EstadoVacioVm(string icono, string titulo, string mensaje,
                         string? textoBoton = null, string? urlBoton = null)
    {
        Icono = icono;
        Titulo = titulo;
        Mensaje = mensaje;
        TextoBoton = textoBoton;
        UrlBoton = urlBoton;
    }
}
