namespace BlockSync.Domain.ValueObjects;

/// <summary>
/// Value Object que representa el encabezado de un bloque.
/// Inspirado en Blockchain, contiene metadata y hash del bloque sin los datos completos.
/// Permite comparación rápida sin necesidad de descargar todos los registros.
/// </summary>
public class BlockHeader
{
    /// <summary>
    /// Periodo del bloque en formato "yyyy-MM" (ej: "2023-03")
    /// </summary>
    public string Periodo { get; set; } = string.Empty;

    /// <summary>
    /// Hash MD5 calculado como: MD5(SumaMonto + "|" + TotalRegistros)
    /// Permite detectar cambios en el bloque sin descargar los datos completos
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad total de registros en el bloque
    /// </summary>
    public int TotalRegistros { get; set; }

    /// <summary>
    /// Suma total de todos los montos del bloque
    /// </summary>
    public decimal SumaMonto { get; set; }

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public BlockHeader() { }

    /// <summary>
    /// Constructor con parámetros para facilitar la creación
    /// </summary>
    public BlockHeader(string periodo, string hash, int totalRegistros, decimal sumaMonto)
    {
        Periodo = periodo;
        Hash = hash;
        TotalRegistros = totalRegistros;
        SumaMonto = sumaMonto;
    }

    /// <summary>
    /// Compara si dos headers son iguales basándose en el hash
    /// </summary>
    public bool IsSameAs(BlockHeader? other)
    {
        if (other == null) return false;
        return Hash == other.Hash && Periodo == other.Periodo;
    }
}
