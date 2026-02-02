using BlockSync.Application.Services;
using BlockSync.Domain.Entities;
using BlockSync.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio de destino para awards (SQLite).
/// Recibe datos sincronizados desde ChileCompraAwardsRepository.
/// </summary>
public class SqliteAwardsDestinationRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteAwardsDestinationRepository> _logger;
    private readonly int _commandTimeout;

    public SqliteAwardsDestinationRepository(
        IConfiguration configuration,
        ILogger<SqliteAwardsDestinationRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("ChileCompraDestination")
            ?? throw new ArgumentNullException(nameof(configuration), "ChileCompra Destination connection string not configured");

        _logger = logger;
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");

        _logger.LogInformation("🔧 SqliteAwardsDestinationRepository inicializado");

        InitializeSchema();
    }

    /// <summary>
    /// Inicializa el esquema (crea tabla si no existe).
    /// </summary>
    private void InitializeSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS Awards (
                Id TEXT PRIMARY KEY,
                Ocid TEXT NOT NULL,
                AwardId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Status TEXT NOT NULL,
                AwardDate TEXT NOT NULL,
                Amount REAL NOT NULL,
                Currency TEXT NOT NULL,
                SupplierRut TEXT NOT NULL,
                SupplierName TEXT NOT NULL,
                BuyerName TEXT NOT NULL,
                PublishedDate TEXT NOT NULL,
                Periodo TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_awards_periodo ON Awards(Periodo);
            CREATE INDEX IF NOT EXISTS idx_awards_ocid ON Awards(Ocid);
        ";

        connection.Execute(createTableSql, commandTimeout: _commandTimeout);

        // PRAGMAs para optimización
        connection.Execute("PRAGMA journal_mode=WAL");
        connection.Execute("PRAGMA synchronous=NORMAL");

        _logger.LogInformation("✅ Esquema inicializado en destino");
    }

    /// <summary>
    /// Obtiene headers de bloques del destino.
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

        _logger.LogInformation("📊 Destination: {Count} block headers obtenidos", headers.Count);
        return headers;
    }

    /// <summary>
    /// Inserta un bloque completo de awards en el destino.
    /// </summary>
    public async Task InsertBlockAsync(List<Award> awards)
    {
        if (awards.Count == 0) return;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT INTO Awards
            (Id, Ocid, AwardId, Title, Status, AwardDate, Amount, Currency,
             SupplierRut, SupplierName, BuyerName, PublishedDate, Periodo)
            VALUES
            (@Id, @Ocid, @AwardId, @Title, @Status, @AwardDate, @Amount, @Currency,
             @SupplierRut, @SupplierName, @BuyerName, @PublishedDate, @Periodo)";

        using var transaction = connection.BeginTransaction();

        foreach (var award in awards)
        {
            await connection.ExecuteAsync(sql, new
            {
                Id = award.Id.ToString(),
                award.Ocid,
                award.AwardId,
                award.Title,
                award.Status,
                AwardDate = award.AwardDate.ToString("yyyy-MM-dd HH:mm:ss"),
                award.Amount,
                award.Currency,
                award.SupplierRut,
                award.SupplierName,
                award.BuyerName,
                PublishedDate = award.PublishedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                award.Periodo
            }, transaction, commandTimeout: _commandTimeout);
        }

        await transaction.CommitAsync();

        _logger.LogInformation("✅ {Count} awards insertados en periodo {Periodo}",
            awards.Count, awards.First().Periodo);
    }

    /// <summary>
    /// Elimina todos los awards de un periodo específico.
    /// </summary>
    public async Task DeleteBlockAsync(string periodo)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var deleted = await connection.ExecuteAsync(
            "DELETE FROM Awards WHERE Periodo = @Periodo",
            new { Periodo = periodo },
            commandTimeout: _commandTimeout);

        _logger.LogInformation("🗑️ {Count} awards eliminados del periodo {Periodo}", deleted, periodo);
    }

    /// <summary>
    /// Obtiene todos los awards de un periodo.
    /// </summary>
    public async Task<List<Award>> GetBlockDataAsync(string periodo)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT
                Id, Ocid, AwardId, Title, Status, AwardDate, Amount, Currency,
                SupplierRut, SupplierName, BuyerName, PublishedDate, Periodo
            FROM Awards
            WHERE Periodo = @Periodo
            ORDER BY AwardDate";

        var rows = await connection.QueryAsync<dynamic>(
            sql,
            new { Periodo = periodo },
            commandTimeout: _commandTimeout);

        return rows.Select(row => new Award
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
    }

    /// <summary>
    /// Limpia todos los datos del destino.
    /// </summary>
    public async Task ClearAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync("DELETE FROM Awards", commandTimeout: _commandTimeout);
        await connection.ExecuteAsync("VACUUM", commandTimeout: _commandTimeout);

        _logger.LogInformation("🗑️ Todos los awards eliminados del destino");
    }

    /// <summary>
    /// Obtiene estadísticas del destino.
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

        return (totalRegistros, periodos.Count, periodos);
    }
}
