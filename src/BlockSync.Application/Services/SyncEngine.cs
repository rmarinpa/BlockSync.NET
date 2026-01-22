using System.Diagnostics;
using BlockSync.Application.DTOs;
using BlockSync.Application.Interfaces;
using BlockSync.Domain.Interfaces;
using BlockSync.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace BlockSync.Application.Services;

/// <summary>
/// Motor principal de sincronización.
/// Implementa el algoritmo blockchain-inspired para sincronización inteligente de datos.
/// </summary>
public class SyncEngine : ISyncEngine
{
    private readonly ISyncSource _source;
    private readonly ISyncDestination _destination;
    private readonly ILogger<SyncEngine> _logger;

    public SyncEngine(
        ISyncSource source,
        ISyncDestination destination,
        ILogger<SyncEngine> logger)
    {
        _source = source;
        _destination = destination;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el proceso completo de sincronización usando comparación de hashes.
    /// Algoritmo:
    /// 1. Obtener headers de origen y destino
    /// 2. Para cada periodo en origen:
    ///    - Si no existe en destino → INSERT
    ///    - Si hashes coinciden → SKIP
    ///    - Si hashes difieren → REPAIR
    /// </summary>
    public async Task<SyncReport> SynchronizeAsync()
    {
        _logger.LogInformation("🚀 Iniciando proceso de sincronización...");
        var stopwatch = Stopwatch.StartNew();

        var report = new SyncReport
        {
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // 1. Obtener headers de ambos sistemas
            _logger.LogInformation("📥 Obteniendo headers de bloques...");
            var sourceHeaders = await _source.GetBlockHeadersAsync();
            var destinationHeaders = await _destination.GetBlockHeadersAsync();

            _logger.LogInformation($"   Origen: {sourceHeaders.Count} bloques");
            _logger.LogInformation($"   Destino: {destinationHeaders.Count} bloques");

            // Crear diccionario para búsqueda rápida
            var destHeadersDict = destinationHeaders.ToDictionary(h => h.Periodo);

            report.TotalBloques = sourceHeaders.Count;

            // 2. Procesar cada bloque del origen
            foreach (var sourceHeader in sourceHeaders.OrderBy(h => h.Periodo))
            {
                var blockStopwatch = Stopwatch.StartNew();
                var blockResult = new BlockSyncResult
                {
                    Periodo = sourceHeader.Periodo,
                    HashOrigen = sourceHeader.Hash
                };

                try
                {
                    // Verificar si existe en destino
                    if (!destHeadersDict.TryGetValue(sourceHeader.Periodo, out var destHeader))
                    {
                        // CASO 1: INSERT - No existe en destino
                        _logger.LogInformation($"➕ {sourceHeader.Periodo} - INSERT (no existe en destino)");
                        blockResult.Accion = SyncActionType.INSERT;
                        blockResult.HashDestinoAntes = null;

                        // Descargar datos completos del origen
                        var data = await _source.GetBlockDataAsync(sourceHeader.Periodo);
                        blockResult.RegistrosProcesados = data.Count;

                        // Insertar en destino
                        await _destination.InsertBlockAsync(data);
                        blockResult.RegistrosInsertados = data.Count;
                        blockResult.Mensaje = $"Bloque insertado con {data.Count} registros";

                        report.BloquesInsertados++;
                        report.TotalRegistrosInsertados += data.Count;
                    }
                    else if (sourceHeader.Hash == destHeader.Hash)
                    {
                        // CASO 2: SKIP - Hashes coinciden
                        _logger.LogInformation($"⏭️  {sourceHeader.Periodo} - SKIP (hashes coinciden)");
                        blockResult.Accion = SyncActionType.SKIP;
                        blockResult.HashDestinoAntes = destHeader.Hash;
                        blockResult.RegistrosProcesados = sourceHeader.TotalRegistros;
                        blockResult.RegistrosInsertados = 0;
                        blockResult.Mensaje = "Sin cambios detectados";

                        report.BloquesOmitidos++;
                    }
                    else
                    {
                        // CASO 3: REPAIR - Hashes difieren (corrupción detectada)
                        _logger.LogWarning($"🔧 {sourceHeader.Periodo} - REPAIR (corrupción detectada!)");
                        _logger.LogWarning($"   Hash Origen:  {sourceHeader.Hash}");
                        _logger.LogWarning($"   Hash Destino: {destHeader.Hash}");

                        blockResult.Accion = SyncActionType.REPAIR;
                        blockResult.HashDestinoAntes = destHeader.Hash;

                        // Eliminar bloque corrupto
                        await _destination.DeleteBlockAsync(sourceHeader.Periodo);

                        // Descargar datos correctos del origen
                        var data = await _source.GetBlockDataAsync(sourceHeader.Periodo);
                        blockResult.RegistrosProcesados = data.Count;

                        // Re-insertar datos correctos
                        await _destination.InsertBlockAsync(data);
                        blockResult.RegistrosInsertados = data.Count;
                        blockResult.Mensaje = $"Bloque reparado: {data.Count} registros re-sincronizados";

                        report.BloquesReparados++;
                        report.TotalRegistrosInsertados += data.Count;
                    }

                    report.TotalRegistrosProcesados += blockResult.RegistrosProcesados;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error procesando bloque {sourceHeader.Periodo}");
                    blockResult.Mensaje = $"Error: {ex.Message}";
                }

                blockStopwatch.Stop();
                blockResult.DuracionMs = blockStopwatch.ElapsedMilliseconds;
                report.DetallesBloques.Add(blockResult);
            }

            stopwatch.Stop();
            report.DuracionMs = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("✅ Sincronización completada!");
            _logger.LogInformation($"   Duración: {report.DuracionMs} ms");
            _logger.LogInformation($"   Bloques Insertados: {report.BloquesInsertados}");
            _logger.LogInformation($"   Bloques Reparados: {report.BloquesReparados}");
            _logger.LogInformation($"   Bloques Omitidos: {report.BloquesOmitidos}");

            // Imprimir reporte ASCII
            Console.WriteLine();
            Console.WriteLine(report.GenerarResumenTexto());
            Console.WriteLine();

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error fatal durante la sincronización");
            stopwatch.Stop();
            report.DuracionMs = stopwatch.ElapsedMilliseconds;
            throw;
        }
    }

    /// <summary>
    /// Obtiene el estado actual del sistema
    /// </summary>
    public async Task<SystemStatusResponse> GetSystemStatusAsync()
    {
        var (sourceTotal, sourceBloques, sourcePeriodos) = await _source.GetStatsAsync();
        var (destTotal, destBloques, destPeriodos) = await _destination.GetStatsAsync();

        // Determinar estado de sincronización
        var estado = "DESINCRONIZADO";
        if (destTotal == 0)
        {
            estado = "SIN_SINCRONIZAR";
        }
        else if (sourceBloques == destBloques)
        {
            // Verificar si los hashes coinciden
            var sourceHeaders = await _source.GetBlockHeadersAsync();
            var destHeaders = await _destination.GetBlockHeadersAsync();
            var destHeadersDict = destHeaders.ToDictionary(h => h.Periodo);

            var todosSincronizados = sourceHeaders.All(sh =>
                destHeadersDict.TryGetValue(sh.Periodo, out var dh) && sh.Hash == dh.Hash);

            estado = todosSincronizados ? "SINCRONIZADO" : "PARCIALMENTE_SINCRONIZADO";
        }

        return new SystemStatusResponse
        {
            Timestamp = DateTime.UtcNow,
            Sistema = "BlockSync.NET",
            Version = "1.0.0",
            Origen = new SystemInfo
            {
                Registros = sourceTotal,
                Bloques = sourceBloques,
                Periodos = sourcePeriodos
            },
            Destino = new SystemInfo
            {
                Registros = destTotal,
                Bloques = destBloques,
                Periodos = destPeriodos
            },
            EstadoSincronizacion = estado
        };
    }

    /// <summary>
    /// Obtiene la comparación detallada de hashes
    /// </summary>
    public async Task<HashComparisonResponse> GetHashComparisonAsync()
    {
        var sourceHeaders = await _source.GetBlockHeadersAsync();
        var destHeaders = await _destination.GetBlockHeadersAsync();
        var destHeadersDict = destHeaders.ToDictionary(h => h.Periodo);

        var comparisons = new List<BlockComparison>();
        int sincronizados = 0;
        int faltanEnDestino = 0;
        int corruptos = 0;

        foreach (var sourceHeader in sourceHeaders.OrderBy(h => h.Periodo))
        {
            var comparison = new BlockComparison
            {
                Periodo = sourceHeader.Periodo,
                Origen = new BlockInfo
                {
                    Hash = sourceHeader.Hash,
                    Registros = sourceHeader.TotalRegistros,
                    Monto = sourceHeader.SumaMonto
                }
            };

            if (!destHeadersDict.TryGetValue(sourceHeader.Periodo, out var destHeader))
            {
                comparison.Estado = "FALTA_EN_DESTINO";
                comparison.Destino = null;
                faltanEnDestino++;
            }
            else if (sourceHeader.Hash == destHeader.Hash)
            {
                comparison.Estado = "SINCRONIZADO";
                comparison.Destino = new BlockInfo
                {
                    Hash = destHeader.Hash,
                    Registros = destHeader.TotalRegistros,
                    Monto = destHeader.SumaMonto
                };
                sincronizados++;
            }
            else
            {
                comparison.Estado = "CORRUPTO";
                comparison.Destino = new BlockInfo
                {
                    Hash = destHeader.Hash,
                    Registros = destHeader.TotalRegistros,
                    Monto = destHeader.SumaMonto
                };
                corruptos++;
            }

            comparisons.Add(comparison);
        }

        return new HashComparisonResponse
        {
            Timestamp = DateTime.UtcNow,
            TotalPeriodos = sourceHeaders.Count,
            Sincronizados = sincronizados,
            FaltanEnDestino = faltanEnDestino,
            Corruptos = corruptos,
            Bloques = comparisons
        };
    }

    /// <summary>
    /// Corrompe datos de un periodo específico para testing
    /// </summary>
    public async Task CorruptDataAsync(int anio, int mes)
    {
        var periodo = $"{anio:D4}-{mes:D2}";
        _logger.LogWarning($"⚠️  Corrompiendo datos del periodo {periodo}...");
        await _destination.CorruptBlockAsync(periodo);
        _logger.LogWarning($"✅ Periodo {periodo} corrompido exitosamente");
    }

    /// <summary>
    /// Reinicia el sistema completo
    /// </summary>
    public async Task ResetSystemAsync()
    {
        _logger.LogInformation("🔄 Reiniciando sistema completo...");
        await _destination.ClearAllAsync();
        await _source.ResetAsync();
        _logger.LogInformation("✅ Sistema reiniciado exitosamente");
    }
}
