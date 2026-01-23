using BlockSync.Application.Services;
using BlockSync.Domain.Entities;
using BlockSync.Domain.Interfaces;
using BlockSync.Domain.ValueObjects;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio de origen que conecta a Oracle Database
/// Implementa ISyncSource para obtener datos históricos desde Oracle
/// </summary>
public class OracleRepository : ISyncSource
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private readonly int _commandTimeout;
    private readonly ILogger<OracleRepository> _logger;

    public OracleRepository(IConfiguration configuration, ILogger<OracleRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("OracleSource")
            ?? throw new InvalidOperationException("⚠️ ConnectionString 'OracleSource' no configurado en secrets.json");

        _tableName = configuration["DatabaseSettings:OracleTableName"] ?? "VENTAS";
        _commandTimeout = int.Parse(configuration["DatabaseSettings:CommandTimeout"] ?? "300");
        _logger = logger;

        _logger.LogInformation("🚀 OracleRepository inicializado con tabla: {TableName}", _tableName);
    }

    /// <summary>
    /// Obtiene los block headers (metadatos) de todos los periodos
    /// Solo descarga agregados (COUNT, SUM) sin bajar data completa
    /// </summary>
    public async Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                TO_CHAR(FECHA_VENTA, 'YYYY-MM') AS Periodo,
                COUNT(*) AS TotalRegistros,
                SUM(MONTO) AS SumaMonto
            FROM {_tableName}
            GROUP BY TO_CHAR(FECHA_VENTA, 'YYYY-MM')
            ORDER BY Periodo";

        _logger.LogDebug("📊 Ejecutando query de block headers en Oracle...");

        var results = await connection.QueryAsync<dynamic>(query, commandTimeout: _commandTimeout);

        var headers = new List<BlockHeader>();

        foreach (var row in results)
        {
            var periodo = (string)row.PERIODO;
            var totalRegistros = Convert.ToInt32(row.TOTALREGISTROS);
            var sumaMonto = Convert.ToDecimal(row.SUMAMONTO);

            // Calcular hash usando la fórmula MD5(SumaMonto|TotalRegistros)
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

        _logger.LogInformation("✅ Se obtuvieron {Count} block headers de Oracle", headers.Count);

        return headers;
    }

    /// <summary>
    /// Obtiene todos los datos de un bloque específico (periodo)
    /// </summary>
    public async Task<List<Venta>> GetBlockDataAsync(string periodo)
    {
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                ID,
                FECHA_VENTA AS FechaVenta,
                CLIENTE,
                PRODUCTO,
                MONTO,
                TO_CHAR(FECHA_VENTA, 'YYYY-MM') AS Periodo
            FROM {_tableName}
            WHERE TO_CHAR(FECHA_VENTA, 'YYYY-MM') = :periodo
            ORDER BY FECHA_VENTA";

        _logger.LogDebug("📥 Descargando datos de Oracle para periodo: {Periodo}", periodo);

        var results = await connection.QueryAsync<dynamic>(query, new { periodo }, commandTimeout: _commandTimeout);

        var ventas = results.Select(row => new Venta
        {
            Id = new Guid((byte[])row.ID),
            FechaVenta = ((DateTime)row.FECHAVENTA),
            Cliente = (string)row.CLIENTE,
            Producto = (string)row.PRODUCTO,
            Monto = (decimal)row.MONTO,
            Periodo = (string)row.PERIODO
        }).ToList();

        _logger.LogInformation("✅ Se descargaron {Count} registros de Oracle para periodo {Periodo}",
            ventas.Count, periodo);

        // Retornar copia defensiva para evitar mutación externa
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
    /// Obtiene estadísticas generales del origen
    /// </summary>
    public async Task<(int TotalRegistros, int TotalBloques, List<string> Periodos)> GetStatsAsync()
    {
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        var countQuery = $"SELECT COUNT(*) FROM {_tableName}";
        var totalRegistros = await connection.ExecuteScalarAsync<int>(countQuery, commandTimeout: _commandTimeout);

        var periodosQuery = $@"
            SELECT DISTINCT TO_CHAR(FECHA_VENTA, 'YYYY-MM') AS Periodo
            FROM {_tableName}
            ORDER BY Periodo";

        var periodos = (await connection.QueryAsync<string>(periodosQuery, commandTimeout: _commandTimeout)).ToList();

        _logger.LogDebug("📊 Stats de Oracle: {TotalRegistros} registros, {TotalBloques} bloques",
            totalRegistros, periodos.Count);

        return (totalRegistros, periodos.Count, periodos);
    }

    /// <summary>
    /// Obtiene todos los datos (usar con precaución - solo para diagnostics)
    /// </summary>
    public async Task<List<Venta>> GetAllDataAsync()
    {
        using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        var query = $@"
            SELECT
                ID,
                FECHA_VENTA AS FechaVenta,
                CLIENTE,
                PRODUCTO,
                MONTO,
                TO_CHAR(FECHA_VENTA, 'YYYY-MM') AS Periodo
            FROM {_tableName}
            ORDER BY FECHA_VENTA";

        _logger.LogWarning("⚠️ Descargando TODOS los datos de Oracle - puede consumir mucha memoria");

        var results = await connection.QueryAsync<dynamic>(query, commandTimeout: _commandTimeout);

        var ventas = results.Select(row => new Venta
        {
            Id = new Guid((byte[])row.ID),
            FechaVenta = ((DateTime)row.FECHAVENTA),
            Cliente = (string)row.CLIENTE,
            Producto = (string)row.PRODUCTO,
            Monto = (decimal)row.MONTO,
            Periodo = (string)row.PERIODO
        }).ToList();

        _logger.LogInformation("✅ Se descargaron {Count} registros totales de Oracle", ventas.Count);

        return ventas;
    }

    /// <summary>
    /// Reset no aplica para producción (Oracle es read-only source)
    /// </summary>
    public Task ResetAsync()
    {
        _logger.LogWarning("⚠️ ResetAsync() no implementado para Oracle (source es read-only en producción)");
        return Task.CompletedTask;
    }
}
