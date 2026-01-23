using BlockSync.Domain.Entities;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlockSync.Infrastructure.Services;

/// <summary>
/// Servicio para poblar la base de datos SQLite con datos generados por Bogus.
/// Reutiliza DataGenerator con seed 8675309 para reproducibilidad.
/// </summary>
public class SqliteDataSeeder
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteDataSeeder> _logger;
    private readonly int _commandTimeout;
    private readonly int _batchSize;

    public SqliteDataSeeder(
        IConfiguration configuration,
        ILogger<SqliteDataSeeder> logger)
    {
        _connectionString = configuration.GetConnectionString("SqliteDatabase")
            ?? throw new ArgumentNullException(nameof(configuration), "SQLite connection string not configured");

        _logger = logger;
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");
        _batchSize = int.Parse(configuration["DatabaseSettings:BulkInsertBatchSize"] ?? "5000");
    }

    /// <summary>
    /// Genera e inserta 1 millón de registros en la base de datos SQLite.
    /// Usa el mismo seed (8675309) que la implementación in-memory para consistencia.
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("🚀 Iniciando seed de datos en SQLite...");

        var startTime = DateTime.Now;

        // Generar 1M de registros usando DataGenerator
        _logger.LogInformation("📊 Generando 1,000,000 registros con Bogus (seed: 8675309)...");
        var ventas = DataGenerator.GenerateVentas();
        _logger.LogInformation("✅ Registros generados: {Count}", ventas.Count);

        // Insertar en SQLite
        await InsertBulkAsync(ventas);

        var duration = DateTime.Now - startTime;
        _logger.LogInformation("✅ Seed completado en {Duration:mm\\:ss}", duration);
    }

    /// <summary>
    /// Limpia la tabla Ventas antes de hacer seed (útil para reset)
    /// </summary>
    public async Task ClearAndSeedAsync()
    {
        _logger.LogInformation("🗑️ Limpiando tabla Ventas...");

        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("DELETE FROM Ventas", commandTimeout: _commandTimeout);
            await connection.ExecuteAsync("VACUUM"); // Liberar espacio
        }

        _logger.LogInformation("✅ Tabla limpiada");

        await SeedAsync();
    }

    /// <summary>
    /// Inserta datos en lotes para optimizar performance en SQLite
    /// </summary>
    private async Task InsertBulkAsync(List<Venta> ventas)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var sql = @"
            INSERT INTO Ventas (Id, FechaVenta, Cliente, Producto, Monto, Periodo)
            VALUES (@Id, @FechaVenta, @Cliente, @Producto, @Monto, @Periodo)";

        var totalInserted = 0;
        var batches = ventas.Chunk(_batchSize).ToList();

        _logger.LogInformation("📦 Insertando {Total} registros en {BatchCount} lotes de {BatchSize}...",
            ventas.Count, batches.Count, _batchSize);

        var batchNumber = 0;
        foreach (var batch in batches)
        {
            batchNumber++;

            using var transaction = connection.BeginTransaction();

            try
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

                transaction.Commit();
                totalInserted += inserted;

                if (batchNumber % 10 == 0 || batchNumber == batches.Count)
                {
                    var progress = (double)totalInserted / ventas.Count * 100;
                    _logger.LogInformation("   Progreso: {Progress:F1}% ({Inserted:N0} / {Total:N0})",
                        progress, totalInserted, ventas.Count);
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "❌ Error al insertar lote {BatchNumber}", batchNumber);
                throw;
            }
        }

        _logger.LogInformation("✅ Total insertado: {Total:N0} registros", totalInserted);
    }

    /// <summary>
    /// Verifica si la base de datos ya tiene datos
    /// </summary>
    public async Task<bool> HasDataAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Ventas",
            commandTimeout: _commandTimeout);

        return count > 0;
    }

    /// <summary>
    /// Obtiene estadísticas de la base de datos
    /// </summary>
    public async Task<(int totalRecords, int periodCount)> GetStatsAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
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
