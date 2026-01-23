using BlockSync.Application.Services;
using BlockSync.Domain.Entities;
using BlockSync.Domain.Interfaces;
using BlockSync.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio SQLite que implementa tanto origen como destino de sincronización.
/// Compatible con Mac sin necesidad de instalar motor de base de datos.
/// </summary>
public class SqliteRepository : ISyncSource, ISyncDestination
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteRepository> _logger;
    private readonly int _commandTimeout;
    private readonly int _batchSize;

    public SqliteRepository(
        IConfiguration configuration,
        ILogger<SqliteRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("SqliteDatabase")
            ?? throw new ArgumentNullException(nameof(configuration), "SQLite connection string not configured");

        _logger = logger;
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");
        _batchSize = int.Parse(configuration["DatabaseSettings:BulkInsertBatchSize"] ?? "5000");

        _logger.LogInformation("🔧 SqliteRepository inicializado con DB: {ConnectionString}",
            _connectionString.Split('=').Last());
    }

    #region ISyncSource Implementation

    /// <summary>
    /// Obtiene los headers de bloques desde SQLite (usado como origen)
    /// </summary>
    public async Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                Periodo,
                COUNT(*) as TotalRegistros,
                SUM(Monto) as SumaMonto
            FROM Ventas
            GROUP BY Periodo
            ORDER BY Periodo";

        var aggregates = await connection.QueryAsync<dynamic>(
            sql,
            commandTimeout: _commandTimeout);

        var headers = new List<BlockHeader>();
        foreach (var agg in aggregates)
        {
            string periodo = agg.Periodo;
            int totalRegistros = (int)(long)agg.TotalRegistros;
            decimal sumaMonto = Convert.ToDecimal(agg.SumaMonto);

            var hash = HashCalculator.CalculateHash(sumaMonto, totalRegistros);
            headers.Add(new BlockHeader(periodo, hash, totalRegistros, sumaMonto));
        }

        _logger.LogInformation("📊 SQLite Source: {Count} block headers obtenidos", headers.Count);
        return headers;
    }

    /// <summary>
    /// Descarga un bloque completo de datos desde SQLite
    /// </summary>
    public async Task<List<Venta>> GetBlockDataAsync(string periodo)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                Id,
                FechaVenta,
                Cliente,
                Producto,
                Monto,
                Periodo
            FROM Ventas
            WHERE Periodo = @Periodo
            ORDER BY FechaVenta";

        var rows = await connection.QueryAsync<dynamic>(
            sql,
            new { Periodo = periodo },
            commandTimeout: _commandTimeout);

        var ventas = rows.Select(row => new Venta
        {
            Id = Guid.Parse((string)row.Id),
            FechaVenta = DateTime.Parse((string)row.FechaVenta),
            Cliente = (string)row.Cliente,
            Producto = (string)row.Producto,
            Monto = Convert.ToDecimal(row.Monto),
            Periodo = (string)row.Periodo
        }).ToList();

        _logger.LogInformation("📦 SQLite Source: {Count} registros descargados para periodo {Periodo}",
            ventas.Count, periodo);

        return ventas;
    }

    /// <summary>
    /// Obtiene estadísticas generales de la base de datos SQLite
    /// </summary>
    public async Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var totalRegistros = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Ventas",
            commandTimeout: _commandTimeout);

        var periodos = (await connection.QueryAsync<string>(
            "SELECT DISTINCT Periodo FROM Ventas ORDER BY Periodo",
            commandTimeout: _commandTimeout)).ToList();

        _logger.LogInformation("📊 SQLite Stats: {TotalRecords} registros en {BlockCount} periodos",
            totalRegistros, periodos.Count);

        return (totalRegistros, periodos.Count, periodos);
    }

    /// <summary>
    /// Descarga todos los datos (para diagnósticos)
    /// </summary>
    public async Task<List<Venta>> GetAllDataAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                Id,
                FechaVenta,
                Cliente,
                Producto,
                Monto,
                Periodo
            FROM Ventas
            ORDER BY FechaVenta";

        var rows = await connection.QueryAsync<dynamic>(
            sql,
            commandTimeout: _commandTimeout);

        var ventas = rows.Select(row => new Venta
        {
            Id = Guid.Parse((string)row.Id),
            FechaVenta = DateTime.Parse((string)row.FechaVenta),
            Cliente = (string)row.Cliente,
            Producto = (string)row.Producto,
            Monto = Convert.ToDecimal(row.Monto),
            Periodo = (string)row.Periodo
        }).ToList();

        _logger.LogInformation("📦 SQLite: {Count} registros totales descargados", ventas.Count);
        return ventas;
    }

    /// <summary>
    /// Reset no hace nada en SQLite (similar a Oracle en producción)
    /// </summary>
    public Task ResetAsync()
    {
        _logger.LogWarning("⚠️ SQLite: ResetAsync no implementado (use ClearAllAsync si necesita limpiar)");
        return Task.CompletedTask;
    }

    #endregion

    #region ISyncDestination Implementation

    /// <summary>
    /// Inserta un bloque completo de ventas en SQLite usando transacción
    /// </summary>
    public async Task InsertBlockAsync(List<Venta> ventas)
    {
        if (!ventas.Any())
        {
            _logger.LogWarning("⚠️ SQLite Destination: No hay registros para insertar");
            return;
        }

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            var sql = @"
                INSERT INTO Ventas (Id, FechaVenta, Cliente, Producto, Monto, Periodo)
                VALUES (@Id, @FechaVenta, @Cliente, @Producto, @Monto, @Periodo)";

            var totalInserted = 0;

            // Procesar en lotes para mejor performance
            foreach (var batch in ventas.Chunk(_batchSize))
            {
                var batchData = batch.Select(v => new
                {
                    Id = v.Id.ToString(),
                    FechaVenta = v.FechaVenta.ToString("yyyy-MM-dd"),
                    v.Cliente,
                    v.Producto,
                    Monto = (double)v.Monto,
                    v.Periodo
                });

                var inserted = await connection.ExecuteAsync(
                    sql,
                    batchData,
                    transaction,
                    commandTimeout: _commandTimeout);

                totalInserted += inserted;
            }

            transaction.Commit();

            _logger.LogInformation("✅ SQLite Destination: {Count} registros insertados en periodo {Periodo}",
                totalInserted, ventas.First().Periodo);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "❌ SQLite Destination: Error al insertar bloque");
            throw;
        }
    }

    /// <summary>
    /// Elimina un bloque completo de ventas por periodo
    /// </summary>
    public async Task DeleteBlockAsync(string periodo)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = "DELETE FROM Ventas WHERE Periodo = @Periodo";

        var deleted = await connection.ExecuteAsync(
            sql,
            new { Periodo = periodo },
            commandTimeout: _commandTimeout);

        _logger.LogInformation("🗑️ SQLite Destination: {Count} registros eliminados del periodo {Periodo}",
            deleted, periodo);
    }

    /// <summary>
    /// Limpia completamente la tabla de destino
    /// </summary>
    public async Task ClearAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            "DELETE FROM Ventas",
            commandTimeout: _commandTimeout);

        // Liberar espacio no usado
        await connection.ExecuteAsync("VACUUM");

        _logger.LogInformation("🗑️ SQLite Destination: Tabla Ventas limpiada completamente");
    }

    /// <summary>
    /// Corrompe un bloque modificando montos aleatoriamente (para testing)
    /// </summary>
    public async Task CorruptBlockAsync(string periodo)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Obtener registros del periodo
        var ventas = await GetBlockDataAsync(periodo);

        if (!ventas.Any())
        {
            _logger.LogWarning("⚠️ SQLite: No hay registros en periodo {Periodo} para corromper", periodo);
            return;
        }

        var random = new Random();
        var corruptCount = random.Next(
            (int)(ventas.Count * 0.10),
            (int)(ventas.Count * 0.30));

        var toCorrupt = ventas
            .OrderBy(_ => random.Next())
            .Take(corruptCount)
            .ToList();

        using var transaction = connection.BeginTransaction();

        try
        {
            var sql = "UPDATE Ventas SET Monto = @NewMonto WHERE Id = @Id";

            foreach (var venta in toCorrupt)
            {
                var newMonto = venta.Monto * (decimal)(random.NextDouble() * 0.5 + 0.75);

                await connection.ExecuteAsync(
                    sql,
                    new { NewMonto = (double)newMonto, Id = venta.Id.ToString() },
                    transaction,
                    commandTimeout: _commandTimeout);
            }

            transaction.Commit();

            _logger.LogWarning("🔧 SQLite: {Count} registros corrompidos en periodo {Periodo} (de {Total} totales)",
                corruptCount, periodo, ventas.Count);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "❌ SQLite: Error al corromper bloque {Periodo}", periodo);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Verifica si la base de datos existe y tiene la tabla Ventas
    /// </summary>
    public async Task<bool> DatabaseExistsAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var tableExists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Ventas'");

            return tableExists > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Inicializa la base de datos ejecutando el script de schema
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        var schemaPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "database", "sqlite", "schema.sql");

        if (!File.Exists(schemaPath))
        {
            _logger.LogWarning("⚠️ SQLite: Schema file not found at {Path}", schemaPath);
            return;
        }

        var schema = await File.ReadAllTextAsync(schemaPath);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(schema, commandTimeout: _commandTimeout);

        _logger.LogInformation("✅ SQLite: Database initialized with schema");
    }

    #endregion
}
