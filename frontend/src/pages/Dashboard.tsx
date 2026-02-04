import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import type { SystemStatusResponse, LedgerStats } from '../types/api';

export function Dashboard() {
  const [status, setStatus] = useState<SystemStatusResponse | null>(null);
  const [ledgerStats, setLedgerStats] = useState<LedgerStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [statusData, ledgerData] = await Promise.all([
        api.getStatus(),
        api.getLedger(),
      ]);
      setStatus(statusData);
      setLedgerStats(ledgerData.stats);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar datos');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 5000);
    return () => clearInterval(interval);
  }, []);

  if (loading && !status) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-center">
          <div className="spinner mb-4 mx-auto"></div>
          <p className="text-neutral-600">Cargando datos del sistema...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="card p-8">
        <div className="text-center">
          <div className="text-5xl mb-4">⚠️</div>
          <div className="text-xl font-semibold text-neutral-900 mb-2">
            Error de Conexión
          </div>
          <div className="text-sm text-neutral-500 mb-6">{error}</div>
          <button onClick={fetchData} className="btn-primary">
            Reintentar
          </button>
        </div>
      </div>
    );
  }

  if (!status || !ledgerStats) return null;

  return (
    <div className="space-y-6 fade-in">
      {/* Page Header */}
      <div className="section-header">
        <div>
          <h1 className="section-title">Dashboard</h1>
          <p className="section-subtitle">
            Monitoreo en tiempo real del sistema de sincronización
          </p>
        </div>
      </div>

      {/* Status Overview */}
      <div className="card">
        <div className="card-header">
          <div className="flex items-center justify-between w-full">
            <h2 className="card-title">Estado del Sistema</h2>
            <div className="flex items-center gap-2 text-sm">
              <span className="status-dot-success"></span>
              <span className="text-neutral-600">{status.estadoSincronizacion}</span>
            </div>
          </div>
        </div>
        <div className="card-content">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="metric">
              <div className="metric-label">Sistema Origen</div>
              <div className="metric-value text-primary-600">
                {status.origen.registros.toLocaleString()}
              </div>
              <div className="text-sm text-neutral-500 mt-2">
                {status.origen.bloques} bloques
              </div>
            </div>

            <div className="metric">
              <div className="metric-label">Sistema Destino</div>
              <div className="metric-value text-primary-600">
                {status.destino.registros.toLocaleString()}
              </div>
              <div className="text-sm text-neutral-500 mt-2">
                {status.destino.bloques} bloques
              </div>
            </div>

            <div className="metric">
              <div className="metric-label">Estado</div>
              <div className="metric-value">
                {ledgerStats.porcentajeSincronizado.toFixed(1)}%
              </div>
              <div className="text-sm text-neutral-500 mt-2">Sincronizado</div>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div className="stat-card">
          <div className="flex items-center justify-between mb-4">
            <div className="text-3xl">📦</div>
            <div className="badge-primary">Total</div>
          </div>
          <div className="metric-value">{ledgerStats.totalBloques}</div>
          <div className="metric-label mt-2">Total Bloques</div>
        </div>

        <div className="stat-card">
          <div className="flex items-center justify-between mb-4">
            <div className="text-3xl">✅</div>
            <div className="badge-success">Activo</div>
          </div>
          <div className="metric-value text-success-600">
            {ledgerStats.sincronizados}
          </div>
          <div className="metric-label mt-2">Sincronizados</div>
        </div>

        <div className="stat-card">
          <div className="flex items-center justify-between mb-4">
            <div className="text-3xl">⚠️</div>
            <div className="badge-danger">Alerta</div>
          </div>
          <div className="metric-value text-danger-600">
            {ledgerStats.corruptos}
          </div>
          <div className="metric-label mt-2">Corruptos</div>
        </div>

        <div className="stat-card">
          <div className="flex items-center justify-between mb-4">
            <div className="text-3xl">⏳</div>
            <div className="badge-warning">Pendiente</div>
          </div>
          <div className="metric-value text-warning-600">
            {ledgerStats.pendientes}
          </div>
          <div className="metric-label mt-2">Pendientes</div>
        </div>
      </div>

      {/* Sync Progress */}
      <div className="card">
        <div className="card-header">
          <h2 className="card-title">Progreso de Sincronización</h2>
        </div>
        <div className="card-content">
          <div className="flex items-center gap-6">
            <div className="flex-1">
              <div className="progress-bar">
                <div
                  className="progress-fill"
                  style={{ width: `${ledgerStats.porcentajeSincronizado}%` }}
                ></div>
              </div>
              <div className="grid grid-cols-3 gap-4 mt-6 text-center text-sm">
                <div>
                  <div className="text-neutral-500 mb-1">SKIP</div>
                  <div className="font-semibold text-success-600">
                    {ledgerStats.sincronizados}
                  </div>
                </div>
                <div>
                  <div className="text-neutral-500 mb-1">REPAIR</div>
                  <div className="font-semibold text-danger-600">
                    {ledgerStats.corruptos}
                  </div>
                </div>
                <div>
                  <div className="text-neutral-500 mb-1">INSERT</div>
                  <div className="font-semibold text-warning-600">
                    {ledgerStats.pendientes}
                  </div>
                </div>
              </div>
            </div>
            <div className="text-center min-w-[120px]">
              <div className="text-5xl font-bold text-primary-600">
                {ledgerStats.porcentajeSincronizado.toFixed(0)}%
              </div>
              <div className="text-sm text-neutral-500 mt-2">Completado</div>
            </div>
          </div>
        </div>
      </div>

      {/* System Info */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="card">
          <div className="card-header">
            <h2 className="card-title">Sistema Origen</h2>
          </div>
          <div className="card-content space-y-3">
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Registros</span>
              <span className="font-semibold">
                {status.origen.registros.toLocaleString()}
              </span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Bloques</span>
              <span className="font-semibold">{status.origen.bloques}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Periodos</span>
              <span className="font-semibold">
                {status.origen.periodos.length}
              </span>
            </div>
          </div>
        </div>

        <div className="card">
          <div className="card-header">
            <h2 className="card-title">Sistema Destino</h2>
          </div>
          <div className="card-content space-y-3">
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Registros</span>
              <span className="font-semibold">
                {status.destino.registros.toLocaleString()}
              </span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Bloques</span>
              <span className="font-semibold">{status.destino.bloques}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Periodos</span>
              <span className="font-semibold">
                {status.destino.periodos.length}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Last Update */}
      <div className="text-center text-sm text-neutral-400">
        Última actualización: {new Date(status.timestamp).toLocaleString('es-CL')}
      </div>
    </div>
  );
}
