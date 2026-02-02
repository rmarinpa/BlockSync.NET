using BlockSync.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Services;

/// <summary>
/// Servicio para poblar la base de datos SQLite con datos reales de ChileCompra.
/// Descarga awards desde la API OCDS y los almacena en SQLite para pruebas y sincronización.
/// </summary>
public class ChileCompraDataSeeder
{
    private readonly string _connectionString;
    private readonly ChileCompraApiClient _apiClient;
    private readonly ILogger<ChileCompraDataSeeder> _logger;
    private readonly int _commandTimeout;
    private readonly int _batchSize;

    public ChileCompraDataSeeder(
        IConfiguration configuration,
        ChileCompraApiClient apiClient,
        ILogger<ChileCompraDataSeeder> logger)
    {
        _connectionString = configuration.GetConnectionString("ChileCompraDb")
            ?? throw new ArgumentNullException(nameof(configuration), "ChileCompra connection string not configured");

        _apiClient = apiClient;
        _logger = logger;
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");
        _batchSize = int.Parse(configuration["DatabaseSettings:BulkInsertBatchSize"] ?? "10000");
    }

    /// <summary>
    /// Inicializa el esquema de la base de datos (crea tablas si no existen).
    /// </summary>
    public async Task InitializeSchemaAsync()
    {
        _logger.LogInformation("🔧 Inicializando esquema de base de datos ChileCompra...");

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

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
            CREATE INDEX IF NOT EXISTS idx_awards_award_date ON Awards(AwardDate);
            CREATE INDEX IF NOT EXISTS idx_awards_amount ON Awards(Amount);
        ";

        await connection.ExecuteAsync(createTableSql, commandTimeout: _commandTimeout);

        _logger.LogInformation("✅ Esquema inicializado correctamente");
    }

    /// <summary>
    /// Descarga y almacena awards desde ChileCompra para un año específico.
    /// </summary>
    /// <param name="year">Año a descargar (ej: 2025)</param>
    /// <param name="maxRecords">Máximo de registros JSONL a procesar (0 = ilimitado)</param>
    public async Task SeedFromApiAsync(int year, int maxRecords = 0)
    {
        _logger.LogInformation("🚀 Iniciando descarga de datos de ChileCompra para año {Year}...", year);

        var startTime = DateTime.Now;

        // Asegurar que el esquema existe
        await InitializeSchemaAsync();

        // Descargar awards desde la API
        _logger.LogInformation("📥 Descargando awards desde ChileCompra API...");
        var awards = await _apiClient.DownloadAwardsAsync(year, maxRecords);

        if (awards.Count == 0)
        {
            _logger.LogWarning("⚠️ No se descargaron awards. Abortando seed.");
            return;
        }

        _logger.LogInformation("✅ {Count:N0} awards descargados", awards.Count);

        // Insertar en SQLite
        await InsertBulkAsync(awards);

        var duration = DateTime.Now - startTime;
        _logger.LogInformation("✅ Seed completado en {Duration:mm\\:ss\\.ff}", duration);

        // Estadísticas finales
        await LogStatsAsync();
    }

    /// <summary>
    /// Limpia la tabla Awards y vuelve a hacer seed.
    /// </summary>
    public async Task ClearAndSeedAsync(int year, int maxRecords = 0)
    {
        // Asegurar que el esquema existe antes de limpiar
        await InitializeSchemaAsync();

        _logger.LogInformation("🗑️ Limpiando tabla Awards...");

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("DELETE FROM Awards", commandTimeout: _commandTimeout);
            await connection.ExecuteAsync("VACUUM"); // Liberar espacio
        }

        _logger.LogInformation("✅ Tabla limpiada");

