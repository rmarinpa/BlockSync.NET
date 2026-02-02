namespace BlockSync.Domain.Entities;

/// <summary>
/// Entidad que representa una adjudicación (award) de ChileCompra en formato OCDS.
/// Un Award representa la decisión de adjudicar un contrato a un proveedor específico.
/// </summary>
public class Award
{
    /// <summary>
    /// Identificador único del award (compuesto: OCID + AwardId)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Open Contracting ID - Identificador único del proceso de contratación completo
    /// Ejemplo: "ocds-70d2nz-3654-7-L120"
    /// </summary>
    public string Ocid { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del award dentro del proceso
    /// Ejemplo: "1"
    /// </summary>
    public string AwardId { get; set; } = string.Empty;

    /// <summary>
    /// Título descriptivo del award/contrato
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Estado actual del award: active, pending, cancelled, etc.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Fecha en que se realizó la adjudicación
    /// </summary>
    public DateTime AwardDate { get; set; }

    /// <summary>
    /// Monto adjudicado en la moneda especificada
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Código de moneda ISO 4217 (típicamente "CLP" para pesos chilenos)
    /// </summary>
    public string Currency { get; set; } = "CLP";

    /// <summary>
    /// RUT del proveedor ganador
    /// Ejemplo: "76.123.456-7"
    /// </summary>
    public string SupplierRut { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del proveedor ganador
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la entidad compradora
    /// </summary>
    public string BuyerName { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de publicación del release OCDS
    /// </summary>
    public DateTime PublishedDate { get; set; }

    /// <summary>
    /// Periodo al que pertenece esta adjudicación en formato "yyyy-MM"
    /// Se usa para agrupar awards en bloques mensuales para sincronización
    /// </summary>
    public string Periodo { get; set; } = string.Empty;
}
