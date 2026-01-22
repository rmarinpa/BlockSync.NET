namespace BlockSync.Application.DTOs;

/// <summary>
/// Resultado de sincronización de un bloque individual.
/// Contiene detalles de qué acción se tomó y el resultado.
/// </summary>
public class BlockSyncResult
{
    /// <summary>
    /// Periodo del bloque sincronizado (ej: "2023-03")
    /// </summary>
    public string Periodo { get; set; } = string.Empty;

    /// <summary>
    /// Acción ejecutada: SKIP, INSERT o REPAIR
    /// </summary>
    public SyncActionType Accion { get; set; }

    /// <summary>
    /// Hash del origen para este bloque
    /// </summary>
    public string HashOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Hash del destino antes de la sincronización (puede ser vacío si no existía)
    /// </summary>
    public string? HashDestinoAntes { get; set; }

    /// <summary>
    /// Cantidad de registros procesados en este bloque
    /// </summary>
    public int RegistrosProcesados { get; set; }

    /// <summary>
    /// Cantidad de registros insertados (0 si fue SKIP)
    /// </summary>
    public int RegistrosInsertados { get; set; }

    /// <summary>
    /// Mensaje descriptivo de lo que ocurrió
    /// </summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>
    /// Duración en milisegundos del procesamiento de este bloque
    /// </summary>
    public long DuracionMs { get; set; }
}