        await SeedFromApiAsync(year, maxRecords);
    }

    /// <summary>
    /// Inserta awards en lotes optimizados con transacciones.
    /// </summary>
    private async Task InsertBulkAsync(List<Award> awards)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Configurar PRAGMAs para velocidad
        await connection.ExecuteAsync("PRAGMA journal_mode=WAL");
        await connection.ExecuteAsync("PRAGMA synchronous=NORMAL");
        await connection.ExecuteAsync("PRAGMA cache_size=-128000"); // 128MB cache
        await connection.ExecuteAsync("PRAGMA temp_store=MEMORY");

        _logger.LogInformation("⚡ PRAGMAs configurados para bulk insert");

        var sql = @"
            INSERT OR REPLACE INTO Awards
            (Id, Ocid, AwardId, Title, Status, AwardDate, Amount, Currency,
             SupplierRut, SupplierName, BuyerName, PublishedDate, Periodo)
            VALUES
            (@Id, @Ocid, @AwardId, @Title, @Status, @AwardDate, @Amount, @Currency,
             @SupplierRut, @SupplierName, @BuyerName, @PublishedDate, @Periodo)";

        var totalInserted = 0;
        var batches = awards.Chunk(_batchSize).ToList();

        _logger.LogInformation("📦 Insertando {Total:N0} awards en {BatchCount} lotes de {BatchSize:N0}...",
            awards.Count, batches.Count, _batchSize);

        for (int i = 0; i < batches.Count; i++)
        {
            var batch = batches[i];

            using (var transaction = connection.BeginTransaction())
            {
                foreach (var award in batch)
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
            }

            totalInserted += batch.Count();

            var progress = (double)(i + 1) / batches.Count * 100;
            _logger.LogInformation("⏳ Progreso: {Progress:F1}% ({Inserted:N0}/{Total:N0} awards)",
                progress, totalInserted, awards.Count);
        }

        _logger.LogInformation("✅ {Total:N0} awards insertados exitosamente", totalInserted);
    }

    /// <summary>
    /// Obtiene y registra estadísticas de la base de datos.
    /// </summary>
    public async Task LogStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT
                COUNT(*) as TotalAwards,
                SUM(Amount) as TotalAmount,
                MIN(AwardDate) as MinDate,
                MAX(AwardDate) as MaxDate,
                COUNT(DISTINCT Periodo) as TotalPeriods,
                COUNT(DISTINCT Ocid) as TotalContracts
            FROM Awards",
            commandTimeout: _commandTimeout);

        if (stats != null)
        {
            _logger.LogInformation("📊 === Estadísticas de ChileCompra DB ===");
            _logger.LogInformation("   Total Awards: {Total:N0}", (long)stats.TotalAwards);
            _logger.LogInformation("   Total Monto: ${Amount:N0} CLP", (double)stats.TotalAmount);
            _logger.LogInformation("   Rango Fechas: {Min} → {Max}", (string)stats.MinDate, (string)stats.MaxDate);
            _logger.LogInformation("   Periodos: {Periods}", (long)stats.TotalPeriods);
            _logger.LogInformation("   Contratos únicos: {Contracts:N0}", (long)stats.TotalContracts);
        }
    }

    /// <summary>
    /// Obtiene estadísticas agregadas para retornar en endpoints.
    /// </summary>
    public async Task<ChileCompraStats> GetStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT
                COUNT(*) as TotalAwards,
                COALESCE(SUM(Amount), 0) as TotalAmount,
                MIN(AwardDate) as MinDate,
                MAX(AwardDate) as MaxDate,
                COUNT(DISTINCT Periodo) as TotalPeriods
            FROM Awards",
            commandTimeout: _commandTimeout);

        if (stats == null || (long)stats.TotalAwards == 0)
        {
            return new ChileCompraStats
            {
                TotalAwards = 0,
                TotalAmountCLP = 0m,
                MinDate = DateTime.MinValue,
                MaxDate = DateTime.MinValue,
                TotalPeriods = 0
            };
        }

        return new ChileCompraStats
        {
            TotalAwards = (long)stats.TotalAwards,
            TotalAmountCLP = (decimal)(double)stats.TotalAmount,
            MinDate = stats.MinDate != null ? DateTime.Parse((string)stats.MinDate) : DateTime.MinValue,
            MaxDate = stats.MaxDate != null ? DateTime.Parse((string)stats.MaxDate) : DateTime.MinValue,
            TotalPeriods = (long)stats.TotalPeriods
        };
    }
}

/// <summary>
/// Estadísticas de la base de datos ChileCompra.
/// </summary>
public class ChileCompraStats
{
    public long TotalAwards { get; set; }
    public decimal TotalAmountCLP { get; set; }
    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }
    public long TotalPeriods { get; set; }
}
