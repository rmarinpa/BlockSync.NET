using BlockSync.Application.DTOs;

namespace BlockSync.Application.Interfaces;

/// <summary>
/// Contrato del motor de sincronización principal.
/// Coordina la comparación y sincronización de bloques entre origen y destino.
/// </summary>
public interface ISyncEngine
{
    /// <summary>
    /// Ejecuta el proceso completo de sincronización
    /// </summary>
    Task<SyncReport> SynchronizeAsync();

    /// <summary>
    /// Obtiene el estado actual del sistema (origen y destino)
    /// </summary>
    Task<SystemStatusResponse> GetSystemStatusAsync();

    /// <summary>
    /// Obtiene la comparación detallada de hashes entre origen y destino
    /// </summary>
    Task<HashComparisonResponse> GetHashComparisonAsync();

    /// <summary>
    /// Corrompe datos de un periodo para testing
    /// </summary>
    Task CorruptDataAsync(int anio, int mes);

    /// <summary>
    /// Reinicia el sistema completo
    /// </summary>
    Task ResetSystemAsync();
}
