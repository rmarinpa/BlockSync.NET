using BlockSync.Application.Services;
using BlockSync.Domain.Entities;
using BlockSync.Domain.Interfaces;
using BlockSync.Domain.ValueObjects;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio de destino que conecta a SQL Server
/// Implementa ISyncDestination para almacenar datos sincronizados
/// </summary>
public class SqlServerRepository : ISyncDestination
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly int _commandTimeout;
    private readonly int _bulkBatchSize;
    private readonly ILogger<SqlServerRepository> _logger;

    public SqlServerRepository(IConfiguration configuration, ILogger<SqlServerRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("SqlServerDestination")
            ?? throw new InvalidOperationException("⚠️ ConnectionString 'SqlServerDestination' no configurado en secrets.json");

        _tableName = configuration["DatabaseSettings:SqlServerTableName"] ?? "Ventas";
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");
        _bulkBatchSize = int.Parse(configuration["DatabaseSettings:BulkInsertBatchSize"] ?? "5000");
        _logger = logger;

        _logger.LogInformation("🚀 SqlServerRepository inicializado con tabla: {TableName}", _tableName);
    }

    /// <summary>
    /// Obtiene los block headers (metadatos) de todos los periodos almacenados
    /// </summary>
    public async Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                Periodo,
                COUNT(*) AS TotalRegistros,
                SUM(Monto) AS SumaMonto
            FROM {_tableName}
            GROUP BY Periodo
            ORDER BY Periodo";

        _logger.LogDebug("📊 Ejecutando query de block headers en SQL Server...");

        var results = await connection.QueryAsync<dynamic>(query, commandTimeout: _commandTimeout);

        var headers = new List<BlockHeader>();

        foreach (var row in results)
        {
            var periodo = (string)row.Periodo;
            var totalRegistros = (int)row.TotalRegistros;
            var sumaMonto = (decimal)row.SumaMonto;

            // Calcular hash usando la misma fórmula que Oracle
            var blockData = $"{sumaMonto}|{totalRegistros}";
            using var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(blockData);
            var hashBytes = md5.ComputeHash(inputBytes);
            var hash = Convert.ToHexString(hashBytes).ToLower();

            var header = new BlockHeader
            {
                Periodo = periodo,
                TotalRegistros = totalRegistros,
                SumaMonto = sumaMonto,
                Hash = hash
            };

            headers.Add(header);
        }

        _logger.LogInformation("✅ Se obtuvieron {Count} block headers de SQL Server", headers.Count);

        return headers;
    }

    /// <summary>
    /// Inserta un bloque completo usando SqlBulkCopy para máxima performance
    /// </summary>
    public async Task InsertBlockAsync(List<Venta> ventas)
    {
        if (ventas == null || ventas.Count == 0)
        {
            _logger.LogWarning("⚠️ Intento de insertar bloque vacío");
            return;
        }

        var periodo = ventas.First().Periodo;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Crear DataTable para SqlBulkCopy
        var dataTable = new DataTable();
        dataTable.Columns.Add("Id", typeof(Guid));
        dataTable.Columns.Add("FechaVenta", typeof(DateTime));
        dataTable.Columns.Add("Cliente", typeof(string));
        dataTable.Columns.Add("Producto", typeof(string));
        dataTable.Columns.Add("Monto", typeof(decimal));
        dataTable.Columns.Add("Periodo", typeof(string));

        foreach (var venta in ventas)
        {
            dataTable.Rows.Add(
                venta.Id,
                venta.FechaVenta,
                venta.Cliente,
                venta.Producto,
                venta.Monto,
                venta.Periodo
            );
        }

        _logger.LogDebug("📥 Insertando {Count} registros en SQL Server usando SqlBulkCopy", ventas.Count);

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = _tableName,
            BatchSize = _bulkBatchSize,
            BulkCopyTimeout = _commandTimeout
        };

        // Mapear columnas
        bulkCopy.ColumnMappings.Add("Id", "Id");
        bulkCopy.ColumnMappings.Add("FechaVenta", "FechaVenta");
        bulkCopy.ColumnMappings.Add("Cliente", "Cliente");
        bulkCopy.ColumnMappings.Add("Producto", "Producto");
        bulkCopy.ColumnMappings.Add("Monto", "Monto");
        bulkCopy.ColumnMappings.Add("Periodo", "Periodo");

        await bulkCopy.WriteToServerAsync(dataTable);

        _logger.LogInformation("✅ Se insertaron {Count} registros para periodo {Periodo} en SQL Server",
            ventas.Count, periodo);
    }

    /// <summary>
    /// Elimina todos los registros de un periodo específico
    /// </summary>
    public async Task DeleteBlockAsync(string periodo)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $"DELETE FROM {_tableName} WHERE Periodo = @Periodo";

        _logger.LogDebug("🗑️ Eliminando bloque {Periodo} de SQL Server", periodo);

        var rowsAffected = await connection.ExecuteAsync(query, new { Periodo = periodo }, commandTimeout: _commandTimeout);

        _logger.LogInformation("✅ Se eliminaron {Count} registros del periodo {Periodo}",
            rowsAffected, periodo);
    }

    /// <summary>
    /// Obtiene estadísticas generales del destino
    /// </summary>
    public async Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var countQuery = $"SELECT COUNT(*) FROM {_tableName}";
        var totalRegistros = await connection.ExecuteScalarAsync<int>(countQuery, commandTimeout: _commandTimeout);

        var periodosQuery = $@"
            SELECT DISTINCT Periodo
            FROM {_tableName}
            ORDER BY Periodo";

        var periodos = (await connection.QueryAsync<string>(periodosQuery, commandTimeout: _commandTimeout)).ToList();

        _logger.LogDebug("📊 Stats de SQL Server: {TotalRegistros} registros, {TotalBloques} bloques",
            totalRegistros, periodos.Count);

        return (totalRegistros, periodos.Count, periodos);
    }

    /// <summary>
    /// Limpia todos los datos de la tabla (TRUNCATE)
    /// </summary>
    public async Task ClearAllAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $"TRUNCATE TABLE {_tableName}";

        _logger.LogWarning("⚠️ Ejecutando TRUNCATE TABLE en SQL Server");

        await connection.ExecuteAsync(query, commandTimeout: _commandTimeout);

        _logger.LogInformation("✅ Tabla {TableName} vaciada en SQL Server", _tableName);
    }

    /// <summary>
    /// Corrompe un bloque modificando aleatoriamente registros (para testing)
    /// </summary>
    public async Task CorruptBlockAsync(string periodo)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Modificar entre 10% y 30% de los registros del periodo
        var query = $@"
            UPDATE TOP (
                SELECT CAST(COUNT(*) * (0.1 + RAND() * 0.2) AS INT)
                FROM {_tableName}
                WHERE Periodo = @Periodo
            ) {_tableName}
            SET Monto = Monto * (0.9 + RAND() * 0.2)
            WHERE Periodo = @Periodo";

        _logger.LogWarning("⚠️ Corrompiendo bloque {Periodo} en SQL Server (testing)", periodo);

        var rowsAffected = await connection.ExecuteAsync(query, new { Periodo = periodo }, commandTimeout: _commandTimeout);

        _logger.LogInformation("✅ Se corrompieron {Count} registros del periodo {Periodo}",
            rowsAffected, periodo);
    }

    /// <summary>
    /// Obtiene todos los datos de un periodo específico
    /// </summary>
    public async Task<List<Venta>> GetBlockDataAsync(string periodo)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                Id,
                FechaVenta,
                Cliente,
                Producto,
                Monto,
                Periodo
            FROM {_tableName}
            WHERE Periodo = @Periodo
            ORDER BY FechaVenta";

        _logger.LogDebug("📥 Obteniendo datos de SQL Server para periodo: {Periodo}", periodo);

        var ventas = (await connection.QueryAsync<Venta>(query, new { Periodo = periodo }, commandTimeout: _commandTimeout)).ToList();

        _logger.LogInformation("✅ Se obtuvieron {Count} registros del periodo {Periodo}",
            ventas.Count, periodo);

        // Retornar copia defensiva
        return ventas.Select(v => new Venta
        {
            Id = v.Id,
            FechaVenta = v.FechaVenta,
            Cliente = v.Cliente,
            Producto = v.Producto,
            Monto = v.Monto,
            Periodo = v.Periodo
        }).ToList();
    }

    /// <summary>
    /// Obtiene todos los datos (usar con precaución - solo para diagnostics)
    /// </summary>
    public async Task<List<Venta>> GetAllDataAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                Id,
                FechaVenta,
                Cliente,
                Producto,
                Monto,
                Periodo
            FROM {_tableName}
            ORDER BY FechaVenta";

        _logger.LogWarning("⚠️ Descargando TODOS los datos de SQL Server - puede consumir mucha memoria");

        var ventas = (await connection.QueryAsync<Venta>(query, commandTimeout: _commandTimeout)).ToList();

        _logger.LogInformation("✅ Se obtuvieron {Count} registros totales de SQL Server", ventas.Count);

        return ventas;
    }
}
