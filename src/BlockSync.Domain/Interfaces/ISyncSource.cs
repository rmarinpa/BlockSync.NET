using BlockSync.Domain.Entities;
using BlockSync.Domain.ValueObjects;

namespace BlockSync.Domain.Interfaces;

/// <summary>
/// Contrato para el sistema de origen (source) desde donde se sincronizan los datos.
/// Típicamente representa un sistema legacy o fuente de verdad.
/// </summary>
public interface ISyncSource
{
    /// <summary>
    /// Obtiene todos los headers de bloques disponibles en el origen.
    /// Esto permite comparación rápida sin descargar datos completos.
    /// </summary>
    /// <returns>Lista de headers con metadata de cada bloque</returns>
    Task<List<BlockHeader>> GetBlockHeadersAsync();

    /// <summary>
    /// Descarga todos los registros de ventas de un periodo específico.
    /// Este método se llama solo cuando se necesita insertar o reparar un bloque.
    /// </summary>
    /// <param name="periodo">Periodo en formato "yyyy-MM" (ej: "2023-03")</param>
    /// <returns>Lista de ventas del periodo solicitado</returns>
    Task<List<Venta>> GetBlockDataAsync(string periodo);

    /// <summary>
    /// Obtiene estadísticas generales del sistema origen
    /// </summary>
    /// <returns>Tupla con (totalRegistros, totalBloques, listaPeriodos)</returns>
    Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync();

    /// <summary>
    /// Reinicia el origen a su estado inicial
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Obtiene TODOS los registros (para diagnósticos)
    /// </summary>
    Task<List<Venta>> GetAllDataAsync();
}
