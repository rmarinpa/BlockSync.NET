namespace BlockSync.Domain.Configuration;

/// <summary>
/// Configuración completa de mapeo de base de datos.
/// Define cómo mapear entidades del dominio a esquemas físicos de diferentes motores de BD.
/// </summary>
public class DatabaseMappingConfiguration
{
    public DatabaseSystemConfig Source { get; set; } = new();
    public DatabaseSystemConfig Destination { get; set; } = new();
    public Dictionary<string, TransformationConfig> DataTransformations { get; set; } = new();
}

/// <summary>
/// Configuración de un sistema de base de datos (Source o Destination).
/// </summary>
public class DatabaseSystemConfig
{
    /// <summary>
    /// Proveedor de base de datos: Oracle, SqlServer, PostgreSQL, MySQL, SQLite
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Cadena de conexión completa
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Mapeo de tablas por nombre lógico (ej: "Ventas" → configuración física)
    /// </summary>
    public Dictionary<string, TableMapping> Tables { get; set; } = new();
}

/// <summary>
/// Mapeo de una tabla del dominio a su representación física en la base de datos.
/// </summary>
public class TableMapping
{
    /// <summary>
    /// Nombre físico de la tabla en la base de datos (ej: "TB_VENDAS", "SALES_TRANSACTIONS")
    /// </summary>
    public string PhysicalName { get; set; } = string.Empty;

    /// <summary>
    /// Schema de la tabla (ej: "dbo", "LEGACY_SCHEMA", "public")
    /// </summary>
    public string Schema { get; set; } = "dbo";

    /// <summary>
    /// Mapeo de columnas por nombre lógico (ej: "Id" → configuración física)
    /// </summary>
    public Dictionary<string, ColumnMapping> Columns { get; set; } = new();

    /// <summary>
    /// Índices definidos en la tabla
    /// </summary>
    public List<IndexDefinition> Indexes { get; set; } = new();

    /// <summary>
    /// Campo que se usa para agrupar bloques temporales (ej: "Periodo" para agrupación mensual)
    /// </summary>
    public string BlockGroupingField { get; set; } = "Periodo";

    /// <summary>
    /// Retorna el nombre completo calificado: Schema.TableName
    /// </summary>
    public string FullName =>
        string.IsNullOrEmpty(Schema) ? PhysicalName : $"{Schema}.{PhysicalName}";
}

/// <summary>
/// Mapeo de una columna del dominio a su representación física en la base de datos.
/// </summary>
public class ColumnMapping
{
    /// <summary>
    /// Nombre físico de la columna en la base de datos (ej: "ID_VENDA", "SALE_ID")
    /// </summary>
    public string PhysicalName { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de dato nativo de la base de datos (ej: "RAW(16)", "UNIQUEIDENTIFIER", "NUMBER(18,0)")
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Indica si esta columna es clave primaria
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Nombre de la transformación a aplicar (referencia a DataTransformations)
    /// Ej: "CentavosToDecimal", "StringToGuid", "TimestampToDateTime"
    /// </summary>
    public string? Transformation { get; set; }

    /// <summary>
    /// Indica si la columna permite NULL
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Valor por defecto en la base de datos (si aplica)
    /// </summary>
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Definición de un índice en la base de datos.
/// </summary>
public class IndexDefinition
{
    /// <summary>
    /// Nombre del índice (ej: "IDX_SALES_PERIOD", "PK_VENDAS")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Columnas que componen el índice (nombres físicos)
    /// </summary>
    public List<string> Columns { get; set; } = new();

    /// <summary>
    /// Indica si es un índice único
    /// </summary>
    public bool IsUnique { get; set; }
}

/// <summary>
/// Configuración de transformación de datos entre el dominio y la base de datos.
/// </summary>
public class TransformationConfig
{
    /// <summary>
    /// Expresión para transformar al leer de la base de datos hacia el dominio.
    /// Ej: "value / 100m" (convierte centavos a decimal)
    /// </summary>
    public string Read { get; set; } = string.Empty;

    /// <summary>
    /// Expresión para transformar al escribir del dominio hacia la base de datos.
    /// Ej: "(long)(value * 100)" (convierte decimal a centavos)
    /// </summary>
    public string Write { get; set; } = string.Empty;
}
