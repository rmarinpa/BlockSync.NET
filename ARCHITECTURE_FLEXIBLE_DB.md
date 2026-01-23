# Arquitectura de Mapeo Flexible para Cualquier Base de Datos

## Objetivo

Permitir que BlockSync.NET se conecte a **cualquier base de datos** (Oracle, SQL Server, MySQL, PostgreSQL, DB2, etc.) donde:
- Las tablas pueden tener **nombres diferentes** (ej: `SALES`, `TB_VENDAS`, `tbl_Ventas`)
- Las columnas pueden tener **nombres diferentes** (ej: `sale_id`, `ID_VENDA`, `VentaID`)
- Los tipos de datos pueden variar
- **Sin usar AutoMapper** - mapeo manual basado en configuración JSON

---

## Solución: Sistema de Mapeo Declarativo por Configuración

### Componentes Principales

```
┌─────────────────────────────────────────────────────────────┐
│                   appsettings.json                          │
│              (Database Mapping Config)                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│            DatabaseMappingConfiguration.cs                  │
│   (Lee configuración y valida mapeos)                       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  DynamicQueryBuilder.cs                     │
│   (Construye queries SQL dinámicas basadas en mapeo)       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                  EntityMapper<TEntity>.cs                   │
│   (Mapea entre entidad Domain y DTO de base de datos)      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          GenericRepository<TEntity>.cs                      │
│   (Repositorio genérico que usa QueryBuilder + Mapper)     │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. Archivo de Configuración (appsettings.json)

```json
{
  "DatabaseMappings": {
    "Source": {
      "Provider": "Oracle",
      "ConnectionString": "Data Source=oracle-prod:1521/ORCL;User Id=legacy;Password=***;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "SALES_TRANSACTIONS",
          "Schema": "LEGACY_SCHEMA",
          "Columns": {
            "Id": {
              "PhysicalName": "SALE_ID",
              "DataType": "RAW(16)",
              "IsPrimaryKey": true
            },
            "FechaVenta": {
              "PhysicalName": "SALE_DATE",
              "DataType": "DATE"
            },
            "Cliente": {
              "PhysicalName": "CUSTOMER_NAME",
              "DataType": "VARCHAR2(200)"
            },
            "Producto": {
              "PhysicalName": "PRODUCT_NAME",
              "DataType": "VARCHAR2(200)"
            },
            "Monto": {
              "PhysicalName": "AMOUNT_CENTS",
              "DataType": "NUMBER(18,0)",
              "Transformation": "CentavosToDecimal"
            },
            "Periodo": {
              "PhysicalName": "PERIOD_ID",
              "DataType": "VARCHAR2(7)"
            }
          },
          "Indexes": [
            { "Name": "IDX_SALES_PERIOD", "Columns": ["PERIOD_ID"] },
            { "Name": "IDX_SALES_DATE", "Columns": ["SALE_DATE"] }
          ]
        },
        "SyncLedger": {
          "PhysicalName": "SYNC_BLOCKCHAIN_LEDGER",
          "Schema": "SYNC_SCHEMA",
          "Columns": {
            "PeriodoId": { "PhysicalName": "BLOCK_PERIOD", "DataType": "VARCHAR2(7)", "IsPrimaryKey": true },
            "Hash": { "PhysicalName": "BLOCK_HASH", "DataType": "VARCHAR2(32)" },
            "Estado": { "PhysicalName": "SYNC_STATUS", "DataType": "VARCHAR2(20)" },
            "UltimaSync": { "PhysicalName": "LAST_SYNC_TIME", "DataType": "TIMESTAMP" },
            "TotalRegistros": { "PhysicalName": "TOTAL_RECORDS", "DataType": "NUMBER(10,0)" },
            "SumaMonto": { "PhysicalName": "SUM_AMOUNT_CENTS", "DataType": "NUMBER(18,0)", "Transformation": "CentavosToDecimal" },
            "UltimaAccion": { "PhysicalName": "LAST_ACTION", "DataType": "VARCHAR2(10)" },
            "CreatedAt": { "PhysicalName": "CREATED_DATE", "DataType": "TIMESTAMP" },
            "UpdatedAt": { "PhysicalName": "UPDATED_DATE", "DataType": "TIMESTAMP" }
          }
        }
      }
    },
    "Destination": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=BlockSyncLocal;Integrated Security=true;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "Sales",
          "Schema": "dbo",
          "Columns": {
            "Id": { "PhysicalName": "SaleId", "DataType": "UNIQUEIDENTIFIER", "IsPrimaryKey": true },
            "FechaVenta": { "PhysicalName": "SaleDate", "DataType": "DATETIME2" },
            "Cliente": { "PhysicalName": "CustomerName", "DataType": "NVARCHAR(200)" },
            "Producto": { "PhysicalName": "ProductName", "DataType": "NVARCHAR(200)" },
            "Monto": { "PhysicalName": "AmountCents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "PeriodId", "DataType": "VARCHAR(7)" }
          }
        },
        "SyncLedger": {
          "PhysicalName": "SyncLedger",
          "Schema": "dbo",
          "Columns": {
            "PeriodoId": { "PhysicalName": "PeriodId", "DataType": "VARCHAR(7)", "IsPrimaryKey": true },
            "Hash": { "PhysicalName": "BlockHash", "DataType": "VARCHAR(32)" },
            "Estado": { "PhysicalName": "Status", "DataType": "VARCHAR(20)" },
            "UltimaSync": { "PhysicalName": "LastSyncTime", "DataType": "DATETIME2" },
            "TotalRegistros": { "PhysicalName": "TotalRecords", "DataType": "INT" },
            "SumaMonto": { "PhysicalName": "SumAmountCents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "UltimaAccion": { "PhysicalName": "LastAction", "DataType": "VARCHAR(10)" },
            "CreatedAt": { "PhysicalName": "CreatedAt", "DataType": "DATETIME2" },
            "UpdatedAt": { "PhysicalName": "UpdatedAt", "DataType": "DATETIME2" }
          }
        }
      }
    }
  },

  "DataTransformations": {
    "CentavosToDecimal": {
      "Read": "value / 100m",
      "Write": "(long)(value * 100)"
    },
    "StringToGuid": {
      "Read": "Guid.Parse(value)",
      "Write": "value.ToString()"
    }
  }
}
```

---

## 2. Clases de Configuración

### DatabaseMappingConfiguration.cs

```csharp
namespace BlockSync.Infrastructure.Configuration;

