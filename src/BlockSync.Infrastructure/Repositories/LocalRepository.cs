using BlockSync.Domain.Entities;
using BlockSync.Domain.Interfaces;
using BlockSync.Domain.ValueObjects;
using BlockSync.Application.Services;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio que simula un sistema local (destino).
/// En producción, esto se conectaría a una base de datos local.
/// Aquí usa almacenamiento en memoria (List).
/// </summary>
public class LocalRepository : ISyncDestination
{
    private readonly ILogger<LocalRepository> _logger;
    private List<Venta> _data;
    private readonly object _lock = new();

    public LocalRepository(ILogger<LocalRepository> logger)
    {
        _logger = logger;
        _data = new List<Venta>();
        _logger.LogInformation("🔧 Repositorio local (destino) inicializado vacío");
    }

    /// <summary>
    /// Obtiene los headers de todos los bloques almacenados localmente
    /// </summary>
    public Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        lock (_lock)
        {
            var headers = _data
                .GroupBy(v => v.Periodo)
                .Select(g => HashCalculator.CreateBlockHeader(g.Key, g.ToList()))
                .OrderBy(h => h.Periodo)
                .ToList();

            return Task.FromResult(headers);
        }
    }

    /// <summary>
    /// Inserta un bloque completo de ventas
    /// </summary>
    public Task InsertBlockAsync(List<Venta> ventas)
    {
        if (!ventas.Any()) return Task.CompletedTask;

        lock (_lock)
        {
            // Crear copias para evitar referencias externas
            var copias = ventas.Select(v => new Venta
            {
                Id = v.Id,
                FechaVenta = v.FechaVenta,
                Cliente = v.Cliente,
                Producto = v.Producto,
                Monto = v.Monto,
                Periodo = v.Periodo
            }).ToList();

            _data.AddRange(copias);

            var periodo = ventas.First().Periodo;
            _logger.LogInformation($"💾 Insertados {ventas.Count} registros del periodo {periodo}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Elimina todos los registros de un periodo
    /// </summary>
    public Task DeleteBlockAsync(string periodo)
    {
        lock (_lock)
        {
            var count = _data.RemoveAll(v => v.Periodo == periodo);
            _logger.LogInformation($"🗑️  Eliminados {count} registros del periodo {periodo}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Obtiene estadísticas del sistema local
    /// </summary>
    public Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync()
    {
        lock (_lock)
        {
            var totalRegistros = _data.Count;
            var periodos = _data.Select(v => v.Periodo).Distinct().OrderBy(p => p).ToList();
            var totalBloques = periodos.Count;

            return Task.FromResult((totalRegistros, totalBloques, periodos));
        }
    }

    /// <summary>
    /// Limpia completamente el almacenamiento local
    /// </summary>
    public Task ClearAllAsync()
    {
        lock (_lock)
        {
            var count = _data.Count;
            _data.Clear();
            _logger.LogInformation($"🧹 Limpiados {count} registros del almacenamiento local");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Corrompe datos de un periodo para simular errores.
    /// Modifica los montos aleatoriamente para que el hash cambie.
    /// </summary>
    public Task CorruptBlockAsync(string periodo)
    {
        lock (_lock)
        {
            var ventasDelPeriodo = _data.Where(v => v.Periodo == periodo).ToList();

            if (!ventasDelPeriodo.Any())
            {
                _logger.LogWarning($"⚠️  No se encontraron datos del periodo {periodo} para corromper");
                return Task.CompletedTask;
            }

            var random = new Random();

            // Modificar entre 10% y 30% de los registros
            var cantidadACorromper = Math.Max(1, ventasDelPeriodo.Count / random.Next(3, 10));

            for (int i = 0; i < cantidadACorromper; i++)
            {
                var ventaAleatoria = ventasDelPeriodo[random.Next(ventasDelPeriodo.Count)];
                // Modificar el monto entre +/- 50%
                var factor = 0.5m + ((decimal)random.NextDouble());
                ventaAleatoria.Monto *= factor;
            }

            _logger.LogWarning($"💥 Corrompidos {cantidadACorromper} registros del periodo {periodo}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Obtiene todos los registros de un periodo específico
    /// </summary>
    public Task<List<Venta>> GetBlockDataAsync(string periodo)
    {
        lock (_lock)
        {
            var ventas = _data
                .Where(v => v.Periodo == periodo)
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

            return Task.FromResult(ventas);
        }
    }

    /// <summary>
    /// Obtiene TODOS los registros (para diagnósticos)
    /// </summary>
    public Task<List<Venta>> GetAllDataAsync()
    {
        lock (_lock)
        {
            var copias = _data.Select(v => new Venta
            {
                Id = v.Id,
                FechaVenta = v.FechaVenta,
                Cliente = v.Cliente,
                Producto = v.Producto,
                Monto = v.Monto,
                Periodo = v.Periodo
            }).ToList();

            return Task.FromResult(copias);
        }
    }
}
