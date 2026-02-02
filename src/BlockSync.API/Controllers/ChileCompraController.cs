using BlockSync.Infrastructure.Services;
using BlockSync.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BlockSync.API.Controllers;

/// <summary>
/// Controller para gestionar datos de ChileCompra (Mercado Público).
/// Permite descargar, almacenar y consultar adjudicaciones (awards) de licitaciones públicas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChileCompraController : ControllerBase
{
    private readonly ChileCompraDataSeeder _seeder;
    private readonly ChileCompraAwardsRepository _sourceRepo;
    private readonly SqliteAwardsDestinationRepository _destRepo;
    private readonly ILogger<ChileCompraController> _logger;

    public ChileCompraController(
        ChileCompraDataSeeder seeder,
        ChileCompraAwardsRepository sourceRepo,
        SqliteAwardsDestinationRepository destRepo,
        ILogger<ChileCompraController> logger)
    {
        _seeder = seeder;
        _sourceRepo = sourceRepo;
        _destRepo = destRepo;
        _logger = logger;
    }

    /// <summary>
    /// Descarga y almacena awards desde ChileCompra API.
    /// </summary>
    /// <param name="year">Año a descargar (ej: 2025, 2024)</param>
    /// <param name="maxRecords">Máximo de registros JSONL a procesar (0 = ilimitado)</param>
    /// <param name="clearFirst">Si es true, limpia la BD antes de hacer seed</param>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedData(
        [FromQuery] int year = 2025,
        [FromQuery] int maxRecords = 0,
        [FromQuery] bool clearFirst = false)
    {
        _logger.LogInformation("🚀 Iniciando seed de ChileCompra - Año: {Year}, MaxRecords: {Max}, ClearFirst: {Clear}",
            year, maxRecords, clearFirst);

        try
        {
            if (clearFirst)
            {
                await _seeder.ClearAndSeedAsync(year, maxRecords);
            }
            else
            {
                await _seeder.SeedFromApiAsync(year, maxRecords);
            }

            var stats = await _seeder.GetStatsAsync();

            return Ok(new
            {
                success = true,
                message = "Datos descargados y almacenados exitosamente",
                year,
                maxRecordsRequested = maxRecords,
                stats = new
                {
                    totalAwards = stats.TotalAwards,
                    totalAmountCLP = stats.TotalAmountCLP,
                    dateRange = $"{stats.MinDate:yyyy-MM-dd} → {stats.MaxDate:yyyy-MM-dd}",
                    totalPeriods = stats.TotalPeriods
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante seed de ChileCompra");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de la base de datos ChileCompra.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _seeder.GetStatsAsync();
            var (totalRegistros, totalBloques, periodos) = await _sourceRepo.GetStatsAsync();

            return Ok(new
            {
                success = true,
                stats = new
                {
                    totalAwards = stats.TotalAwards,
                    totalAmountCLP = stats.TotalAmountCLP,
                    totalAmountFormatted = $"${stats.TotalAmountCLP:N0} CLP",
                    dateRange = new
                    {
                        from = stats.MinDate.ToString("yyyy-MM-dd"),
                        to = stats.MaxDate.ToString("yyyy-MM-dd")
                    },
                    totalPeriods = stats.TotalPeriods,
                    periods = periodos
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error obteniendo estadísticas");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Sincroniza awards desde la BD origen a la BD destino usando comparación de hashes.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Synchronize()
    {
        _logger.LogInformation("🔄 Iniciando sincronización de awards...");

        try
        {
            var startTime = DateTime.Now;

            // Obtener headers de ambos sistemas
            var sourceHeaders = await _sourceRepo.GetBlockHeadersAsync();
            var destHeaders = await _destRepo.GetBlockHeadersAsync();

            var destHeadersDict = destHeaders.ToDictionary(h => h.Periodo);

            var report = new
            {
                timestamp = DateTime.UtcNow,
                totalBloques = sourceHeaders.Count,
                bloquesInsertados = 0,
                bloquesSincronizados = 0,
                bloquesReparados = 0,
                totalRegistrosInsertados = 0,
                detalles = new List<object>()
            };

            int insertados = 0, sincronizados = 0, reparados = 0, totalRegs = 0;

            foreach (var sourceHeader in sourceHeaders.OrderBy(h => h.Periodo))
            {
                if (!destHeadersDict.TryGetValue(sourceHeader.Periodo, out var destHeader))
                {
                    // INSERT - No existe en destino
                    _logger.LogInformation("➕ {Periodo} - INSERT", sourceHeader.Periodo);

                    var data = await _sourceRepo.GetBlockDataAsync(sourceHeader.Periodo);
                    await _destRepo.InsertBlockAsync(data);

                    insertados++;
                    totalRegs += data.Count;

                    report.detalles.Add(new
                    {
                        periodo = sourceHeader.Periodo,
                        accion = "INSERT",
                        registros = data.Count,
                        hashOrigen = sourceHeader.Hash
                    });
                }
                else if (sourceHeader.Hash == destHeader.Hash)
                {
                    // SKIP - Hashes coinciden
                    _logger.LogInformation("✅ {Periodo} - SKIP (hashes coinciden)", sourceHeader.Periodo);
                    sincronizados++;

                    report.detalles.Add(new
                    {
                        periodo = sourceHeader.Periodo,
                        accion = "SKIP",
                        registros = sourceHeader.TotalRegistros,
                        hashOrigen = sourceHeader.Hash
                    });
                }
                else
                {
                    // REPAIR - Hashes difieren
                    _logger.LogInformation("🔧 {Periodo} - REPAIR (hashes difieren)", sourceHeader.Periodo);

                    await _destRepo.DeleteBlockAsync(sourceHeader.Periodo);
                    var data = await _sourceRepo.GetBlockDataAsync(sourceHeader.Periodo);
                    await _destRepo.InsertBlockAsync(data);

                    reparados++;
                    totalRegs += data.Count;

                    report.detalles.Add(new
                    {
                        periodo = sourceHeader.Periodo,
                        accion = "REPAIR",
                        registros = data.Count,
                        hashOrigen = sourceHeader.Hash,
                        hashDestinoAntes = destHeader.Hash
                    });
                }
            }

            var duration = DateTime.Now - startTime;

            return Ok(new
            {
                success = true,
                report = new
                {
                    report.timestamp,
                    duracion = $"{duration.TotalMilliseconds:F0}ms",
                    report.totalBloques,
                    bloquesInsertados = insertados,
                    bloquesSincronizados = sincronizados,
                    bloquesReparados = reparados,
                    totalRegistrosInsertados = totalRegs,
                    report.detalles
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante sincronización");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene los awards de un periodo específico.
    /// </summary>
    /// <param name="periodo">Periodo en formato yyyy-MM (ej: 2025-01)</param>
    [HttpGet("awards/{periodo}")]
    public async Task<IActionResult> GetAwards(string periodo)
    {
        try
        {
            var awards = await _sourceRepo.GetBlockDataAsync(periodo);

            return Ok(new
            {
                success = true,
                periodo,
                count = awards.Count,
                totalAmount = awards.Sum(a => a.Amount),
                awards = awards.Take(100) // Limitar a 100 para no sobrecargar respuesta
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error obteniendo awards");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Limpia la base de datos destino.
    /// </summary>
    [HttpPost("destination/clear")]
    public async Task<IActionResult> ClearDestination()
    {
        try
        {
            await _destRepo.ClearAllAsync();

            return Ok(new
            {
                success = true,
                message = "Base de datos destino limpiada exitosamente"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error limpiando destino");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de la base de datos destino.
    /// </summary>
    [HttpGet("destination/stats")]
    public async Task<IActionResult> GetDestinationStats()
    {
        try
        {
            var (totalRegistros, totalBloques, periodos) = await _destRepo.GetStatsAsync();

            return Ok(new
            {
                success = true,
                stats = new
                {
                    totalAwards = totalRegistros,
                    totalPeriods = totalBloques,
                    periods = periodos
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error obteniendo estadísticas del destino");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