/// <summary>
/// Configuración completa de mapeo de base de datos
/// </summary>
public class DatabaseMappingConfiguration
{
    public DatabaseSystemConfig Source { get; set; } = new();
    public DatabaseSystemConfig Destination { get; set; } = new();
    public Dictionary<string, TransformationConfig> DataTransformations { get; set; } = new();
}

/// <summary>
/// Configuración de un sistema de base de datos (Source o Destination)
/// </summary>
public class DatabaseSystemConfig
{
    public string Provider { get; set; } = string.Empty; // Oracle, SqlServer, PostgreSQL, MySQL
    public string ConnectionString { get; set; } = string.Empty;
    public Dictionary<string, TableMapping> Tables { get; set; } = new();
}

/// <summary>
/// Mapeo de una tabla (Ventas o SyncLedger)
/// </summary>
public class TableMapping
{
    public string PhysicalName { get; set; } = string.Empty;  // Nombre real en la BD
    public string Schema { get; set; } = "dbo";                // Schema (LEGACY_SCHEMA, dbo, public, etc.)
    public Dictionary<string, ColumnMapping> Columns { get; set; } = new();
    public List<IndexDefinition> Indexes { get; set; } = new();

    /// <summary>
    /// Retorna el nombre completo: Schema.TableName
    /// </summary>
    public string FullName => string.IsNullOrEmpty(Schema) ? PhysicalName : $"{Schema}.{PhysicalName}";
}

/// <summary>
/// Mapeo de una columna
/// </summary>
public class ColumnMapping
{
    public string PhysicalName { get; set; } = string.Empty;  // Nombre real en la BD
    public string DataType { get; set; } = string.Empty;       // Tipo de dato nativo (VARCHAR2, NVARCHAR, etc.)
    public bool IsPrimaryKey { get; set; }
    public string? Transformation { get; set; }                // Nombre de transformación (ej: CentavosToDecimal)
}

/// <summary>
/// Definición de índice
/// </summary>
public class IndexDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
}

/// <summary>
/// Configuración de transformación de datos
/// </summary>
public class TransformationConfig
{
    public string Read { get; set; } = string.Empty;   // Expresión para leer de BD (ej: "value / 100m")
    public string Write { get; set; } = string.Empty;  // Expresión para escribir a BD (ej: "(long)(value * 100)")
}
```

---

## 3. EntityMapper<TEntity> - Mapeo Manual Sin AutoMapper

```csharp
namespace BlockSync.Infrastructure.Mapping;

/// <summary>
/// Mapeador genérico entre entidades del dominio y DTOs de base de datos.
/// NO usa AutoMapper - mapeo manual basado en configuración.
/// </summary>
public class EntityMapper<TEntity> where TEntity : class, new()
{
    private readonly TableMapping _tableMapping;
    private readonly Dictionary<string, TransformationConfig> _transformations;

