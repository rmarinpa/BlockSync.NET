using BlockSync.Domain.Entities;
using BlockSync.Domain.Interfaces;
using BlockSync.Domain.ValueObjects;
using BlockSync.Application.Services;
using BlockSync.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio que simula un sistema legacy (origen).
/// En producción, esto se conectaría a una base de datos real o API externa.
/// Aquí usa almacenamiento en memoria con datos generados por Bogus.
/// </summary>
public class LegacyRepository : ISyncSource
{
    private readonly ILogger<LegacyRepository> _logger;
    private Dictionary<string, List<Venta>> _data;

    public LegacyRepository(ILogger<LegacyRepository> logger)
    {
        _logger = logger;
        _data = new Dictionary<string, List<Venta>>();
        Initialize();
    }

    /// <summary>
    /// Inicializa el repositorio con datos generados por Bogus
    /// </summary>
    private void Initialize()
    {
        _logger.LogInformation("🔧 Inicializando repositorio legacy (origen)...");
        _data = DataGenerator.GenerateVentasGroupedByPeriod();
        _logger.LogInformation($"✅ Repositorio legacy inicializado con {_data.Values.Sum(v => v.Count)} registros en {_data.Count} bloques");
    }

    /// <summary>
    /// Obtiene los headers de todos los bloques sin descargar los datos completos
    /// </summary>
    public Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        var headers = _data
            .Select(kvp => HashCalculator.CreateBlockHeader(kvp.Key, kvp.Value))
            .OrderBy(h => h.Periodo)
            .ToList();

        return Task.FromResult(headers);
    }

    /// <summary>
    /// Descarga los datos completos de un periodo específico
    /// </summary>
    public Task<List<Venta>> GetBlockDataAsync(string periodo)
    {
        if (_data.TryGetValue(periodo, out var ventas))
        {
            // Retornar una copia para evitar modificaciones externas
            return Task.FromResult(ventas.Select(v => new Venta
            {
                Id = v.Id,
                FechaVenta = v.FechaVenta,
                Cliente = v.Cliente,
                Producto = v.Producto,
                Monto = v.Monto,
                Periodo = v.Periodo
            }).ToList());
        }

        return Task.FromResult(new List<Venta>());
    }

    /// <summary>
    /// Obtiene estadísticas del sistema legacy
    /// </summary>
    public Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync()
    {
        var totalRegistros = _data.Values.Sum(v => v.Count);
        var totalBloques = _data.Count;
        var periodos = _data.Keys.OrderBy(k => k).ToList();

        return Task.FromResult((totalRegistros, totalBloques, periodos));
    }

    /// <summary>
    /// Reinicia el repositorio al estado original
    /// </summary>
    public Task ResetAsync()
    {
        _logger.LogInformation("🔄 Reiniciando repositorio legacy...");
        Initialize();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Obtiene TODOS los registros (para diagnósticos)
    /// </summary>
    public Task<List<Venta>> GetAllDataAsync()
    {
        var allData = _data.Values
            .SelectMany(v => v)
            .Select(v => new Venta
            {
                Id = v.Id,
                FechaVenta = v.FechaVenta,
                Cliente = v.Cliente,
                Producto = v.Producto,
                Monto = v.Monto,
                Periodo = v.Periodo
            })
            .ToList();

        return Task.FromResult(allData);
    }
}
