// Auto-generated types from OpenAPI specification

export interface SystemStatusResponse {
  timestamp: string;
  sistema: string;
  version: string;
  origen: SystemInfo;
  destino: SystemInfo;
  estadoSincronizacion: string;
}

export interface SystemInfo {
  registros: number;
  bloques: number;
  periodos: string[];
}

export interface SyncResponse {
  exitoso: boolean;
  resumen: SyncSummary;
  reporte: SyncReport;
  resumenTexto: string;
}

export interface SyncSummary {
  duracionMs: number;
  totalBloques: number;
  bloquesOmitidos: number;
  bloquesInsertados: number;
  bloquesReparados: number;
  totalRegistrosProcesados: number;
  totalRegistrosInsertados: number;
}

export interface SyncReport {
  timestamp: string;
  duracionMs: number;
  totalBloques: number;
  bloquesOmitidos: number;
  bloquesInsertados: number;
  bloquesReparados: number;
  totalRegistrosProcesados: number;
  totalRegistrosInsertados: number;
  detallesBloques: BlockSyncResult[];
}

export interface BlockSyncResult {
  periodo: string;
  accion: SyncActionType;
  hashOrigen: string;
  hashDestinoAntes: string | null;
  registrosProcesados: number;
  registrosInsertados: number;
  mensaje: string;
  duracionMs: number;
}

export enum SyncActionType {
  SKIP = 0,
  INSERT = 1,
  REPAIR = 2,
}

export interface LedgerResponse {
  timestamp: string;
  sistema: string;
  stats: LedgerStats;
  entradas: LedgerEntryDto[];
}

export interface LedgerStats {
  totalBloques: number;
  sincronizados: number;
  corruptos: number;
  pendientes: number;
  errores: number;
  porcentajeSincronizado: number;
}

export interface LedgerEntryDto {
  periodo: string;
  hash: string;
  estado: string;
  ultimaSync: string;
  totalRegistros: number;
  sumaMonto: number;
  ultimaAccion: string;
  createdAt: string;
  updatedAt: string;
}

export interface HashComparisonResponse {
  timestamp: string;
  totalPeriodos: number;
  sincronizados: number;
  faltanEnDestino: number;
  corruptos: number;
  bloques: BlockComparison[];
}

export interface BlockComparison {
  periodo: string;
  estado: string;
  origen: BlockInfo;
  destino: BlockInfo;
}

export interface BlockInfo {
  hash: string;
  registros: number;
  monto: number;
}

export interface DiagnosticsResponse {
  timestamp: string;
  sistema: string;
  origen: DataStatistics;
  destino: DataStatistics;
  muestraAleatoria: SampleRecord[];
  memoria: MemoryInfo;
  resumen: string;
}

export interface DataStatistics {
  totalRegistros: number;
  totalBloques: number;
  montoTotal: number;
  montoPromedio: number;
  montoMinimo: number;
  montoMaximo: number;
  primerRegistroId: string;
  ultimoRegistroId: string;
  fechaMinima: string;
  fechaMaxima: string;
  clientesUnicos: number;
  productosUnicos: number;
  top10PeriodosPorRegistros: PeriodInfo[];
}

export interface PeriodInfo {
  periodo: string;
  registros: number;
  montoTotal: number;
  hash: string;
}

export interface SampleRecord {
  id: string;
  fecha: string;
  cliente: string;
  producto: string;
  monto: number;
  periodo: string;
}

export interface MemoryInfo {
  memoriaUsadaMB: number;
  memoriaTotalMB: number;
  collectionsGC0: number;
  collectionsGC1: number;
  collectionsGC2: number;
}

export interface HackResponse {
  exitoso: boolean;
  mensaje: string;
  periodoAfectado: string;
  instrucciones: string;
}

export interface ResetResponse {
  exitoso: boolean;
  mensaje: string;
  accionesRealizadas: string[];
  siguientePaso: string;
}
