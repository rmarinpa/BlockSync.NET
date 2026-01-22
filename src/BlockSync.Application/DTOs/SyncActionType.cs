namespace BlockSync.Application.DTOs;

/// <summary>
/// Tipo de acción que se ejecuta para un bloque durante la sincronización.
/// Inspirado en el comportamiento de blockchain para validación de bloques.
/// </summary>
public enum SyncActionType
{
    /// <summary>
    /// SKIP: Los hashes coinciden, no hay cambios. Se omite la descarga.
    /// Similar a validación de bloque en blockchain cuando el hash es correcto.
    /// </summary>
    SKIP = 0,

    /// <summary>
    /// INSERT: El bloque no existe en el destino. Se descarga e inserta completo.
    /// Primera sincronización o bloques nuevos.
    /// </summary>
    INSERT = 1,

    /// <summary>
    /// REPAIR: Los hashes difieren, datos corruptos detectados.
    /// Se elimina el bloque corrupto y se re-descarga desde el origen.
    /// Similar a re-sync de bloque inválido en blockchain.
    /// </summary>
    REPAIR = 2
}
