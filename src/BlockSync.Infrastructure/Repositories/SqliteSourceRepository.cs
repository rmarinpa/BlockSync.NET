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
/// Repositorio SQLite para el sistema origen (source).
/// Lee de source.db - base de datos de solo lectura que simula sistema legacy.
/// </summary>
public class SqliteSourceRepository : ISyncSource
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteSourceRepository> _logger;
    private readonly int _commandTimeout;

    public SqliteSourceRepository(
        IConfiguration configuration,
        ILogger<SqliteSourceRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("SqliteSource")
            ?? throw new ArgumentNullException(nameof(configuration), "SQLite Source connection string not configured");

        _logger = logger;
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");

        _logger.LogInformation("🔧 SqliteSourceRepository inicializado con DB: {ConnectionString}",
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

        _logger.LogInformation("✅ SQLite Source: PRAGMAs de optimización configurados");
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
            decimal sumaMonto = sumaMontoCentavos / 100m; // Convertir de centavos a decimal

            var hash = HashCalculator.CalculateHash(sumaMonto, totalRegistros);
            headers.Add(new BlockHeader(periodo, hash, totalRegistros, sumaMonto));
        }

        _logger.LogInformation("📊 SQLite Source: {Count} block headers obtenidos", headers.Count);
        return headers;
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

        _logger.LogInformation("📦 SQLite Source: {Count} registros descargados para periodo {Periodo}",
            ventas.Count, periodo);

        return ventas;
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

        _logger.LogInformation("📊 SQLite Source Stats: {TotalRecords} registros en {BlockCount} periodos",
            totalRegistros, periodos.Count);

        return (totalRegistros, periodos.Count, periodos);
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

        _logger.LogInformation("📦 SQLite Source: {Count} registros totales descargados", ventas.Count);
        return ventas;
    }

    public Task ResetAsync()
    {
        _logger.LogWarning("⚠️ SQLite Source: ResetAsync no implementado (sistema origen es inmutable)");
        return Task.CompletedTask;
    }
}
