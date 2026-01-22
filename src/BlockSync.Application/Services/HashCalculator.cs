using System.Security.Cryptography;
using System.Text;
using BlockSync.Domain.Entities;
using BlockSync.Domain.ValueObjects;

namespace BlockSync.Application.Services;

/// <summary>
/// Servicio para cálculo de hashes MD5 de bloques.
/// Usa la fórmula: MD5(SumaMonto + "|" + TotalRegistros)
/// Inspirado en Merkle Trees de blockchain para verificación de integridad.
/// </summary>
public class HashCalculator
{
    /// <summary>
    /// Calcula el hash MD5 de un conjunto de ventas
    /// </summary>
    /// <param name="ventas">Lista de ventas del bloque</param>
    /// <returns>Hash MD5 como string hexadecimal</returns>
    public static string CalculateHash(List<Venta> ventas)
    {
        if (!ventas.Any())
            return string.Empty;

        // Calcular suma total de montos
        var sumaMonto = ventas.Sum(v => v.Monto);
        var totalRegistros = ventas.Count;

        // Crear string para hashear: "suma|count"
        var blockData = $"{sumaMonto}|{totalRegistros}";

        // Calcular MD5
        using var md5 = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(blockData);
        var hashBytes = md5.ComputeHash(inputBytes);

        // Convertir a hexadecimal
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// Crea un BlockHeader a partir de un conjunto de ventas
    /// </summary>
    public static BlockHeader CreateBlockHeader(string periodo, List<Venta> ventas)
    {
        var sumaMonto = ventas.Sum(v => v.Monto);
        var totalRegistros = ventas.Count;
        var hash = CalculateHash(ventas);

        return new BlockHeader(periodo, hash, totalRegistros, sumaMonto);
    }
}