    public EntityMapper(
        TableMapping tableMapping,
        Dictionary<string, TransformationConfig> transformations)
    {
        _tableMapping = tableMapping;
        _transformations = transformations;
    }

    /// <summary>
    /// Convierte un row de Dapper (dynamic) a entidad del dominio
    /// </summary>
    public TEntity MapFromDatabase(dynamic row)
    {
        var entity = new TEntity();
        var entityType = typeof(TEntity);

        foreach (var property in entityType.GetProperties())
        {
            if (!_tableMapping.Columns.TryGetValue(property.Name, out var columnMapping))
                continue;

            var physicalName = columnMapping.PhysicalName;
            var value = GetDynamicPropertyValue(row, physicalName);

            if (value == null)
                continue;

            // Aplicar transformación si existe
            if (!string.IsNullOrEmpty(columnMapping.Transformation)
                && _transformations.TryGetValue(columnMapping.Transformation, out var transform))
            {
                value = ApplyReadTransformation(value, transform);
            }

            // Convertir tipo si es necesario
            value = ConvertType(value, property.PropertyType);

            property.SetValue(entity, value);
        }

        return entity;
    }

    /// <summary>
    /// Convierte una entidad del dominio a diccionario para Dapper (parametros)
    /// </summary>
    public Dictionary<string, object?> MapToDatabase(TEntity entity)
    {
        var parameters = new Dictionary<string, object?>();
        var entityType = typeof(TEntity);

        foreach (var property in entityType.GetProperties())
        {
            if (!_tableMapping.Columns.TryGetValue(property.Name, out var columnMapping))
                continue;

            var value = property.GetValue(entity);

            // Aplicar transformación si existe
            if (!string.IsNullOrEmpty(columnMapping.Transformation)
                && _transformations.TryGetValue(columnMapping.Transformation, out var transform))
            {
                value = ApplyWriteTransformation(value, transform);
            }

            parameters[columnMapping.PhysicalName] = value;
        }

        return parameters;
    }

    private object? GetDynamicPropertyValue(dynamic row, string propertyName)
    {
        try
        {
            var dict = (IDictionary<string, object>)row;
            return dict.ContainsKey(propertyName) ? dict[propertyName] : null;
        }
        catch
        {
            return null;
        }
    }

    private object? ApplyReadTransformation(object? value, TransformationConfig transform)
    {
        // Transformaciones comunes hardcoded por performance
        switch (transform.Read)
        {
            case "value / 100m":
                return Convert.ToDecimal(value) / 100m;

            case "Guid.Parse(value)":
                return Guid.Parse(value?.ToString() ?? string.Empty);

            default:
                // Podrías usar Roslyn o DynamicExpresso para evaluar expresiones dinámicas
                return value;
        }
    }

    private object? ApplyWriteTransformation(object? value, TransformationConfig transform)
    {
        switch (transform.Write)
        {
            case "(long)(value * 100)":
                return (long)(Convert.ToDecimal(value) * 100);

            case "value.ToString()":
                return value?.ToString();

            default:
                return value;
        }
    }

    private object? ConvertType(object? value, Type targetType)
    {
        if (value == null) return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Conversiones especiales
        if (underlyingType == typeof(Guid) && value is byte[] bytes)
        {
            return new Guid(bytes);
        }

        return Convert.ChangeType(value, underlyingType);
    }
}
```

---

## 4. DynamicQueryBuilder - Construcción de Queries Dinámicas

```csharp
namespace BlockSync.Infrastructure.Query;

/// <summary>
/// Constructor de queries SQL dinámicas basadas en mapeo de configuración.
/// Soporta diferentes dialectos: Oracle, SQL Server, PostgreSQL, MySQL.
/// </summary>
public class DynamicQueryBuilder
{
    private readonly TableMapping _tableMapping;
    private readonly string _provider;

    public DynamicQueryBuilder(TableMapping tableMapping, string provider)
    {
        _tableMapping = tableMapping;
        _provider = provider;
    }

    /// <summary>
    /// SELECT * FROM tabla WHERE periodo = @periodo
    /// </summary>
    public string BuildSelectByPeriod()
    {
        var columns = string.Join(", ", _tableMapping.Columns.Values.Select(c => c.PhysicalName));
        var periodoColumn = _tableMapping.Columns["Periodo"].PhysicalName;

        return $@"
            SELECT {columns}
            FROM {_tableMapping.FullName}
            WHERE {periodoColumn} = {GetParameterPlaceholder("periodo")}";
    }

