using BlockSync.Domain.Configuration;

namespace BlockSync.Infrastructure.QueryBuilding;

/// <summary>
/// Constructor de queries SQL dinámicas basadas en mapeo de configuración.
/// Soporta diferentes dialectos: Oracle, SQL Server, PostgreSQL, MySQL, SQLite.
/// </summary>
public class DynamicQueryBuilder
{
    private readonly TableMapping _tableMapping;
    private readonly string _provider;

    public DynamicQueryBuilder(TableMapping tableMapping, string provider)
    {
        _tableMapping = tableMapping ?? throw new ArgumentNullException(nameof(tableMapping));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// SELECT * FROM tabla WHERE periodo = @periodo
    /// </summary>
    public string BuildSelectByPeriod()
    {
        var columns = string.Join(", ", _tableMapping.Columns.Values.Select(c => c.PhysicalName));
        var periodoColumn = _tableMapping.Columns[_tableMapping.BlockGroupingField].PhysicalName;

        return $@"
SELECT {columns}
FROM {_tableMapping.FullName}
WHERE {periodoColumn} = {GetParameterPlaceholder("periodo")}";
    }

    /// <summary>
    /// SELECT Periodo, COUNT(*), SUM(Monto) FROM tabla GROUP BY Periodo
    /// Usado para obtener block headers sin descargar datos completos.
    /// </summary>
    public string BuildBlockHeaders()
    {
        var periodoCol = _tableMapping.Columns[_tableMapping.BlockGroupingField].PhysicalName;

        // Buscar columna de monto (puede tener transformación de centavos)
        var montoMapping = _tableMapping.Columns["Monto"];
        var montoCol = montoMapping.PhysicalName;

        // Si el monto está en centavos, sumamos centavos directamente
        // La conversión a decimal se hará en el mapper
        return $@"
SELECT
    {periodoCol} as Periodo,
    COUNT(*) as TotalRegistros,
    SUM({montoCol}) as SumaMontoCentavos
FROM {_tableMapping.FullName}
GROUP BY {periodoCol}
ORDER BY {periodoCol}";
    }

    /// <summary>
    /// INSERT INTO tabla (col1, col2, ...) VALUES (@val1, @val2, ...)
    /// </summary>
    public string BuildInsert()
    {
        var columns = string.Join(", ", _tableMapping.Columns.Values.Select(c => c.PhysicalName));
        var parameters = string.Join(", ", _tableMapping.Columns.Values.Select(c =>
            GetParameterPlaceholder(c.PhysicalName)));

        return $@"
INSERT INTO {_tableMapping.FullName} ({columns})
VALUES ({parameters})";
    }

    /// <summary>
    /// DELETE FROM tabla WHERE periodo = @periodo
    /// </summary>
    public string BuildDeleteByPeriod()
    {
        var periodoCol = _tableMapping.Columns[_tableMapping.BlockGroupingField].PhysicalName;

        return $@"
DELETE FROM {_tableMapping.FullName}
WHERE {periodoCol} = {GetParameterPlaceholder("periodo")}";
    }

    /// <summary>
    /// SELECT COUNT(*) FROM tabla
    /// </summary>
    public string BuildCount()
    {
        return $"SELECT COUNT(*) FROM {_tableMapping.FullName}";
    }

    /// <summary>
    /// SELECT COUNT(*) as Total, SUM(Monto) FROM tabla
    /// </summary>
    public string BuildStats()
    {
        var montoCol = _tableMapping.Columns["Monto"].PhysicalName;

        return $@"
SELECT
    COUNT(*) as Total,
    SUM({montoCol}) as SumaMontoCentavos
FROM {_tableMapping.FullName}";
    }

    /// <summary>
    /// SELECT Periodo, COUNT(*) FROM tabla GROUP BY Periodo ORDER BY Periodo
    /// </summary>
    public string BuildPeriodDistribution()
    {
        var periodoCol = _tableMapping.Columns[_tableMapping.BlockGroupingField].PhysicalName;

        return $@"
SELECT
    {periodoCol} as Periodo,
    COUNT(*) as TotalRegistros
FROM {_tableMapping.FullName}
GROUP BY {periodoCol}
ORDER BY {periodoCol}";
    }

    /// <summary>
    /// SELECT * FROM tabla
    /// </summary>
    public string BuildSelectAll()
    {
        var columns = string.Join(", ", _tableMapping.Columns.Values.Select(c => c.PhysicalName));

        return $@"
SELECT {columns}
FROM {_tableMapping.FullName}";
    }

    /// <summary>
    /// UPSERT para SyncLedger (depende del provider).
    /// Usado para mantener la metadata del blockchain.
    /// </summary>
    public string BuildUpsertLedger()
    {
        return _provider.ToLower() switch
        {
            "oracle" => BuildOracleUpsert(),
            "sqlserver" => BuildSqlServerMerge(),
            "postgresql" => BuildPostgreSQLUpsert(),
            "mysql" => BuildMySQLUpsert(),
            "sqlite" => BuildSQLiteUpsert(),
            _ => throw new NotSupportedException($"Provider {_provider} not supported for UPSERT")
        };
    }

    /// <summary>
    /// Oracle: MERGE statement
    /// </summary>
    private string BuildOracleUpsert()
    {
        var pkCol = GetPrimaryKeyColumn();
        var allCols = _tableMapping.Columns.Values.Select(c => c.PhysicalName).ToList();

        var updateSet = string.Join(", ",
            allCols.Where(c => c != pkCol).Select(c => $"{c} = {GetParameterPlaceholder(c)}"));

        var insertCols = string.Join(", ", allCols);
        var insertVals = string.Join(", ", allCols.Select(c => GetParameterPlaceholder(c)));

        return $@"
MERGE INTO {_tableMapping.FullName} target
USING (SELECT {GetParameterPlaceholder(pkCol)} as {pkCol} FROM DUAL) source
ON (target.{pkCol} = source.{pkCol})
WHEN MATCHED THEN
    UPDATE SET {updateSet}
WHEN NOT MATCHED THEN
    INSERT ({insertCols}) VALUES ({insertVals})";
    }

    /// <summary>
    /// SQL Server: MERGE statement
    /// </summary>
    private string BuildSqlServerMerge()
    {
        var pkCol = GetPrimaryKeyColumn();
        var allCols = _tableMapping.Columns.Values.Select(c => c.PhysicalName).ToList();

        var updateSet = string.Join(", ",
            allCols.Where(c => c != pkCol).Select(c => $"{c} = {GetParameterPlaceholder(c)}"));

        var insertCols = string.Join(", ", allCols);
        var insertVals = string.Join(", ", allCols.Select(c => GetParameterPlaceholder(c)));

        return $@"
MERGE {_tableMapping.FullName} AS target
USING (SELECT {GetParameterPlaceholder(pkCol)} AS {pkCol}) AS source
ON target.{pkCol} = source.{pkCol}
WHEN MATCHED THEN
    UPDATE SET {updateSet}
WHEN NOT MATCHED THEN
    INSERT ({insertCols}) VALUES ({insertVals});";
    }

    /// <summary>
    /// PostgreSQL: INSERT ... ON CONFLICT DO UPDATE
    /// </summary>
    private string BuildPostgreSQLUpsert()
    {
        var pkCol = GetPrimaryKeyColumn();
        var allCols = _tableMapping.Columns.Values.Select(c => c.PhysicalName).ToList();

        var insertCols = string.Join(", ", allCols);
        var insertVals = string.Join(", ", allCols.Select(c => GetParameterPlaceholder(c)));
        var updateSet = string.Join(", ",
            allCols.Where(c => c != pkCol).Select(c => $"{c} = EXCLUDED.{c}"));

        return $@"
INSERT INTO {_tableMapping.FullName} ({insertCols})
VALUES ({insertVals})
ON CONFLICT ({pkCol})
DO UPDATE SET {updateSet}";
    }

    /// <summary>
    /// MySQL: INSERT ... ON DUPLICATE KEY UPDATE
    /// </summary>
    private string BuildMySQLUpsert()
    {
        var allCols = _tableMapping.Columns.Values.Select(c => c.PhysicalName).ToList();

        var insertCols = string.Join(", ", allCols);
        var insertVals = string.Join(", ", allCols.Select(c => GetParameterPlaceholder(c)));
        var updateSet = string.Join(", ", allCols.Select(c => $"{c} = VALUES({c})"));

        return $@"
INSERT INTO {_tableMapping.FullName} ({insertCols})
VALUES ({insertVals})
ON DUPLICATE KEY UPDATE {updateSet}";
    }

    /// <summary>
    /// SQLite: INSERT OR REPLACE
    /// </summary>
    private string BuildSQLiteUpsert()
    {
        var allCols = _tableMapping.Columns.Values.Select(c => c.PhysicalName).ToList();

        var insertCols = string.Join(", ", allCols);
        var insertVals = string.Join(", ", allCols.Select(c => GetParameterPlaceholder(c)));

        return $@"
INSERT OR REPLACE INTO {_tableMapping.FullName} ({insertCols})
VALUES ({insertVals})";
    }

    /// <summary>
    /// Retorna el placeholder correcto para parámetros según el provider.
    /// Oracle: :param, otros: @param
    /// </summary>
    public string GetParameterPlaceholder(string paramName)
    {
        return _provider.ToLower() switch
        {
            "oracle" => $":{paramName}",
            _ => $"@{paramName}"
        };
    }

    /// <summary>
    /// Obtiene el nombre físico de la columna de clave primaria.
    /// </summary>
    private string GetPrimaryKeyColumn()
    {
        var pkColumn = _tableMapping.Columns.Values
            .FirstOrDefault(c => c.IsPrimaryKey);

        if (pkColumn == null)
        {
            throw new InvalidOperationException(
                $"No primary key column defined for table {_tableMapping.PhysicalName}");
        }

        return pkColumn.PhysicalName;
    }
}
