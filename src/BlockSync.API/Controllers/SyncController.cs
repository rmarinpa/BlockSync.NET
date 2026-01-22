using BlockSync.Application.DTOs;
using BlockSync.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BlockSync.API.Controllers;

/// <summary>
/// Controlador principal para operaciones de sincronización.
/// Expone endpoints REST para ejecutar y monitorear la sincronización de datos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SyncController : ControllerBase
{
    private readonly ISyncEngine _syncEngine;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncEngine syncEngine, ILogger<SyncController> logger)
    {
        _syncEngine = syncEngine;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado actual del sistema (origen y destino)
    /// </summary>
    /// <returns>Estado detallado de origen, destino y estado de sincronización</returns>
    /// <response code="200">Estado obtenido exitosamente</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemStatusResponse>> GetStatus()
    {
        _logger.LogInformation("📊 Solicitando estado del sistema...");
        var status = await _syncEngine.GetSystemStatusAsync();
        return Ok(status);
    }

    /// <summary>
    /// Ejecuta el proceso completo de sincronización
    /// </summary>
    /// <returns>Reporte detallado de la sincronización ejecutada</returns>
    /// <response code="200">Sincronización ejecutada exitosamente</response>
    /// <response code="500">Error durante la sincronización</response>
    [HttpPost]
    [ProducesResponseType(typeof(SyncResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SyncResponse>> ExecuteSync()
    {
        try
        {
            _logger.LogInformation("🚀 Iniciando sincronización...");
            var report = await _syncEngine.SynchronizeAsync();

            var response = new SyncResponse
            {
                Exitoso = true,
                Resumen = new SyncSummary
                {
                    DuracionMs = report.DuracionMs,
                    TotalBloques = report.TotalBloques,
                    BloquesOmitidos = report.BloquesOmitidos,
                    BloquesInsertados = report.BloquesInsertados,
                    BloquesReparados = report.BloquesReparados,
                    TotalRegistrosProcesados = report.TotalRegistrosProcesados,
                    TotalRegistrosInsertados = report.TotalRegistrosInsertados
                },
                Reporte = report,
                ResumenTexto = report.GenerarResumenTexto()
            };

            _logger.LogInformation("✅ Sincronización completada exitosamente");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la sincronización");
            return StatusCode(500, new SyncResponse
            {
                Exitoso = false,
                ResumenTexto = $"Error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Simula corrupción de datos en un periodo específico para testing
    /// </summary>
    /// <param name="anio">Año del periodo (ej: 2023)</param>
    /// <param name="mes">Mes del periodo (1-12)</param>
    /// <returns>Confirmación de la corrupción simulada</returns>
    /// <response code="200">Datos corrompidos exitosamente</response>
    /// <response code="400">Parámetros inválidos</response>
    /// <response code="500">Error durante la corrupción</response>
    [HttpPost("hack/{anio}/{mes}")]
    [ProducesResponseType(typeof(HackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HackResponse>> HackData(int anio, int mes)
    {
        if (mes < 1 || mes > 12)
        {
            return BadRequest(new HackResponse
            {
                Exitoso = false,
                Mensaje = "El mes debe estar entre 1 y 12"
            });
        }

        if (anio < 2020 || anio > DateTime.Now.Year)
        {
            return BadRequest(new HackResponse
            {
                Exitoso = false,
                Mensaje = $"El año debe estar entre 2020 y {DateTime.Now.Year}"
            });
        }

        try
        {
            await _syncEngine.CorruptDataAsync(anio, mes);

            var periodo = $"{anio:D4}-{mes:D2}";
            return Ok(new HackResponse
            {
                Exitoso = true,
                Mensaje = $"Datos del periodo {periodo} han sido corrompidos intencionalmente",
                PeriodoAfectado = periodo,
                Instrucciones = "Ejecuta POST /api/sync para detectar y reparar la corrupción"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ Error al corromper datos de {anio}-{mes}");
            return StatusCode(500, new HackResponse
            {
                Exitoso = false,
                Mensaje = $"Error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Reinicia el sistema completo (limpia destino y restaura origen)
    /// </summary>
    /// <returns>Confirmación del reinicio</returns>
    /// <response code="200">Sistema reiniciado exitosamente</response>
    /// <response code="500">Error durante el reinicio</response>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ResetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResetResponse>> ResetSystem()
    {
        try
        {
            _logger.LogInformation("🔄 Reiniciando sistema...");
            await _syncEngine.ResetSystemAsync();

            return Ok(new ResetResponse
            {
                Exitoso = true,
                Mensaje = "Sistema reiniciado completamente",
                AccionesRealizadas = new List<string>
                {
                    "✓ Almacenamiento local limpiado",
                    "✓ Datos de origen restaurados al estado original",
                    "✓ Todas las corrupciones eliminadas"
                },
                SiguientePaso = "Ejecuta POST /api/sync para sincronizar desde cero"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al reiniciar sistema");
            return StatusCode(500, new ResetResponse
            {
                Exitoso = false,
                Mensaje = $"Error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Obtiene el mapa de comparación de hashes entre origen y destino
    /// </summary>
    /// <returns>Comparación detallada de todos los bloques</returns>
    /// <response code="200">Comparación obtenida exitosamente</response>
    [HttpGet("hashes")]
    [ProducesResponseType(typeof(HashComparisonResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<HashComparisonResponse>> GetHashComparison()
    {
        _logger.LogInformation("🔍 Obteniendo comparación de hashes...");
        var comparison = await _syncEngine.GetHashComparisonAsync();
        return Ok(comparison);
    }
}
