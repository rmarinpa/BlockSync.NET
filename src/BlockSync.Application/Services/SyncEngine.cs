using System.Diagnostics;
using BlockSync.Application.DTOs;
using BlockSync.Application.Interfaces;
using BlockSync.Domain.Entities;
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

                        // Actualizar ledger con INSERT exitoso
                        await UpdateLedgerAsync(sourceHeader, SyncAccion.INSERT);
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

                        // Actualizar ledger con SKIP exitoso
                        await UpdateLedgerAsync(sourceHeader, SyncAccion.SKIP);
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

                        // Actualizar ledger con REPAIR exitoso
                        await UpdateLedgerAsync(sourceHeader, SyncAccion.REPAIR);
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

    /// <summary>
    /// Obtiene diagnóstico detallado del sistema para demostrar datos reales
    /// </summary>
    public async Task<DiagnosticsResponse> GetDiagnosticsAsync()
    {
        _logger.LogInformation("🔍 Generando diagnóstico detallado del sistema...");

        // Obtener todos los datos
        var sourceData = await _source.GetAllDataAsync();
        var destData = await _destination.GetAllDataAsync();

        var response = new DiagnosticsResponse
        {
            Timestamp = DateTime.UtcNow,
            Sistema = "BlockSync.NET - Diagnóstico Completo",
            Origen = GenerateStatistics(sourceData, "Origen"),
            Destino = GenerateStatistics(destData, "Destino"),
            MuestraAleatoria = GenerateSampleRecords(sourceData, 10),
            Memoria = GetMemoryInfo()
        };

        // Generar resumen
        response.Resumen = $"Sistema con {response.Origen.TotalRegistros:N0} registros en origen " +
                          $"({response.Origen.TotalBloques} bloques) y {response.Destino.TotalRegistros:N0} en destino. " +
                          $"Memoria utilizada: {response.Memoria.MemoriaUsadaMB:N0} MB. " +
                          $"Datos generados con {response.Origen.ClientesUnicos:N0} clientes únicos y " +
                          $"{response.Origen.ProductosUnicos:N0} productos únicos.";

        _logger.LogInformation("✅ Diagnóstico generado exitosamente");
        return response;
    }

    private DataStatistics GenerateStatistics(List<Venta> data, string source)
    {
        if (!data.Any())
        {
            return new DataStatistics
            {
                TotalRegistros = 0,
                TotalBloques = 0
            };
        }

        var grouped = data.GroupBy(v => v.Periodo).ToList();

        var stats = new DataStatistics
        {
            TotalRegistros = data.Count,
            TotalBloques = grouped.Count,
            MontoTotal = data.Sum(v => v.Monto),
            MontoPromedio = data.Average(v => v.Monto),
            MontoMinimo = data.Min(v => v.Monto),
            MontoMaximo = data.Max(v => v.Monto),
            PrimerRegistroId = data.First().Id.ToString(),
            UltimoRegistroId = data.Last().Id.ToString(),
            FechaMinima = data.Min(v => v.FechaVenta),
            FechaMaxima = data.Max(v => v.FechaVenta),
            ClientesUnicos = data.Select(v => v.Cliente).Distinct().Count(),
            ProductosUnicos = data.Select(v => v.Producto).Distinct().Count(),
            Top10PeriodosPorRegistros = grouped
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new PeriodInfo
                {
                    Periodo = g.Key,
                    Registros = g.Count(),
                    MontoTotal = g.Sum(v => v.Monto),
                    Hash = HashCalculator.CalculateHash(g.ToList())
                })
                .ToList()
        };

        _logger.LogInformation($"📊 Estadísticas de {source}: {stats.TotalRegistros:N0} registros, " +
                              $"{stats.ClientesUnicos:N0} clientes únicos, " +
                              $"{stats.ProductosUnicos:N0} productos únicos");

        return stats;
    }

    private List<SampleRecord> GenerateSampleRecords(List<Venta> data, int count)
    {
        if (!data.Any()) return new List<SampleRecord>();

        var random = new Random();
        return Enumerable.Range(0, Math.Min(count, data.Count))
            .Select(_ =>
            {
                var venta = data[random.Next(data.Count)];
                return new SampleRecord
                {
                    Id = venta.Id.ToString(),
                    Fecha = venta.FechaVenta,
                    Cliente = venta.Cliente,
                    Producto = venta.Producto,
                    Monto = venta.Monto,
                    Periodo = venta.Periodo
                };
            })
            .ToList();
    }

    private MemoryInfo GetMemoryInfo()
    {
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var memoryUsed = currentProcess.WorkingSet64 / 1024 / 1024; // MB
        var totalMemory = GC.GetTotalMemory(false) / 1024 / 1024; // MB

        return new MemoryInfo
        {
            MemoriaUsadaMB = memoryUsed,
            MemoriaTotalMB = totalMemory,
            CollectionsGC0 = GC.CollectionCount(0),
            CollectionsGC1 = GC.CollectionCount(1),
            CollectionsGC2 = GC.CollectionCount(2)
        };
    }

    /// <summary>
    /// Obtiene el ledger de sincronización (Blockchain metadata)
    /// </summary>
    public async Task<LedgerResponse> GetLedgerAsync()
    {
        _logger.LogInformation("📒 Obteniendo ledger de sincronización...");

        var entries = await _destination.GetLedgerEntriesAsync();
        var (total, sincronizados, corruptos, pendientes, errores) = await _destination.GetLedgerStatsAsync();

        var response = new LedgerResponse
        {
            Timestamp = DateTime.UtcNow,
            Stats = new LedgerStats
            {
                TotalBloques = total,
                Sincronizados = sincronizados,
                Corruptos = corruptos,
                Pendientes = pendientes,
                Errores = errores,
                PorcentajeSincronizado = total > 0 ? (double)sincronizados / total * 100 : 0
            },
            Entradas = entries.Select(LedgerEntryDto.FromEntity).ToList()
        };

        _logger.LogInformation("📒 Ledger obtenido: {Total} entradas ({Sincronizados} sincronizados, {Corruptos} corruptos)",
            total, sincronizados, corruptos);

        return response;
    }

    /// <summary>
    /// Actualiza el ledger de sincronización con la metadata del bloque sincronizado
    /// </summary>
    private async Task UpdateLedgerAsync(BlockHeader header, SyncAccion accion)
    {
        try
        {
            var entry = new SyncLedgerEntry
            {
                PeriodoId = header.Periodo,
                Hash = header.Hash,
                Estado = SyncEstado.SINCRONIZADO,
                UltimaSync = DateTime.UtcNow,
                TotalRegistros = header.TotalRegistros,
                SumaMontoCentavos = (long)(header.SumaMonto * 100),
                UltimaAccion = accion,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _destination.UpsertLedgerEntryAsync(entry);
        }
        catch (Exception ex)
        {
            // Log error pero no fallar la sincronización si el ledger falla
            _logger.LogWarning(ex, "⚠️ Error al actualizar ledger para periodo {Periodo}", header.Periodo);
        }
    }
}
