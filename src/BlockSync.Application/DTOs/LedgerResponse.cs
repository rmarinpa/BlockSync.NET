using BlockSync.Domain.Entities;

namespace BlockSync.Application.DTOs;

/// <summary>
/// Respuesta del endpoint del ledger de sincronización
/// </summary>
public class LedgerResponse
{
    public DateTime Timestamp { get; set; }
    public string Sistema { get; set; } = "BlockSync.NET - Blockchain Ledger";
    public LedgerStats Stats { get; set; } = new();
    public List<LedgerEntryDto> Entradas { get; set; } = new();
}

/// <summary>
/// Estadísticas del ledger
/// </summary>
public class LedgerStats
{
    public int TotalBloques { get; set; }
    public int Sincronizados { get; set; }
    public int Corruptos { get; set; }
    public int Pendientes { get; set; }
    public int Errores { get; set; }
    public double PorcentajeSincronizado { get; set; }
}

/// <summary>
/// DTO para una entrada del ledger (simplificado para API)
/// </summary>
public class LedgerEntryDto
{
    public string Periodo { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime UltimaSync { get; set; }
    public int TotalRegistros { get; set; }
    public decimal SumaMonto { get; set; }
    public string UltimaAccion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Convierte un SyncLedgerEntry en LedgerEntryDto
    /// </summary>
    public static LedgerEntryDto FromEntity(SyncLedgerEntry entry)
    {
        return new LedgerEntryDto
        {
            Periodo = entry.PeriodoId,
            Hash = entry.Hash,
            Estado = entry.Estado.ToString(),
            UltimaSync = entry.UltimaSync,
            TotalRegistros = entry.TotalRegistros,
            SumaMonto = entry.SumaMonto,
            UltimaAccion = entry.UltimaAccion.ToString(),
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt
        };
    }
}