    /// <summary>
    /// SELECT Periodo, COUNT(*), SUM(Monto) FROM tabla GROUP BY Periodo
    /// </summary>
    public string BuildBlockHeaders()
    {
        var periodoCol = _tableMapping.Columns["Periodo"].PhysicalName;
        var montoCol = _tableMapping.Columns["Monto"].PhysicalName;

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
        var parameters = string.Join(", ", _tableMapping.Columns.Values.Select(c => GetParameterPlaceholder(c.PhysicalName)));

        return $@"
            INSERT INTO {_tableMapping.FullName} ({columns})
            VALUES ({parameters})";
    }

    /// <summary>
    /// DELETE FROM tabla WHERE periodo = @periodo
    /// </summary>
    public string BuildDeleteByPeriod()
    {
        var periodoCol = _tableMapping.Columns["Periodo"].PhysicalName;

        return $@"
            DELETE FROM {_tableMapping.FullName}
            WHERE {periodoCol} = {GetParameterPlaceholder("periodo")}";
    }

    /// <summary>
    /// UPSERT para SyncLedger (depende del provider)
    /// </summary>
    public string BuildUpsertLedger()
    {
        switch (_provider.ToLower())
        {
            case "oracle":
                return BuildOracleUpsert();

            case "sqlserver":
                return BuildSqlServerMerge();

            case "postgresql":
                return BuildPostgreSQLUpsert();

            case "mysql":
                return BuildMySQLUpsert();

            case "sqlite":
                return BuildSQLiteUpsert();

            default:
                throw new NotSupportedException($"Provider {_provider} not supported");
        }
    }

