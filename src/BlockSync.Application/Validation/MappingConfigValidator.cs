using BlockSync.Domain.Configuration;

namespace BlockSync.Application.Validation;

/// <summary>
/// Validador de configuración de mapeo de base de datos.
/// Verifica que la configuración sea correcta antes de usarla en runtime.
/// </summary>
public class MappingConfigValidator
{
    private static readonly HashSet<string> ValidProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Oracle", "SqlServer", "PostgreSQL", "MySQL", "SQLite"
    };

    private static readonly HashSet<string> RequiredColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "FechaVenta", "Cliente", "Producto", "Monto", "Periodo"
    };

    /// <summary>
    /// Resultado de validación con lista de errores.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Valida la configuración completa de mapeo.
    /// </summary>
    public ValidationResult Validate(DatabaseMappingConfiguration config)
    {
        var result = new ValidationResult();

        if (config == null)
        {
            result.Errors.Add("La configuración de mapeo es null");
            return result;
        }

        // Validar Source
        ValidateSystemConfig(config.Source, "Source", result);

        // Validar Destination
        ValidateSystemConfig(config.Destination, "Destination", result);

        // Validar transformaciones referenciadas
        ValidateTransformationReferences(config, result);

        return result;
    }

    /// <summary>
    /// Valida configuración de un sistema (Source o Destination).
    /// </summary>
    private void ValidateSystemConfig(
        DatabaseSystemConfig systemConfig,
        string systemName,
        ValidationResult result)
    {
        if (systemConfig == null)
        {
            result.Errors.Add($"{systemName}: Configuración del sistema es null");
            return;
        }

        // Validar Provider
        if (string.IsNullOrWhiteSpace(systemConfig.Provider))
        {
            result.Errors.Add($"{systemName}: Provider no puede estar vacío");
        }
        else if (!ValidProviders.Contains(systemConfig.Provider))
        {
            result.Errors.Add(
                $"{systemName}: Provider '{systemConfig.Provider}' no es válido. " +
                $"Valores permitidos: {string.Join(", ", ValidProviders)}");
        }

        // Validar ConnectionString
        if (string.IsNullOrWhiteSpace(systemConfig.ConnectionString))
        {
            result.Errors.Add($"{systemName}: ConnectionString no puede estar vacío");
        }
        else
        {
            ValidateConnectionString(systemConfig.ConnectionString, systemConfig.Provider, systemName, result);
        }

        // Validar que existe al menos una tabla
        if (systemConfig.Tables == null || !systemConfig.Tables.Any())
        {
            result.Errors.Add($"{systemName}: Debe tener al menos una tabla configurada");
            return;
        }

        // Validar cada tabla
        foreach (var (tableName, tableMapping) in systemConfig.Tables)
        {
            ValidateTableMapping(tableName, tableMapping, systemName, result);
        }
    }

    /// <summary>
    /// Valida el formato básico de una connection string.
    /// </summary>
    private void ValidateConnectionString(
        string connectionString,
        string provider,
        string systemName,
        ValidationResult result)
    {
        // Validaciones básicas según provider
        switch (provider.ToLower())
        {
            case "oracle":
                if (!connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"{systemName}: Oracle connection string debe contener 'Data Source'");
                }
                break;

            case "sqlserver":
                if (!connectionString.Contains("Server", StringComparison.OrdinalIgnoreCase)
                    && !connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"{systemName}: SQL Server connection string debe contener 'Server' o 'Data Source'");
                }
                break;

            case "postgresql":
                if (!connectionString.Contains("Host", StringComparison.OrdinalIgnoreCase)
                    && !connectionString.Contains("Server", StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"{systemName}: PostgreSQL connection string debe contener 'Host' o 'Server'");
                }
                break;

            case "mysql":
                if (!connectionString.Contains("Server", StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"{systemName}: MySQL connection string debe contener 'Server'");
                }
                break;

            case "sqlite":
                if (!connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"{systemName}: SQLite connection string debe contener 'Data Source'");
                }
                break;
        }
    }

    /// <summary>
    /// Valida mapeo de una tabla.
    /// </summary>
    private void ValidateTableMapping(
        string tableName,
        TableMapping tableMapping,
        string systemName,
        ValidationResult result)
    {
        var context = $"{systemName}.Tables[{tableName}]";

        if (tableMapping == null)
        {
            result.Errors.Add($"{context}: Configuración de tabla es null");
            return;
        }

        // Validar nombre físico
        if (string.IsNullOrWhiteSpace(tableMapping.PhysicalName))
        {
            result.Errors.Add($"{context}: PhysicalName no puede estar vacío");
        }

        // Validar que existen columnas
        if (tableMapping.Columns == null || !tableMapping.Columns.Any())
        {
            result.Errors.Add($"{context}: Debe tener al menos una columna configurada");
            return;
        }

        // Validar columnas requeridas (solo para tabla "Ventas")
        if (tableName.Equals("Ventas", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var requiredColumn in RequiredColumns)
            {
                if (!tableMapping.Columns.ContainsKey(requiredColumn))
                {
                    result.Errors.Add($"{context}: Falta mapeo de columna requerida '{requiredColumn}'");
                }
            }
        }

        // Validar cada columna
        foreach (var (columnName, columnMapping) in tableMapping.Columns)
        {
            ValidateColumnMapping(columnName, columnMapping, context, result);
        }

        // Validar que existe campo de agrupación de bloques
        if (string.IsNullOrWhiteSpace(tableMapping.BlockGroupingField))
        {
            result.Errors.Add($"{context}: BlockGroupingField no puede estar vacío");
        }
        else if (!tableMapping.Columns.ContainsKey(tableMapping.BlockGroupingField))
        {
            result.Errors.Add(
                $"{context}: BlockGroupingField '{tableMapping.BlockGroupingField}' " +
                $"no existe en las columnas mapeadas");
        }
    }

    /// <summary>
    /// Valida mapeo de una columna.
    /// </summary>
    private void ValidateColumnMapping(
        string columnName,
        ColumnMapping columnMapping,
        string context,
        ValidationResult result)
    {
        var columnContext = $"{context}.Columns[{columnName}]";

        if (columnMapping == null)
        {
            result.Errors.Add($"{columnContext}: Configuración de columna es null");
            return;
        }

        // Validar nombre físico
        if (string.IsNullOrWhiteSpace(columnMapping.PhysicalName))
        {
            result.Errors.Add($"{columnContext}: PhysicalName no puede estar vacío");
        }

        // Validar tipo de dato
        if (string.IsNullOrWhiteSpace(columnMapping.DataType))
        {
            result.Errors.Add($"{columnContext}: DataType no puede estar vacío");
        }
    }

    /// <summary>
    /// Valida que todas las transformaciones referenciadas existen.
    /// </summary>
    private void ValidateTransformationReferences(
        DatabaseMappingConfiguration config,
        ValidationResult result)
    {
        var definedTransformations = config.DataTransformations?.Keys.ToHashSet()
            ?? new HashSet<string>();

        // Validar transformaciones en Source
        ValidateTransformationsInSystem(config.Source, "Source", definedTransformations, result);

        // Validar transformaciones en Destination
        ValidateTransformationsInSystem(config.Destination, "Destination", definedTransformations, result);
    }

    /// <summary>
    /// Valida transformaciones en un sistema específico.
    /// </summary>
    private void ValidateTransformationsInSystem(
        DatabaseSystemConfig systemConfig,
        string systemName,
        HashSet<string> definedTransformations,
        ValidationResult result)
    {
        if (systemConfig?.Tables == null) return;

        foreach (var (tableName, tableMapping) in systemConfig.Tables)
        {
            if (tableMapping?.Columns == null) continue;

            foreach (var (columnName, columnMapping) in tableMapping.Columns)
            {
                if (string.IsNullOrWhiteSpace(columnMapping.Transformation))
                    continue;

                if (!definedTransformations.Contains(columnMapping.Transformation))
                {
                    result.Errors.Add(
                        $"{systemName}.Tables[{tableName}].Columns[{columnName}]: " +
                        $"Transformación '{columnMapping.Transformation}' no está definida en DataTransformations");
                }
            }
        }
    }
}
