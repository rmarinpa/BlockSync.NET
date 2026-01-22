using BlockSync.Domain.Entities;
using BlockSync.Domain.ValueObjects;

namespace BlockSync.Domain.Interfaces;

/// <summary>
/// Contrato para el sistema de destino (destination) donde se sincronizan los datos.
/// Típicamente representa un sistema local o destino de replicación.
/// </summary>
public interface ISyncDestination
{
    /// <summary>
    /// Obtiene todos los headers de bloques disponibles en el destino.
    /// Permite comparar con el origen para detectar diferencias.
    /// </summary>
    /// <returns>Lista de headers con metadata de cada bloque</returns>
    Task<List<BlockHeader>> GetBlockHeadersAsync();

    /// <summary>
    /// Inserta un bloque completo de ventas en el destino.
    /// Se usa para sincronización inicial o cuando falta un bloque.
    /// </summary>
    /// <param name="ventas">Lista de ventas a insertar</param>
    Task InsertBlockAsync(List<Venta> ventas);

    /// <summary>
    /// Elimina todos los registros de un periodo específico.
    /// Se usa antes de reparar un bloque corrupto.
    /// </summary>
    /// <param name="periodo">Periodo en formato "yyyy-MM" a eliminar</param>
    Task DeleteBlockAsync(string periodo);

    /// <summary>
    /// Obtiene estadísticas generales del sistema destino
    /// </summary>
    /// <returns>Tupla con (totalRegistros, totalBloques, listaPeriodos)</returns>
    Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync();

    /// <summary>
    /// Limpia completamente todos los datos del destino
    /// </summary>
    Task ClearAllAsync();

    /// <summary>
    /// Corrompe intencionalmente los datos de un periodo para testing.
    /// Modifica los montos para que el hash cambie.
    /// </summary>
    /// <param name="periodo">Periodo a corromper en formato "yyyy-MM"</param>
    Task CorruptBlockAsync(string periodo);

    /// <summary>
    /// Obtiene todos los registros de un periodo específico
    /// </summary>
    /// <param name="periodo">Periodo en formato "yyyy-MM"</param>
    Task<List<Venta>> GetBlockDataAsync(string periodo);
}
