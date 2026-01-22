namespace BlockSync.Domain.Entities;

/// <summary>
/// Entidad que representa una venta del sistema legacy.
/// Esta es la entidad principal del dominio que se sincroniza entre sistemas.
/// </summary>
public class Venta
{
    /// <summary>
    /// Identificador único de la venta
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Fecha en que se realizó la venta
    /// </summary>
    public DateTime FechaVenta { get; set; }

    /// <summary>
    /// Nombre del cliente que realizó la compra
    /// </summary>
    public string Cliente { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del producto vendido
    /// </summary>
    public string Producto { get; set; } = string.Empty;

    /// <summary>
    /// Monto total de la venta
    /// </summary>
    public decimal Monto { get; set; }

    /// <summary>
    /// Periodo al que pertenece esta venta en formato "yyyy-MM"
    /// Este campo se usa para agrupar ventas en bloques mensuales
    /// </summary>
    public string Periodo { get; set; } = string.Empty;
}
