namespace BlockSync.Domain.Entities;

/// <summary>
/// Entrada en el ledger de sincronización (Blockchain-style).
/// Registra el estado de cada periodo sincronizado para auditoría y trazabilidad.
/// </summary>
public class SyncLedgerEntry
{
    /// <summary>
    /// Identificador del periodo (formato yyyy-MM)
    /// </summary>
    public string PeriodoId { get; set; } = string.Empty;

    /// <summary>
    /// Hash MD5 del bloque sincronizado
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Estado actual del periodo
    /// </summary>
    public SyncEstado Estado { get; set; }

    /// <summary>
    /// Timestamp de la última sincronización
    /// </summary>
    public DateTime UltimaSync { get; set; }

    /// <summary>
    /// Total de registros en el bloque
    /// </summary>
    public int TotalRegistros { get; set; }

    /// <summary>
    /// Suma de montos en centavos
    /// </summary>
    public long SumaMontoCentavos { get; set; }

    /// <summary>
    /// Última acción realizada en este periodo
    /// </summary>
    public SyncAccion UltimaAccion { get; set; }

    /// <summary>
    /// Timestamp de creación del registro
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp de última actualización
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Suma de montos como decimal (calculado)
    /// </summary>
    public decimal SumaMonto => SumaMontoCentavos / 100m;
}

/// <summary>
/// Estados posibles de un periodo en el ledger
/// </summary>
public enum SyncEstado
{
    SINCRONIZADO,
    CORRUPTO,
    PENDIENTE,
    ERROR
}

/// <summary>
/// Acciones de sincronización
/// </summary>
public enum SyncAccion
{
    SKIP,
    INSERT,
    REPAIR
}
