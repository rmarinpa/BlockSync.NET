import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import type { DiagnosticsResponse } from '../types/api';

export function Diagnostics() {
  const [data, setData] = useState<DiagnosticsResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchData = async () => {
    try {
      setLoading(true);
      const diagnostics = await api.getDiagnostics();
      setData(diagnostics);
    } catch (err) {
      console.error('Error loading diagnostics:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="spinner"></div>
      </div>
    );
  }

  if (!data) return null;

  const formatMoney = (amount: number) => {
    return amount.toLocaleString('es-CL', {
      style: 'currency',
      currency: 'CLP',
    });
  };

  return (
    <div className="space-y-6 fade-in">
      {/* Page Header */}
      <div className="section-header">
        <div>
          <h1 className="section-title">System Diagnostics</h1>
          <p className="section-subtitle">{data.resumen}</p>
        </div>
        <button
          onClick={fetchData}
          className="btn-secondary"
        >
          🔄 Actualizar
        </button>
      </div>

      {/* Memory Stats */}
      <div className="card">
        <div className="card-header bg-primary-50">
          <h2 className="card-title text-primary-700">💾 Memoria del Sistema</h2>
        </div>
        <div className="card-content">
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4 text-center">
            <div>
              <div className="text-xs text-neutral-500 mb-1">Memoria Usada</div>
              <div className="text-2xl font-bold text-danger-600">
                {data.memoria.memoriaUsadaMB} MB
              </div>
            </div>
            <div>
              <div className="text-xs text-neutral-500 mb-1">Memoria Total</div>
              <div className="text-2xl font-bold text-primary-600">
                {data.memoria.memoriaTotalMB} MB
              </div>
            </div>
            <div>
              <div className="text-xs text-neutral-500 mb-1">GC Gen 0</div>
              <div className="text-2xl font-bold">{data.memoria.collectionsGC0}</div>
            </div>
            <div>
              <div className="text-xs text-neutral-500 mb-1">GC Gen 1</div>
              <div className="text-2xl font-bold">{data.memoria.collectionsGC1}</div>
            </div>
            <div>
              <div className="text-xs text-neutral-500 mb-1">GC Gen 2</div>
              <div className="text-2xl font-bold">{data.memoria.collectionsGC2}</div>
            </div>
          </div>
        </div>
      </div>

      {/* Data Statistics - Origin vs Destination */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Origin */}
        <div className="card">
          <div className="card-header bg-success-50">
            <h2 className="card-title text-success-700">📊 Sistema Origen</h2>
          </div>
          <div className="card-content space-y-3">
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Registros</span>
              <span className="font-semibold text-primary-600">
                {data.origen.totalRegistros.toLocaleString()}
              </span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Bloques</span>
              <span className="font-semibold">{data.origen.totalBloques}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Monto Total</span>
              <span className="font-semibold text-success-600">
                {formatMoney(data.origen.montoTotal)}
              </span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Monto Promedio</span>
              <span className="font-semibold">{formatMoney(data.origen.montoPromedio)}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Clientes Únicos</span>
              <span className="font-semibold">{data.origen.clientesUnicos.toLocaleString()}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Productos Únicos</span>
              <span className="font-semibold">{data.origen.productosUnicos.toLocaleString()}</span>
            </div>
          </div>
        </div>

        {/* Destination */}
        <div className="card">
          <div className="card-header bg-primary-50">
            <h2 className="card-title text-primary-700">📊 Sistema Destino</h2>
          </div>
          <div className="card-content space-y-3">
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Registros</span>
              <span className="font-semibold text-primary-600">
                {data.destino.totalRegistros.toLocaleString()}
              </span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Bloques</span>
              <span className="font-semibold">{data.destino.totalBloques}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Monto Total</span>
              <span className="font-semibold text-success-600">
                {formatMoney(data.destino.montoTotal)}
              </span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Monto Promedio</span>
              <span className="font-semibold">{formatMoney(data.destino.montoPromedio)}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Clientes Únicos</span>
              <span className="font-semibold">{data.destino.clientesUnicos.toLocaleString()}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-neutral-600">Productos Únicos</span>
              <span className="font-semibold">{data.destino.productosUnicos.toLocaleString()}</span>
            </div>
          </div>
        </div>
      </div>

      {/* Random Sample */}
      {data.muestraAleatoria && data.muestraAleatoria.length > 0 && (
        <div className="card">
          <div className="card-header">
            <div className="flex items-center justify-between w-full">
              <h2 className="card-title">📝 Muestra Aleatoria de Datos</h2>
              <button
                onClick={fetchData}
                className="text-xs bg-primary-500 text-white px-3 py-1 rounded-lg hover:bg-primary-600 transition-colors"
              >
                🔄 Nueva Muestra
              </button>
            </div>
          </div>
          <div className="overflow-x-auto">
            <table className="table">
              <thead>
                <tr className="table-header">
                  <th className="table-cell text-left">ID</th>
                  <th className="table-cell text-left">Fecha</th>
                  <th className="table-cell text-left">Cliente</th>
                  <th className="table-cell text-left">Producto</th>
                  <th className="table-cell text-right">Monto</th>
                  <th className="table-cell text-center">Periodo</th>
                </tr>
              </thead>
              <tbody>
                {data.muestraAleatoria.map((record) => (
                  <tr key={record.id} className="table-row">
                    <td className="table-cell">
                      <code className="hash-display">
                        {record.id.substring(0, 8)}...
                      </code>
                    </td>
                    <td className="table-cell text-sm">
                      {new Date(record.fecha).toLocaleDateString('es-CL')}
                    </td>
                    <td className="table-cell text-sm">{record.cliente}</td>
                    <td className="table-cell text-sm">{record.producto}</td>
                    <td className="table-cell text-right font-semibold text-primary-600">
                      {formatMoney(record.monto)}
                    </td>
                    <td className="table-cell text-center">
                      <span className="badge-success">{record.periodo}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Top 10 Periods */}
      {data.origen.top10PeriodosPorRegistros &&
        data.origen.top10PeriodosPorRegistros.length > 0 && (
          <div className="card">
            <div className="card-header">
              <h2 className="card-title">🏆 Top 10 Periodos por Registros</h2>
            </div>
            <div className="card-content">
              <div className="space-y-2">
                {data.origen.top10PeriodosPorRegistros.map((period, index) => (
                  <div
                    key={period.periodo}
                    className="card p-4 flex items-center gap-4 hover:shadow-md transition-shadow"
                  >
                    <div className="text-2xl font-bold text-primary-600 min-w-[40px]">
                      #{index + 1}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-4 mb-1">
                        <span className="font-semibold text-primary-600">
                          {period.periodo}
                        </span>
                        <span className="text-neutral-600 text-sm">
                          {period.registros.toLocaleString()} registros
                        </span>
                        <span className="text-success-600 text-sm font-semibold">
                          {formatMoney(period.montoTotal)}
                        </span>
                      </div>
                      <code className="hash-display text-xs">
                        Hash: {period.hash.substring(0, 32)}...
                      </code>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}
    </div>
  );
}
