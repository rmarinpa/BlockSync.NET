import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import type { LedgerResponse, LedgerEntryDto } from '../types/api';

export function Ledger() {
  const [data, setData] = useState<LedgerResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<string>('ALL');
  const [expandedRow, setExpandedRow] = useState<string | null>(null);

  const fetchData = async () => {
    try {
      setLoading(true);
      const ledger = await api.getLedger();
      setData(ledger);
    } catch (err) {
      console.error('Error loading ledger:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchData, 10000);
    return () => clearInterval(interval);
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="spinner"></div>
      </div>
    );
  }

  if (!data) return null;

  const filteredEntradas = filter === 'ALL'
    ? data.entradas
    : data.entradas.filter((e) => e.estado === filter);

  const getBadgeClass = (estado: string) => {
    switch (estado) {
      case 'SINCRONIZADO':
        return 'badge-success';
      case 'CORRUPTO':
        return 'badge-danger';
      case 'PENDIENTE':
        return 'badge-warning';
      default:
        return 'badge-neutral';
    }
  };

  const getAccionColor = (accion: string) => {
    switch (accion) {
      case 'SKIP':
        return 'text-success-600';
      case 'INSERT':
        return 'text-warning-600';
      case 'REPAIR':
        return 'text-danger-600';
      default:
        return 'text-neutral-500';
    }
  };

  const formatHash = (hash: string) => {
    return `${hash.substring(0, 8)}...${hash.substring(hash.length - 8)}`;
  };

  const formatDate = (date: string) => {
    return new Date(date).toLocaleString('es-CL', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  return (
    <div className="space-y-6 fade-in">
      {/* Page Header */}
      <div className="section-header">
        <div>
          <h1 className="section-title">Blockchain Ledger</h1>
          <p className="section-subtitle">
            Historial completo de sincronizaciones por periodo
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
        <div className="card p-4 text-center">
          <div className="text-2xl font-bold text-neutral-900">
            {data.stats.totalBloques}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Total</div>
        </div>
        <div className="card p-4 text-center">
          <div className="text-2xl font-bold text-success-600">
            {data.stats.sincronizados}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Sincronizados</div>
        </div>
        <div className="card p-4 text-center">
          <div className="text-2xl font-bold text-danger-600">
            {data.stats.corruptos}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Corruptos</div>
        </div>
        <div className="card p-4 text-center">
          <div className="text-2xl font-bold text-warning-600">
            {data.stats.pendientes}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Pendientes</div>
        </div>
        <div className="card p-4 text-center">
          <div className="text-2xl font-bold text-danger-600">
            {data.stats.errores}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Errores</div>
        </div>
      </div>

      {/* Filters */}
      <div className="card">
        <div className="card-content">
          <div className="flex flex-wrap gap-2">
            {['ALL', 'SINCRONIZADO', 'CORRUPTO', 'PENDIENTE', 'ERROR'].map((f) => (
              <button
                key={f}
                onClick={() => setFilter(f)}
                className={`
                  px-4 py-2 rounded-lg text-sm font-medium transition-all
                  ${
                    filter === f
                      ? 'bg-primary-500 text-white shadow-sm'
                      : 'bg-neutral-100 text-neutral-600 hover:bg-neutral-200'
                  }
                `}
              >
                {f}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Ledger Table */}
      <div className="table-container">
        <div className="overflow-x-auto">
          <table className="table">
            <thead>
              <tr className="table-header">
                <th className="table-cell text-left">Periodo</th>
                <th className="table-cell text-left">Hash</th>
                <th className="table-cell text-center">Estado</th>
                <th className="table-cell text-right">Registros</th>
                <th className="table-cell text-right">Monto Total</th>
                <th className="table-cell text-center">Última Acción</th>
                <th className="table-cell text-left">Última Sync</th>
              </tr>
            </thead>
            <tbody>
              {filteredEntradas.map((entrada) => (
                <>
                  <tr
                    key={entrada.periodo}
                    className="table-row"
                    onClick={() =>
                      setExpandedRow(
                        expandedRow === entrada.periodo ? null : entrada.periodo
                      )
                    }
                  >
                    <td className="table-cell font-semibold text-primary-600">
                      {entrada.periodo}
                    </td>
                    <td className="table-cell">
                      <code className="hash-display">
                        {formatHash(entrada.hash)}
                      </code>
                    </td>
                    <td className="table-cell text-center">
                      <span className={getBadgeClass(entrada.estado)}>
                        {entrada.estado}
                      </span>
                    </td>
                    <td className="table-cell text-right">
                      {entrada.totalRegistros.toLocaleString()}
                    </td>
                    <td className="table-cell text-right font-medium text-primary-600">
                      ${entrada.sumaMonto.toLocaleString('es-CL', {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2,
                      })}
                    </td>
                    <td className="table-cell text-center">
                      <span className={`font-semibold ${getAccionColor(entrada.ultimaAccion)}`}>
                        {entrada.ultimaAccion}
                      </span>
                    </td>
                    <td className="table-cell text-neutral-500">
                      {formatDate(entrada.ultimaSync)}
                    </td>
                  </tr>
                  {expandedRow === entrada.periodo && (
                    <tr className="bg-neutral-50">
                      <td colSpan={7} className="p-6">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                          <div className="space-y-4">
                            <div>
                              <div className="text-xs font-semibold text-neutral-500 uppercase mb-2">
                                Hash Completo
                              </div>
                              <div className="hash-full">
                                {entrada.hash}
                              </div>
                            </div>
                            <div>
                              <div className="text-xs font-semibold text-neutral-500 uppercase mb-2">
                                Timestamps
                              </div>
                              <div className="space-y-2 text-sm">
                                <div className="flex justify-between">
                                  <span className="text-neutral-600">Creado:</span>
                                  <span className="font-mono">{formatDate(entrada.createdAt)}</span>
                                </div>
                                <div className="flex justify-between">
                                  <span className="text-neutral-600">Actualizado:</span>
                                  <span className="font-mono">{formatDate(entrada.updatedAt)}</span>
                                </div>
                              </div>
                            </div>
                          </div>
                          <div className="card">
                            <div className="card-header">
                              <div className="text-sm font-semibold">Metadatos del Bloque</div>
                            </div>
                            <div className="card-content space-y-3">
                              <div className="flex justify-between text-sm">
                                <span className="text-neutral-600">Registros:</span>
                                <span className="font-semibold">{entrada.totalRegistros.toLocaleString()}</span>
                              </div>
                              <div className="flex justify-between text-sm">
                                <span className="text-neutral-600">Monto Total:</span>
                                <span className="font-semibold text-primary-600">
                                  ${entrada.sumaMonto.toLocaleString('es-CL')}
                                </span>
                              </div>
                              <div className="flex justify-between text-sm">
                                <span className="text-neutral-600">Estado:</span>
                                <span className={getBadgeClass(entrada.estado)}>
                                  {entrada.estado}
                                </span>
                              </div>
                              <div className="flex justify-between text-sm">
                                <span className="text-neutral-600">Última Acción:</span>
                                <span className={`font-semibold ${getAccionColor(entrada.ultimaAccion)}`}>
                                  {entrada.ultimaAccion}
                                </span>
                              </div>
                            </div>
                          </div>
                        </div>
                      </td>
                    </tr>
                  )}
                </>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {filteredEntradas.length === 0 && (
        <div className="card p-12 text-center">
          <div className="text-5xl mb-4">📭</div>
          <div className="text-lg font-semibold text-neutral-900 mb-2">
            No hay bloques
          </div>
          <div className="text-sm text-neutral-500">
            No se encontraron bloques con el filtro seleccionado
          </div>
        </div>
      )}
    </div>
  );
}
