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
/// Repositorio SQLite para el sistema destino (destination).
/// Escribe en destination.db - base de datos local que se sincroniza con source.
/// </summary>
public class SqliteDestinationRepository : ISyncDestination
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteDestinationRepository> _logger;
    private readonly int _commandTimeout;
    private readonly int _batchSize;

    public SqliteDestinationRepository(
        IConfiguration configuration,
        ILogger<SqliteDestinationRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("SqliteDestination")
            ?? throw new ArgumentNullException(nameof(configuration), "SQLite Destination connection string not configured");

        _logger = logger;
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");
        _batchSize = int.Parse(configuration["DatabaseSettings:BulkInsertBatchSize"] ?? "5000");

        _logger.LogInformation("🔧 SqliteDestinationRepository inicializado con DB: {ConnectionString}",
            _connectionString.Split('=').Last());

        InitializePragmas();
    }

    /// <summary>
    /// Configura PRAGMAs de SQLite para optimizar performance
    /// </summary>
    private void InitializePragmas()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.Execute("PRAGMA journal_mode=WAL");
        connection.Execute("PRAGMA synchronous=NORMAL");
        connection.Execute("PRAGMA cache_size=-64000");
        connection.Execute("PRAGMA temp_store=MEMORY");

        _logger.LogInformation("✅ SQLite Destination: PRAGMAs de optimización configurados");
    }

    public async Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                Periodo,
                COUNT(*) as TotalRegistros,
                SUM(MontoCentavos) as SumaMontoCentavos
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
            long sumaMontoCentavos = (long)agg.SumaMontoCentavos;
            decimal sumaMonto = sumaMontoCentavos / 100m;

            var hash = HashCalculator.CalculateHash(sumaMonto, totalRegistros);
            headers.Add(new BlockHeader(periodo, hash, totalRegistros, sumaMonto));
        }

        _logger.LogInformation("📊 SQLite Destination: {Count} block headers obtenidos", headers.Count);
        return headers;
    }

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
                INSERT INTO Ventas (Id, FechaVenta, Cliente, Producto, MontoCentavos, Periodo)
                VALUES (@Id, @FechaVenta, @Cliente, @Producto, @MontoCentavos, @Periodo)";

            var totalInserted = 0;

            // Procesar en lotes para mejor performance
            foreach (var batch in ventas.Chunk(_batchSize))
            {
                var batchData = batch.Select(v => new
                {
                    Id = v.Id.ToByteArray(),
                    FechaVenta = v.FechaVenta.ToString("yyyy-MM-dd"),
                    v.Cliente,
                    v.Producto,
                    MontoCentavos = (long)(v.Monto * 100), // Convertir a centavos
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

    public async Task CorruptBlockAsync(string periodo)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Obtener IDs de registros del periodo
        var ids = (await connection.QueryAsync<byte[]>(
            "SELECT Id FROM Ventas WHERE Periodo = @Periodo",
            new { Periodo = periodo },
            commandTimeout: _commandTimeout)).ToList();

        if (!ids.Any())
        {
            _logger.LogWarning("⚠️ SQLite Destination: No hay registros en periodo {Periodo} para corromper", periodo);
            return;
        }

        var random = new Random();
        var corruptCount = random.Next(
            (int)(ids.Count * 0.10),
            (int)(ids.Count * 0.30));

        var toCorrupt = ids
            .OrderBy(_ => random.Next())
            .Take(corruptCount)
            .ToList();

        using var transaction = connection.BeginTransaction();

        try
        {
            var sql = @"
                UPDATE Ventas
                SET MontoCentavos = @NewMontoCentavos
                WHERE Id = @Id";

            foreach (var id in toCorrupt)
            {
                // Generar nuevo monto positivo aleatorio (entre $10 y $1000)
                var newMontoCentavos = random.Next(1000, 100000);

                await connection.ExecuteAsync(
                    sql,
                    new { Id = id, NewMontoCentavos = newMontoCentavos },
                    transaction,
                    commandTimeout: _commandTimeout);
            }

            transaction.Commit();

            _logger.LogWarning("🔧 SQLite Destination: {Count} registros corrompidos en periodo {Periodo} (de {Total} totales)",
                corruptCount, periodo, ids.Count);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "❌ SQLite Destination: Error al corromper bloque {Periodo}", periodo);
            throw;
        }
    }

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

        _logger.LogInformation("📊 SQLite Destination Stats: {TotalRecords} registros en {BlockCount} periodos",
            totalRegistros, periodos.Count);

        return (totalRegistros, periodos.Count, periodos);
    }

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
                MontoCentavos,
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
            Id = new Guid((byte[])row.Id),
            FechaVenta = DateTime.Parse((string)row.FechaVenta),
            Cliente = (string)row.Cliente,
            Producto = (string)row.Producto,
            Monto = (long)row.MontoCentavos / 100m,
            Periodo = (string)row.Periodo
        }).ToList();

        _logger.LogInformation("📦 SQLite Destination: {Count} registros descargados para periodo {Periodo}",
            ventas.Count, periodo);

        return ventas;
    }

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
                MontoCentavos,
                Periodo
            FROM Ventas
            ORDER BY FechaVenta";

        var rows = await connection.QueryAsync<dynamic>(
            sql,
            commandTimeout: _commandTimeout);

        var ventas = rows.Select(row => new Venta
        {
            Id = new Guid((byte[])row.Id),
            FechaVenta = DateTime.Parse((string)row.FechaVenta),
            Cliente = (string)row.Cliente,
            Producto = (string)row.Producto,
            Monto = (long)row.MontoCentavos / 100m,
            Periodo = (string)row.Periodo
        }).ToList();

        _logger.LogInformation("📦 SQLite Destination: {Count} registros totales descargados", ventas.Count);
        return ventas;
    }
}
