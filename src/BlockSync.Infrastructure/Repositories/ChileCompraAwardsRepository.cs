using BlockSync.Application.Services;
using BlockSync.Domain.Entities;
using BlockSync.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio para leer awards de ChileCompra desde SQLite (origen).
/// Actúa como fuente de datos para sincronización.
/// </summary>
public class ChileCompraAwardsRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ChileCompraAwardsRepository> _logger;
    private readonly int _commandTimeout;

    public ChileCompraAwardsRepository(
        IConfiguration configuration,
        ILogger<ChileCompraAwardsRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("ChileCompraDb")
            ?? throw new ArgumentNullException(nameof(configuration), "ChileCompra connection string not configured");

        _logger = logger;
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");

        _logger.LogInformation("🔧 ChileCompraAwardsRepository inicializado");
    }

    /// <summary>
    /// Obtiene headers de bloques agrupados por periodo.
    /// Calcula hash basado en suma de montos y cantidad de awards.
    /// </summary>
    public async Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                Periodo,
                COUNT(*) as TotalRegistros,
                CAST(SUM(Amount) * 100 AS INTEGER) as SumaMontoCentavos
            FROM Awards
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

        _logger.LogInformation("📊 ChileCompra: {Count} block headers obtenidos", headers.Count);
        return headers;
    }

    /// <summary>
    /// Obtiene todos los awards de un periodo específico.
    /// </summary>
    public async Task<List<Award>> GetBlockDataAsync(string periodo)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                Id,
                Ocid,
                AwardId,
                Title,
                Status,
                AwardDate,
                Amount,
                Currency,
                SupplierRut,
                SupplierName,
                BuyerName,
                PublishedDate,
                Periodo
            FROM Awards
            WHERE Periodo = @Periodo
            ORDER BY AwardDate";

        var rows = await connection.QueryAsync<dynamic>(
            sql,
            new { Periodo = periodo },
            commandTimeout: _commandTimeout);

        var awards = rows.Select(row => new Award
        {
            Id = Guid.Parse((string)row.Id),
            Ocid = (string)row.Ocid,
            AwardId = (string)row.AwardId,
            Title = (string)row.Title,
            Status = (string)row.Status,
            AwardDate = DateTime.Parse((string)row.AwardDate),
            Amount = (decimal)(double)row.Amount,
            Currency = (string)row.Currency,
            SupplierRut = (string)row.SupplierRut,
            SupplierName = (string)row.SupplierName,
            BuyerName = (string)row.BuyerName,
            PublishedDate = DateTime.Parse((string)row.PublishedDate),
            Periodo = (string)row.Periodo
        }).ToList();

        _logger.LogInformation("📦 ChileCompra: {Count} awards descargados para periodo {Periodo}",
            awards.Count, periodo);

        return awards;
    }

    /// <summary>
    /// Obtiene estadísticas generales de la base de datos.
    /// </summary>
    public async Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var totalRegistros = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Awards",
            commandTimeout: _commandTimeout);

        var periodos = (await connection.QueryAsync<string>(
            "SELECT DISTINCT Periodo FROM Awards ORDER BY Periodo",
            commandTimeout: _commandTimeout)).ToList();

        _logger.LogInformation("📊 ChileCompra Stats: {TotalRecords} awards en {BlockCount} periodos",
            totalRegistros, periodos.Count);

        return (totalRegistros, periodos.Count, periodos);
    }

    /// <summary>
    /// Obtiene todos los awards (para diagnósticos).
    /// </summary>
    public async Task<List<Award>> GetAllDataAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                Id,
                Ocid,
                AwardId,
                Title,
                Status,
                AwardDate,
                Amount,
                Currency,
                SupplierRut,
                SupplierName,
                BuyerName,
                PublishedDate,
                Periodo
            FROM Awards
            ORDER BY AwardDate";

        var rows = await connection.QueryAsync<dynamic>(
            sql,
            commandTimeout: _commandTimeout);

        var awards = rows.Select(row => new Award
        {
            Id = Guid.Parse((string)row.Id),
            Ocid = (string)row.Ocid,
            AwardId = (string)row.AwardId,
            Title = (string)row.Title,
            Status = (string)row.Status,
            AwardDate = DateTime.Parse((string)row.AwardDate),
            Amount = (decimal)(double)row.Amount,
            Currency = (string)row.Currency,
            SupplierRut = (string)row.SupplierRut,
            SupplierName = (string)row.SupplierName,
            BuyerName = (string)row.BuyerName,
            PublishedDate = DateTime.Parse((string)row.PublishedDate),
            Periodo = (string)row.Periodo
        }).ToList();

        _logger.LogInformation("📦 ChileCompra: {Count} awards totales descargados", awards.Count);
        return awards;
    }
}
