using System.Text;

namespace BlockSync.Application.DTOs;

/// <summary>
/// Reporte completo de una operación de sincronización.
/// Contiene resumen agregado y detalles por bloque.
/// </summary>
public class SyncReport
{
    /// <summary>
    /// Timestamp de cuando se ejecutó la sincronización
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duración total de la sincronización en milisegundos
    /// </summary>
    public long DuracionMs { get; set; }

    /// <summary>
    /// Total de bloques procesados
    /// </summary>
    public int TotalBloques { get; set; }

    /// <summary>
    /// Bloques omitidos (SKIP) porque no tenían cambios
    /// </summary>
    public int BloquesOmitidos { get; set; }

    /// <summary>
    /// Bloques insertados (INSERT) porque no existían en destino
    /// </summary>
    public int BloquesInsertados { get; set; }

    /// <summary>
    /// Bloques reparados (REPAIR) porque estaban corruptos
    /// </summary>
    public int BloquesReparados { get; set; }

    /// <summary>
    /// Total de registros procesados en todos los bloques
    /// </summary>
    public int TotalRegistrosProcesados { get; set; }

    /// <summary>
    /// Total de registros insertados (excluye los SKIP)
    /// </summary>
    public int TotalRegistrosInsertados { get; set; }

    /// <summary>
    /// Lista detallada de resultados por cada bloque
    /// </summary>
    public List<BlockSyncResult> DetallesBloques { get; set; } = new();

    /// <summary>
    /// Genera un resumen en texto ASCII para logs/consola
    /// </summary>
    public string GenerarResumenTexto()
    {
        var sb = new StringBuilder();
        sb.AppendLine("╔════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║           REPORTE DE SINCRONIZACIÓN - BLOCKSYNC.NET       ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"⏱️  Duración: {DuracionMs} ms");
        sb.AppendLine($"📦 Total Bloques: {TotalBloques}");
        sb.AppendLine($"   ├─ ⏭️  Omitidos (SKIP):    {BloquesOmitidos}");
        sb.AppendLine($"   ├─ ➕ Insertados (INSERT): {BloquesInsertados}");
        sb.AppendLine($"   └─ 🔧 Reparados (REPAIR):  {BloquesReparados}");
        sb.AppendLine();
        sb.AppendLine($"📊 Registros Procesados: {TotalRegistrosProcesados:N0}");
        sb.AppendLine($"💾 Registros Insertados: {TotalRegistrosInsertados:N0}");
        sb.AppendLine();

        if (DetallesBloques.Any())
        {
            sb.AppendLine("📋 DETALLE POR BLOQUE:");
            sb.AppendLine("────────────────────────────────────────────────────────────");
            foreach (var detalle in DetallesBloques.OrderBy(d => d.Periodo))
            {
                var icono = detalle.Accion switch
                {
                    SyncActionType.SKIP => "⏭️ ",
                    SyncActionType.INSERT => "➕",
                    SyncActionType.REPAIR => "🔧",
                    _ => "❓"
                };
                sb.AppendLine($"{icono} {detalle.Periodo} | {detalle.Accion,-6} | {detalle.RegistrosProcesados,5} registros | {detalle.DuracionMs,4} ms");
            }
        }

        sb.AppendLine("════════════════════════════════════════════════════════════");
        return sb.ToString();
    }
}