    private string BuildOracleUpsert()
    {
        var pkCol = _tableMapping.Columns["PeriodoId"].PhysicalName;
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

    private string BuildSqlServerMerge()
    {
        // Similar a Oracle pero con sintaxis SQL Server
        var pkCol = _tableMapping.Columns["PeriodoId"].PhysicalName;
        // ... implementación similar
        return ""; // TODO
    }

    private string BuildPostgreSQLUpsert()
    {
        var pkCol = _tableMapping.Columns["PeriodoId"].PhysicalName;
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
    /// Retorna el placeholder correcto para parámetros según el provider
    /// Oracle: :param, SQL Server/SQLite: @param, PostgreSQL: @param, MySQL: @param
    /// </summary>
    private string GetParameterPlaceholder(string paramName)
    {
        switch (_provider.ToLower())
        {
            case "oracle":
                return $":{paramName}";
            default:
                return $"@{paramName}";
        }
    }
}
```

---

## 5. GenericRepository - Repositorio Genérico Configurable

```csharp
namespace BlockSync.Infrastructure.Repositories;

/// <summary>
/// Repositorio genérico que funciona con cualquier base de datos
/// usando mapeo por configuración
/// </summary>
public class GenericSyncRepository<TEntity> : ISyncSource, ISyncDestination
    where TEntity : class, new()
{
    private readonly string _connectionString;
    private readonly TableMapping _tableMapping;
    private readonly EntityMapper<TEntity> _mapper;
    private readonly DynamicQueryBuilder _queryBuilder;
    private readonly IDbConnection _connection;

    public GenericSyncRepository(
        DatabaseSystemConfig config,
        string tableName,
        Dictionary<string, TransformationConfig> transformations)
    {
        _connectionString = config.ConnectionString;
        _tableMapping = config.Tables[tableName];
        _mapper = new EntityMapper<TEntity>(_tableMapping, transformations);
        _queryBuilder = new DynamicQueryBuilder(_tableMapping, config.Provider);
        _connection = CreateConnection(config.Provider, _connectionString);
    }

    public async Task<List<BlockHeader>> GetBlockHeadersAsync()
    {
        var sql = _queryBuilder.BuildBlockHeaders();

        var rows = await _connection.QueryAsync<dynamic>(sql);

        return rows.Select(row => new BlockHeader
        {
            Periodo = row.Periodo,
            TotalRegistros = row.TotalRegistros,
            SumaMonto = row.SumaMontoCentavos / 100m  // Siempre almacenamos en centavos
        }).ToList();
    }

    public async Task<List<Venta>> GetBlockDataAsync(string periodo)
    {
        var sql = _queryBuilder.BuildSelectByPeriod();

        var rows = await _connection.QueryAsync<dynamic>(sql, new { periodo });

        return rows.Select(row => _mapper.MapFromDatabase(row)).Cast<Venta>().ToList();
    }

    public async Task InsertBlockAsync(List<Venta> ventas)
    {
        var sql = _queryBuilder.BuildInsert();

        using var transaction = _connection.BeginTransaction();

        foreach (var venta in ventas)
        {
            var parameters = _mapper.MapToDatabase(venta as TEntity);
            await _connection.ExecuteAsync(sql, parameters, transaction);
        }

        transaction.Commit();
    }

    public async Task DeleteBlockAsync(string periodo)
    {
        var sql = _queryBuilder.BuildDeleteByPeriod();
        await _connection.ExecuteAsync(sql, new { periodo });
    }

    // ... más métodos ...

    private IDbConnection CreateConnection(string provider, string connectionString)
    {
        return provider.ToLower() switch
        {
            "oracle" => new OracleConnection(connectionString),
            "sqlserver" => new SqlConnection(connectionString),
            "postgresql" => new NpgsqlConnection(connectionString),
            "mysql" => new MySqlConnection(connectionString),
            "sqlite" => new SqliteConnection(connectionString),
            _ => throw new NotSupportedException($"Provider {provider} not supported")
        };
    }
}
```

---

## 6. Configuración en Program.cs

```csharp
// Leer configuración de mapeo
var mappingConfig = builder.Configuration
    .GetSection("DatabaseMappings")
    .Get<DatabaseMappingConfiguration>();

// Registrar repositorios genéricos con configuración
builder.Services.AddScoped<ISyncSource>(sp =>
    new GenericSyncRepository<Venta>(
        mappingConfig.Source,
        "Ventas",
        mappingConfig.DataTransformations
    )
);

builder.Services.AddScoped<ISyncDestination>(sp =>
    new GenericSyncRepository<Venta>(
        mappingConfig.Destination,
        "Ventas",
        mappingConfig.DataTransformations
    )
);
```

---

## 7. Ejemplos de Configuración para Diferentes Bases de Datos

### Ejemplo: Oracle Legacy System → PostgreSQL Local

```json
{
  "DatabaseMappings": {
    "Source": {
      "Provider": "Oracle",
      "ConnectionString": "Data Source=legacy:1521/PROD;User Id=app;Password=***;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "TB_VENDAS_HISTORICO",
          "Schema": "LEGACY",
          "Columns": {
            "Id": { "PhysicalName": "ID_VENDA", "DataType": "RAW(16)" },
            "FechaVenta": { "PhysicalName": "DT_VENDA", "DataType": "DATE" },
            "Cliente": { "PhysicalName": "NM_CLIENTE", "DataType": "VARCHAR2(200)" },
            "Producto": { "PhysicalName": "NM_PRODUTO", "DataType": "VARCHAR2(200)" },
            "Monto": { "PhysicalName": "VL_CENTAVOS", "DataType": "NUMBER(18,0)", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "CD_PERIODO", "DataType": "VARCHAR2(7)" }
          }
        }
      }
    },
    "Destination": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=localhost;Database=blocksync;Username=postgres;Password=***;",
      "Tables": {
        "Ventas": {
          "PhysicalName": "sales",
          "Schema": "public",
          "Columns": {
            "Id": { "PhysicalName": "sale_id", "DataType": "UUID" },
            "FechaVenta": { "PhysicalName": "sale_date", "DataType": "TIMESTAMP" },
            "Cliente": { "PhysicalName": "customer_name", "DataType": "VARCHAR(200)" },
            "Producto": { "PhysicalName": "product_name", "DataType": "VARCHAR(200)" },
            "Monto": { "PhysicalName": "amount_cents", "DataType": "BIGINT", "Transformation": "CentavosToDecimal" },
            "Periodo": { "PhysicalName": "period_id", "DataType": "VARCHAR(7)" }
          }
        }
      }
    }
  }
}
```

---

## Ventajas de Esta Arquitectura

✅ **Zero Code Changes**: Solo cambiar appsettings.json para nueva BD
✅ **Multibase**: Soporta Oracle, SQL Server, PostgreSQL, MySQL, SQLite
✅ **Sin AutoMapper**: Mapeo manual optimizado por performance
✅ **Validación**: Configuración se valida al iniciar la app
✅ **Testeable**: Fácil de hacer unit tests con configuración mock
✅ **Flexible**: Soporta transformaciones personalizadas
✅ **Clean Architecture**: Mantiene separación de capas

---

## Próximos Pasos

1. **Implementar clases de configuración**
2. **Crear DynamicQueryBuilder con todos los providers**
3. **Implementar EntityMapper genérico**
4. **Crear GenericRepository**
5. **Agregar validación de configuración al startup**
6. **Crear herramienta CLI para generar configuración desde schema existente**

