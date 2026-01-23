using BlockSync.Domain.Configuration;
using BlockSync.Domain.Entities;
using BlockSync.Domain.Interfaces;
using BlockSync.Domain.ValueObjects;
using BlockSync.Application.Mapping;
using BlockSync.Application.Services;
using BlockSync.Infrastructure.QueryBuilding;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio genérico que funciona con cualquier base de datos
/// usando mapeo por configuración. Implementa ISyncSource e ISyncDestination.
/// </summary>
public class GenericSyncRepository<TEntity> : ISyncSource, ISyncDestination
    where TEntity : Venta, new()
{
    private readonly string _connectionString;
    private readonly string _provider;
    private readonly TableMapping _tableMapping;
    private readonly EntityMapper<TEntity> _mapper;
    private readonly DynamicQueryBuilder _queryBuilder;
    private readonly ILogger<GenericSyncRepository<TEntity>> _logger;
    private readonly int _batchSize;

    public GenericSyncRepository(
        DatabaseSystemConfig config,
        string tableName,
        Dictionary<string, TransformationConfig> transformations,
        ILogger<GenericSyncRepository<TEntity>> logger,
        int batchSize = 5000)
    {
        _connectionString = config.ConnectionString ?? throw new ArgumentNullException(nameof(config.ConnectionString));
        _provider = config.Provider ?? throw new ArgumentNullException(nameof(config.Provider));
        _tableMapping = config.Tables[tableName] ?? throw new ArgumentException($"Table '{tableName}' not found in configuration");
        _transformations = transformations ?? new Dictionary<string, TransformationConfig>();
        _mapper = new EntityMapper<TEntity>(_tableMapping, transformations);
        _queryBuilder = new DynamicQueryBuilder(_tableMapping, _provider);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _batchSize = batchSize;

        _logger.LogInformation($"🔧 GenericSyncRepository<{typeof(TEntity).Name}> inicializado: Provider={_provider}, Table={_tableMapping.FullName}");
    }

    #region ISyncSource Implementation

    public async Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        var sql = _queryBuilder.BuildBlockHeaders();
        var rows = await connection.QueryAsync<dynamic>(sql);

        var headers = new List<BlockHeader>();
        foreach (var row in rows)
        {
            var periodo = (string)row.Periodo;
            var totalRegistros = (int)(long)row.TotalRegistros;

            // SumaMontoCentavos viene como long de la BD
            var sumaMontoCentavos = (long)row.SumaMontoCentavos;
            var sumaMonto = sumaMontoCentavos / 100m;

            // Calcular hash usando HashCalculator
            var hash = HashCalculator.CalculateHash(sumaMonto, totalRegistros);

            headers.Add(new BlockHeader
            {
                Periodo = periodo,
                Hash = hash,
                TotalRegistros = totalRegistros,
                SumaMonto = sumaMonto
            });
        }

        _logger.LogInformation($"📊 {_provider}: {headers.Count} block headers obtenidos");
        return headers;
    }

    public async Task<List<Venta>> GetBlockDataAsync(string periodo)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        var sql = _queryBuilder.BuildSelectByPeriod();
        var rows = await connection.QueryAsync<dynamic>(sql, new { periodo });

        var ventas = rows.Select(row => _mapper.MapFromDatabase(row)).Cast<Venta>().ToList();

        _logger.LogInformation($"📦 {_provider}: {ventas.Count} registros descargados para periodo {periodo}");
        return ventas;
    }

    public async Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync()
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        var countSql = _queryBuilder.BuildCount();
        var totalRegistros = await connection.ExecuteScalarAsync<int>(countSql);

        var periodsSql = _queryBuilder.BuildPeriodDistribution();
        var periods = await connection.QueryAsync<dynamic>(periodsSql);

        var periodos = periods.Select(p => (string)p.Periodo).ToList();
        var totalBloques = periodos.Count;

        _logger.LogInformation($"📊 {_provider} Stats: {totalRegistros:N0} registros en {totalBloques} periodos");
        return (totalRegistros, totalBloques, periodos);
    }

    public async Task ResetAsync()
    {
        // En un repositorio genérico, reset solo limpia datos
        // No tiene lógica de "regenerar" datos como SqliteDataSeeder
        await ClearAllAsync();
        _logger.LogInformation($"🔄 {_provider}: Sistema reiniciado (datos limpiados)");
    }

    public async Task<List<Venta>> GetAllDataAsync()
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        var sql = _queryBuilder.BuildSelectAll();
        var rows = await connection.QueryAsync<dynamic>(sql);

        var ventas = rows.Select(row => _mapper.MapFromDatabase(row)).Cast<Venta>().ToList();

        _logger.LogInformation($"📦 {_provider}: {ventas.Count} registros obtenidos (todos)");
        return ventas;
    }

    #endregion

    #region ISyncDestination Implementation

    public async Task InsertBlockAsync(List<Venta> ventas)
    {
        if (ventas == null || !ventas.Any())
        {
            _logger.LogWarning("⚠️  InsertBlockAsync llamado sin registros");
            return;
        }

        using var connection = CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var sql = _queryBuilder.BuildInsert();
            var periodo = ventas.First().Periodo;

            // Insertar en batches para mejor performance
            for (int i = 0; i < ventas.Count; i += _batchSize)
            {
                var batch = ventas.Skip(i).Take(_batchSize);
                foreach (var venta in batch)
                {
                    var parameters = _mapper.MapToDatabase(venta as TEntity);
                    await connection.ExecuteAsync(sql, parameters, transaction);
                }

                _logger.LogDebug($"   Batch {i / _batchSize + 1}: {batch.Count()} registros insertados");
            }

            transaction.Commit();
            _logger.LogInformation($"✅ {_provider}: {ventas.Count} registros insertados en periodo {periodo}");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, $"❌ Error al insertar bloque en {_provider}");
            throw;
        }
    }

    public async Task DeleteBlockAsync(string periodo)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        var sql = _queryBuilder.BuildDeleteByPeriod();
        var rowsDeleted = await connection.ExecuteAsync(sql, new { periodo });

        _logger.LogInformation($"🗑️ {_provider}: {rowsDeleted} registros eliminados del periodo {periodo}");
    }

    public async Task ClearAllAsync()
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();

        var sql = $"DELETE FROM {_tableMapping.FullName}";
        var rowsDeleted = await connection.ExecuteAsync(sql);

        _logger.LogInformation($"🧹 {_provider}: {rowsDeleted} registros limpiados completamente");
    }

    public async Task CorruptBlockAsync(string periodo)
    {
        // Obtener datos del periodo
        var ventas = await GetBlockDataAsync(periodo);

        if (!ventas.Any())
        {
            _logger.LogWarning($"⚠️ No se encontraron datos del periodo {periodo} para corromper");
            return;
        }

        using var connection = CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var random = new Random();
            var cantidadACorromper = Math.Max(1, ventas.Count / random.Next(3, 10));

            // Construir UPDATE dinámico
            var montoColumn = _tableMapping.Columns["Monto"].PhysicalName;
            var idColumn = _tableMapping.Columns["Id"].PhysicalName;

            var updateSql = $@"
UPDATE {_tableMapping.FullName}
SET {montoColumn} = {_queryBuilder.GetParameterPlaceholder("newMonto")}
WHERE {idColumn} = {_queryBuilder.GetParameterPlaceholder("id")}";

            for (int i = 0; i < cantidadACorromper; i++)
            {
                var ventaAleatoria = ventas[random.Next(ventas.Count)];
                var newMontoCentavos = random.Next(1000, 100000); // Entre 10 y 1000 en decimal

                var parameters = new Dictionary<string, object>
                {
                    ["newMonto"] = newMontoCentavos,
                    ["id"] = ventaAleatoria.Id.ToByteArray() // Convertir Guid a byte[] para SQLite
                };

                await connection.ExecuteAsync(updateSql, parameters, transaction);
            }

            transaction.Commit();
            _logger.LogWarning($"💥 {_provider}: {cantidadACorromper} registros corrompidos en periodo {periodo}");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, $"❌ Error al corromper datos en {_provider}");
            throw;
        }
    }

    #endregion

    #region Ledger Methods (ISyncDestination)

    public async Task UpsertLedgerEntryAsync(SyncLedgerEntry entry)
    {
        // Solo aplica si la tabla SyncLedger está configurada
        if (!_tableMapping.Schema.Contains("SyncLedger") && !_tableMapping.PhysicalName.Contains("SyncLedger"))
        {
            // Esta es una tabla de datos, no de ledger
            // El ledger debe manejarse con otro repositorio
            _logger.LogDebug("UpsertLedgerEntryAsync: No aplicable para tabla de datos");
            return;
        }

        using var connection = CreateConnection();
        await connection.OpenAsync();

        var sql = _queryBuilder.BuildUpsertLedger();

        var parameters = new Dictionary<string, object?>
        {
            ["PeriodoId"] = entry.PeriodoId,
            ["Hash"] = entry.Hash,
            ["Estado"] = entry.Estado.ToString(),
            ["UltimaSync"] = entry.UltimaSync.ToString("yyyy-MM-ddTHH:mm:ss"),
            ["TotalRegistros"] = entry.TotalRegistros,
            ["SumaMontoCentavos"] = entry.SumaMontoCentavos,
            ["UltimaAccion"] = entry.UltimaAccion.ToString(),
            ["CreatedAt"] = entry.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
            ["UpdatedAt"] = entry.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
        };

        await connection.ExecuteAsync(sql, parameters);
        _logger.LogDebug($"📝 Ledger entry upserted: {entry.PeriodoId}");
    }

    public async Task<List<SyncLedgerEntry>> GetLedgerEntriesAsync()
    {
        // Implementación simple - en producción debería verificar que existe tabla SyncLedger
        _logger.LogDebug("GetLedgerEntriesAsync: No implementado en repositorio genérico");
        return new List<SyncLedgerEntry>();
    }

    public async Task<SyncLedgerEntry?> GetLedgerEntryAsync(string periodoId)
    {
        _logger.LogDebug($"GetLedgerEntryAsync({periodoId}): No implementado en repositorio genérico");
        return null;
    }

    public async Task<(int total, int sincronizados, int corruptos, int pendientes, int errores)> GetLedgerStatsAsync()
    {
        _logger.LogDebug("GetLedgerStatsAsync: No implementado en repositorio genérico");
        return (0, 0, 0, 0, 0);
    }

    #endregion

    #region Helper Methods

    private readonly Dictionary<string, TransformationConfig> _transformations;

    /// <summary>
    /// Crea una conexión según el provider configurado.
    /// </summary>
    private DbConnection CreateConnection()
    {
        return _provider.ToLower() switch
        {
            "sqlite" => new SqliteConnection(_connectionString),

            // Otros providers requieren sus respectivos paquetes NuGet
            "oracle" => throw new NotImplementedException(
                "Oracle provider requiere Oracle.ManagedDataAccess.Core NuGet package"),

            "sqlserver" => throw new NotImplementedException(
                "SQL Server provider requiere Microsoft.Data.SqlClient NuGet package"),

            "postgresql" => throw new NotImplementedException(
                "PostgreSQL provider requiere Npgsql NuGet package"),

            "mysql" => throw new NotImplementedException(
                "MySQL provider requiere MySql.Data NuGet package"),

            _ => throw new NotSupportedException($"Provider {_provider} no está soportado")
        };
    }

    #endregion
}
