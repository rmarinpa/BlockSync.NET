using BlockSync.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Services;

/// <summary>
/// Servicio optimizado para poblar la base de datos SQLite con datos generados por Bogus.
/// Reutiliza DataGenerator con seed 8675309 para reproducibilidad.
/// Incluye optimizaciones de performance con PRAGMAs y transacciones grandes.
/// </summary>
public class SqliteDataSeeder
{
    private readonly string _sourceConnectionString;
    private readonly ILogger<SqliteDataSeeder> _logger;
    private readonly int _commandTimeout;
    private readonly int _batchSize;

    public SqliteDataSeeder(
        IConfiguration configuration,
        ILogger<SqliteDataSeeder> logger)
    {
        _sourceConnectionString = configuration.GetConnectionString("SqliteSource")
            ?? throw new ArgumentNullException(nameof(configuration), "SQLite Source connection string not configured");

        _logger = logger;
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");
        _batchSize = int.Parse(configuration["DatabaseSettings:BulkInsertBatchSize"] ?? "10000"); // Aumentado para mejor performance
    }

    /// <summary>
    /// Genera e inserta 1 millón de registros en la base de datos SQLite SOURCE.
    /// Usa el mismo seed (8675309) que la implementación in-memory para consistencia.
    /// OPTIMIZADO con PRAGMAs y transacciones grandes.
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("🚀 Iniciando seed OPTIMIZADO de datos en SQLite Source...");

        var startTime = DateTime.Now;

        // Generar 1M de registros usando DataGenerator
        _logger.LogInformation("📊 Generando 1,000,000 registros con Bogus (seed: 8675309)...");
        var ventas = DataGenerator.GenerateVentas();
        _logger.LogInformation("✅ Registros generados: {Count}", ventas.Count);

        // Insertar en SQLite con optimizaciones
        await InsertBulkOptimizedAsync(ventas);

        var duration = DateTime.Now - startTime;
        _logger.LogInformation("✅ Seed completado en {Duration:mm\\:ss} (optimizado)", duration);
    }

    /// <summary>
    /// Limpia la tabla Ventas antes de hacer seed (útil para reset)
    /// </summary>
    public async Task ClearAndSeedAsync()
    {
        _logger.LogInformation("🗑️ Limpiando tabla Ventas en Source...");

        using (var connection = new SqliteConnection(_sourceConnectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("DELETE FROM Ventas", commandTimeout: _commandTimeout);
            await connection.ExecuteAsync("VACUUM"); // Liberar espacio
        }

        _logger.LogInformation("✅ Tabla limpiada");

        await SeedAsync();
    }

    /// <summary>
    /// Inserta datos OPTIMIZADO con BLOB/INTEGER y transacciones grandes
    /// Hasta 3-4x más rápido que versión anterior
    /// </summary>
    private async Task InsertBulkOptimizedAsync(List<Venta> ventas)
    {
        using var connection = new SqliteConnection(_sourceConnectionString);
        await connection.OpenAsync();

        // Configurar PRAGMAs para máxima velocidad
        await connection.ExecuteAsync("PRAGMA journal_mode=WAL");
        await connection.ExecuteAsync("PRAGMA synchronous=OFF"); // Muy rápido pero menos seguro (OK para seed)
        await connection.ExecuteAsync("PRAGMA cache_size=-256000"); // 256MB cache
        await connection.ExecuteAsync("PRAGMA temp_store=MEMORY");
        await connection.ExecuteAsync("PRAGMA locking_mode=EXCLUSIVE");

        _logger.LogInformation("⚡ PRAGMAs de alta velocidad configurados");

        var sql = @"
            INSERT INTO Ventas (Id, FechaVenta, Cliente, Producto, MontoCentavos, Periodo)
            VALUES (@Id, @FechaVenta, @Cliente, @Producto, @MontoCentavos, @Periodo)";

        var totalInserted = 0;
        var batches = ventas.Chunk(_batchSize).ToList();

        _logger.LogInformation("📦 Insertando {Total:N0} registros en {BatchCount} lotes de {BatchSize:N0}...",
            ventas.Count, batches.Count, _batchSize);

        var batchNumber = 0;
        var lastProgressReport = 0;

        foreach (var batch in batches)
        {
            batchNumber++;

            using var transaction = connection.BeginTransaction();

            try
            {
                var batchData = batch.Select(v => new
                {
                    Id = v.Id.ToByteArray(), // BLOB (16 bytes)
                    FechaVenta = v.FechaVenta.ToString("yyyy-MM-dd"),
                    v.Cliente,
                    v.Producto,
                    MontoCentavos = (long)(v.Monto * 100), // INTEGER (centavos)
                    v.Periodo
                });

                var inserted = await connection.ExecuteAsync(
                    sql,
                    batchData,
                    transaction,
                    commandTimeout: _commandTimeout);

                transaction.Commit();
                totalInserted += inserted;

                // Reportar progreso cada 5% o al final
                var progressPercent = (int)((double)totalInserted / ventas.Count * 100);
                if (progressPercent >= lastProgressReport + 5 || batchNumber == batches.Count)
                {
                    lastProgressReport = progressPercent;
                    _logger.LogInformation("   ⚡ Progreso: {Progress}% ({Inserted:N0} / {Total:N0})",
                        progressPercent, totalInserted, ventas.Count);
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "❌ Error al insertar lote {BatchNumber}", batchNumber);
                throw;
            }
        }

        // Restaurar PRAGMAs a valores seguros
        await connection.ExecuteAsync("PRAGMA synchronous=NORMAL");
        await connection.ExecuteAsync("PRAGMA locking_mode=NORMAL");

        _logger.LogInformation("✅ Total insertado: {Total:N0} registros (OPTIMIZADO)", totalInserted);
    }

    /// <summary>
    /// Verifica si la base de datos SOURCE ya tiene datos
    /// </summary>
    public async Task<bool> HasDataAsync()
    {
        using var connection = new SqliteConnection(_sourceConnectionString);
        await connection.OpenAsync();

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Ventas",
            commandTimeout: _commandTimeout);

        return count > 0;
    }

    /// <summary>
    /// Obtiene estadísticas de la base de datos SOURCE
    /// </summary>
    public async Task<(int totalRecords, int periodCount)> GetStatsAsync()
    {
        using var connection = new SqliteConnection(_sourceConnectionString);
        await connection.OpenAsync();

        var totalRecords = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Ventas",
            commandTimeout: _commandTimeout);

        var periodCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(DISTINCT Periodo) FROM Ventas",
            commandTimeout: _commandTimeout);

        return (totalRecords, periodCount);
    }
}
