import { useState } from 'react';
import { api } from '../lib/api';
import type { SyncResponse, HackResponse, ResetResponse } from '../types/api';

export function Actions() {
  const [syncResult, setSyncResult] = useState<SyncResponse | null>(null);
  const [hackResult, setHackResult] = useState<HackResponse | null>(null);
  const [resetResult, setResetResult] = useState<ResetResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [hackYear, setHackYear] = useState('2023');
  const [hackMonth, setHackMonth] = useState('6');

  const handleSync = async () => {
    try {
      setLoading(true);
      setSyncResult(null);
      const result = await api.sync();
      setSyncResult(result);
    } catch (err) {
      console.error('Error during sync:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleHack = async () => {
    try {
      setLoading(true);
      setHackResult(null);
      const result = await api.hack(parseInt(hackYear), parseInt(hackMonth));
      setHackResult(result);
    } catch (err) {
      console.error('Error during hack:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleReset = async () => {
    if (!confirm('¿Estás seguro? Esto eliminará todos los datos del destino.')) {
      return;
    }
    try {
      setLoading(true);
      setResetResult(null);
      const result = await api.reset();
      setResetResult(result);
    } catch (err) {
      console.error('Error during reset:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6 fade-in">
      {/* Page Header */}
      <div className="section-header">
        <div>
          <h1 className="section-title">System Actions</h1>
          <p className="section-subtitle">
            Ejecutar operaciones de sincronización y mantenimiento
          </p>
        </div>
      </div>

      {/* Action Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Sync */}
        <div className="card">
          <div className="card-header bg-primary-50">
            <h2 className="card-title text-primary-700">⚡ Sincronizar</h2>
          </div>
          <div className="card-content space-y-4">
            <p className="text-sm text-neutral-600">
              Ejecuta el motor de sincronización. Compara hashes y aplica acciones SKIP, INSERT o REPAIR según corresponda.
            </p>
            <button
              onClick={handleSync}
              disabled={loading}
              className="btn-primary w-full"
            >
              {loading ? (
                <span className="flex items-center justify-center gap-2">
                  <div className="spinner"></div>
                  Sincronizando...
                </span>
              ) : (
                'Ejecutar Sync'
              )}
            </button>
          </div>
        </div>

        {/* Hack */}
        <div className="card">
          <div className="card-header bg-warning-50">
            <h2 className="card-title text-warning-700">🔧 Simular Hack</h2>
          </div>
          <div className="card-content space-y-4">
            <p className="text-sm text-neutral-600">
              Corrompe un bloque específico para probar la detección y reparación automática.
            </p>
            <div className="flex gap-2">
              <input
                type="number"
                value={hackYear}
                onChange={(e) => setHackYear(e.target.value)}
                placeholder="Año"
                className="input flex-1"
              />
              <input
                type="number"
                value={hackMonth}
                onChange={(e) => setHackMonth(e.target.value)}
                placeholder="Mes"
                min="1"
                max="12"
                className="input flex-1"
              />
            </div>
            <button
              onClick={handleHack}
              disabled={loading}
              className="btn-secondary w-full"
            >
              {loading ? 'Procesando...' : 'Corromper Bloque'}
            </button>
          </div>
        </div>

        {/* Reset */}
        <div className="card">
          <div className="card-header bg-danger-50">
            <h2 className="card-title text-danger-700">⟲ Reset</h2>
          </div>
          <div className="card-content space-y-4">
            <p className="text-sm text-neutral-600">
              Elimina todos los datos del destino y reinicia el sistema al estado inicial.
            </p>
            <button
              onClick={handleReset}
              disabled={loading}
              className="btn-danger w-full"
            >
              {loading ? 'Reseteando...' : 'Reset Sistema'}
            </button>
          </div>
        </div>
      </div>

      {/* Sync Result */}
      {syncResult && (
        <div className="card slide-down">
          <div className="card-header">
            <div className="flex items-center justify-between w-full">
              <h2 className="card-title">
                {syncResult.exitoso ? '✓' : '✗'} Resultado de Sincronización
              </h2>
              <span className="text-sm text-neutral-500">
                {syncResult.resumen.duracionMs}ms
              </span>
            </div>
          </div>
          <div className="card-content space-y-6">
            {/* Summary Stats */}
            <div className="grid grid-cols-3 md:grid-cols-7 gap-4 text-center">
              <div>
                <div className="text-xs text-neutral-500 mb-1">Total</div>
                <div className="text-xl font-bold">{syncResult.resumen.totalBloques}</div>
              </div>
              <div>
                <div className="text-xs text-neutral-500 mb-1">SKIP</div>
                <div className="text-xl font-bold text-success-600">
                  {syncResult.resumen.bloquesOmitidos}
                </div>
              </div>
              <div>
                <div className="text-xs text-neutral-500 mb-1">INSERT</div>
                <div className="text-xl font-bold text-warning-600">
                  {syncResult.resumen.bloquesInsertados}
                </div>
              </div>
              <div>
                <div className="text-xs text-neutral-500 mb-1">REPAIR</div>
                <div className="text-xl font-bold text-danger-600">
                  {syncResult.resumen.bloquesReparados}
                </div>
              </div>
              <div>
                <div className="text-xs text-neutral-500 mb-1">Procesados</div>
                <div className="text-xl font-bold">
                  {syncResult.resumen.totalRegistrosProcesados.toLocaleString()}
                </div>
              </div>
              <div>
                <div className="text-xs text-neutral-500 mb-1">Insertados</div>
                <div className="text-xl font-bold">
                  {syncResult.resumen.totalRegistrosInsertados.toLocaleString()}
                </div>
              </div>
              <div>
                <div className="text-xs text-neutral-500 mb-1">Duración</div>
                <div className="text-xl font-bold text-primary-600">
                  {syncResult.resumen.duracionMs}ms
                </div>
              </div>
            </div>

            {/* Block Details */}
            {syncResult.reporte.detallesBloques.length > 0 && (
              <div>
                <div className="text-sm font-semibold mb-3">
                  Detalles por Bloque:
                </div>
                <div className="space-y-2 max-h-96 overflow-y-auto">
                  {syncResult.reporte.detallesBloques.map((block) => {
                    const accionColors = {
                      0: 'text-success-600', // SKIP
                      1: 'text-warning-600', // INSERT
                      2: 'text-danger-600', // REPAIR
                    };
                    const accionNames = {
                      0: 'SKIP',
                      1: 'INSERT',
                      2: 'REPAIR',
                    };
                    return (
                      <div
                        key={block.periodo}
                        className="card p-4 flex items-center justify-between"
                      >
                        <div className="flex items-center gap-4">
                          <span className="font-semibold text-primary-600">
                            {block.periodo}
                          </span>
                          <span className={`font-bold ${accionColors[block.accion]}`}>
                            {accionNames[block.accion]}
                          </span>
                          <span className="text-neutral-600 text-sm">{block.mensaje}</span>
                        </div>
                        <div className="flex items-center gap-4 text-xs text-neutral-500">
                          {block.registrosInsertados > 0 && (
                            <span className="text-primary-600">
                              +{block.registrosInsertados.toLocaleString()} registros
                            </span>
                          )}
                          <span>{block.duracionMs}ms</span>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Hack Result */}
      {hackResult && (
        <div className="card slide-down">
          <div className="card-header bg-warning-50">
            <h2 className="card-title text-warning-700">
              {hackResult.exitoso ? '⚠' : '✗'} Resultado de Hack
            </h2>
          </div>
          <div className="card-content space-y-4">
            <div className="alert-warning">
              <div className="font-semibold mb-2">
                {hackResult.mensaje}
              </div>
              <div className="text-sm">
                Periodo afectado: <span className="font-semibold">
                  {hackResult.periodoAfectado}
                </span>
              </div>
            </div>
            {hackResult.instrucciones && (
              <div className="code-block">
                <pre className="text-sm whitespace-pre-wrap">
                  {hackResult.instrucciones}
                </pre>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Reset Result */}
      {resetResult && (
        <div className="card slide-down">
          <div className="card-header bg-danger-50">
            <h2 className="card-title text-danger-700">
              {resetResult.exitoso ? '✓' : '✗'} Resultado de Reset
            </h2>
          </div>
          <div className="card-content space-y-4">
            <div className="alert-danger">
              <div className="font-semibold">
                {resetResult.mensaje}
              </div>
            </div>
            {resetResult.accionesRealizadas && resetResult.accionesRealizadas.length > 0 && (
              <div>
                <div className="text-sm font-semibold mb-2">Acciones realizadas:</div>
                <ul className="space-y-1">
                  {resetResult.accionesRealizadas.map((accion, i) => (
                    <li key={i} className="text-sm flex items-center gap-2">
                      <span className="text-success-600">•</span>
                      <span>{accion}</span>
                    </li>
                  ))}
                </ul>
              </div>
            )}
            {resetResult.siguientePaso && (
              <div className="alert-info">
                {resetResult.siguientePaso}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
