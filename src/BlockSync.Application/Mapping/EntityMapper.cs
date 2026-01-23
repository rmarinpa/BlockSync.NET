using BlockSync.Domain.Configuration;
using System.Reflection;

namespace BlockSync.Application.Mapping;

/// <summary>
/// Mapeador genérico entre entidades del dominio y DTOs de base de datos.
/// NO usa AutoMapper - mapeo manual basado en configuración para mejor performance.
/// </summary>
public class EntityMapper<TEntity> where TEntity : class, new()
{
    private readonly TableMapping _tableMapping;
    private readonly Dictionary<string, TransformationConfig> _transformations;

    public EntityMapper(
        TableMapping tableMapping,
        Dictionary<string, TransformationConfig> transformations)
    {
        _tableMapping = tableMapping ?? throw new ArgumentNullException(nameof(tableMapping));
        _transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
    }

    /// <summary>
    /// Convierte un row de Dapper (dynamic) a entidad del dominio.
    /// </summary>
    /// <param name="row">Row dinámico de Dapper</param>
    /// <returns>Instancia de TEntity con datos mapeados</returns>
    public TEntity MapFromDatabase(dynamic row)
    {
        if (row == null)
            throw new ArgumentNullException(nameof(row));

        var entity = new TEntity();
        var entityType = typeof(TEntity);

        foreach (var property in entityType.GetProperties())
        {
            // Buscar mapeo de la columna
            if (!_tableMapping.Columns.TryGetValue(property.Name, out var columnMapping))
                continue;

            var physicalName = columnMapping.PhysicalName;
            var value = GetDynamicPropertyValue(row, physicalName);

            if (value == null)
            {
                // Si la propiedad no acepta null, no intentar asignar
                if (!IsNullable(property.PropertyType))
                    continue;

                property.SetValue(entity, null);
                continue;
            }

            // Aplicar transformación de lectura si existe
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
    /// Convierte una entidad del dominio a diccionario para Dapper (parámetros).
    /// </summary>
    /// <param name="entity">Instancia de TEntity</param>
    /// <returns>Diccionario con nombres físicos de columnas y valores transformados</returns>
    public Dictionary<string, object?> MapToDatabase(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var parameters = new Dictionary<string, object?>();
        var entityType = typeof(TEntity);

        foreach (var property in entityType.GetProperties())
        {
            // Buscar mapeo de la columna
            if (!_tableMapping.Columns.TryGetValue(property.Name, out var columnMapping))
                continue;

            var value = property.GetValue(entity);

            // Aplicar transformación de escritura si existe
            if (!string.IsNullOrEmpty(columnMapping.Transformation)
                && _transformations.TryGetValue(columnMapping.Transformation, out var transform))
            {
                value = ApplyWriteTransformation(value, transform);
            }

            // Usar nombre físico como key
            parameters[columnMapping.PhysicalName] = value;
        }

        return parameters;
    }

    /// <summary>
    /// Obtiene un valor de un objeto dinámico por nombre de propiedad.
    /// </summary>
    private object? GetDynamicPropertyValue(dynamic row, string propertyName)
    {
        try
        {
            // Intentar acceso como diccionario (más común con Dapper)
            if (row is IDictionary<string, object> dict)
            {
                // Buscar case-insensitive
                var key = dict.Keys.FirstOrDefault(k =>
                    string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));

                return key != null ? dict[key] : null;
            }

            // Fallback: acceso por reflexión
            var type = row.GetType();
            var prop = type.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            return prop?.GetValue(row);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Aplica transformación de lectura (BD → Dominio).
    /// </summary>
    private object? ApplyReadTransformation(object? value, TransformationConfig transform)
    {
        if (value == null) return null;

        // Transformaciones hardcoded por performance (más rápido que evaluar expresiones)
        switch (transform.Read)
        {
            case "value / 100m":
                // Centavos (long/int) → Decimal
                return Convert.ToDecimal(value) / 100m;

            case "value / 100.0m":
                return Convert.ToDecimal(value) / 100.0m;

            case "Guid.Parse(value)":
                // String → Guid
                return Guid.Parse(value?.ToString() ?? string.Empty);

            case "new Guid(value)":
                // byte[] → Guid
                if (value is byte[] bytes)
                    return new Guid(bytes);
                return value;

            case "DateTime.Parse(value)":
                // String → DateTime
                return DateTime.Parse(value?.ToString() ?? string.Empty);

            case "value.ToString()":
                return value?.ToString();

            default:
                // Si no hay transformación conocida, retornar el valor original
                return value;
        }
    }

    /// <summary>
    /// Aplica transformación de escritura (Dominio → BD).
    /// </summary>
    private object? ApplyWriteTransformation(object? value, TransformationConfig transform)
    {
        if (value == null) return null;

        // Transformaciones hardcoded por performance
        switch (transform.Write)
        {
            case "(long)(value * 100)":
                // Decimal → Centavos (long)
                return (long)(Convert.ToDecimal(value) * 100);

            case "(int)(value * 100)":
                // Decimal → Centavos (int)
                return (int)(Convert.ToDecimal(value) * 100);

            case "value.ToString()":
                // Guid/DateTime → String
                return value?.ToString();

            case "value.ToByteArray()":
                // Guid → byte[]
                if (value is Guid guid)
                    return guid.ToByteArray();
                return value;

            case "value.ToString(\"yyyy-MM-ddTHH:mm:ss\")":
                // DateTime → ISO String
                if (value is DateTime dt)
                    return dt.ToString("yyyy-MM-ddTHH:mm:ss");
                return value;

            default:
                return value;
        }
    }

    /// <summary>
    /// Convierte un valor al tipo objetivo si es necesario.
    /// </summary>
    private object? ConvertType(object? value, Type targetType)
    {
        if (value == null) return null;

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Si ya es del tipo correcto, no convertir
        if (underlyingType.IsInstanceOfType(value))
            return value;

        // Conversiones especiales
        if (underlyingType == typeof(Guid))
        {
            if (value is byte[] bytes)
                return new Guid(bytes);
            if (value is string str)
                return Guid.Parse(str);
        }

        if (underlyingType == typeof(DateTime))
        {
            if (value is string str)
                return DateTime.Parse(str);
        }

        if (underlyingType == typeof(decimal))
        {
            return Convert.ToDecimal(value);
        }

        // Conversión estándar
        try
        {
            return Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            // Si falla la conversión, retornar el valor original
            return value;
        }
    }

    /// <summary>
    /// Verifica si un tipo es nullable (Nullable<T> o tipo de referencia).
    /// </summary>
    private bool IsNullable(Type type)
    {
        if (!type.IsValueType) return true; // Tipos de referencia son nullable
        return Nullable.GetUnderlyingType(type) != null; // Nullable<T>
    }
}
