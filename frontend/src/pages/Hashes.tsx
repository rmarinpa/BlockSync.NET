import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import type { HashComparisonResponse, BlockComparison } from '../types/api';

export function Hashes() {
  const [data, setData] = useState<HashComparisonResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchData = async () => {
    try {
      setLoading(true);
      const hashes = await api.getHashes();
      setData(hashes);
    } catch (err) {
      console.error('Error loading hashes:', err);
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

  const getEstadoStyles = (estado: string) => {
    switch (estado) {
      case 'SINCRONIZADO':
        return {
          border: 'border-success-200',
          bg: 'bg-success-50',
          text: 'text-success-600',
          icon: '✓',
        };
      case 'CORRUPTO':
        return {
          border: 'border-danger-200',
          bg: 'bg-danger-50',
          text: 'text-danger-600',
          icon: '⚠',
        };
      case 'FALTA_EN_DESTINO':
        return {
          border: 'border-warning-200',
          bg: 'bg-warning-50',
          text: 'text-warning-600',
          icon: '○',
        };
      default:
        return {
          border: 'border-neutral-200',
          bg: 'bg-neutral-50',
          text: 'text-neutral-600',
          icon: '?',
        };
    }
  };

  const formatHash = (hash: string | null | undefined) => {
    if (!hash) return '─'.repeat(32);
    return hash;
  };

  const BlockCard = ({ block }: { block: BlockComparison }) => {
    const styles = getEstadoStyles(block.estado);
    const hashesMatch = block.origen?.hash === block.destino?.hash;

    return (
      <div className={`card border-2 ${styles.border} ${styles.bg}`}>
        <div className="card-header">
          <div className="flex items-center justify-between w-full">
            <div className="flex items-center gap-2">
              <span className={`text-xl ${styles.text}`}>{styles.icon}</span>
              <span className="font-semibold">{block.periodo}</span>
            </div>
            <span className={`badge ${styles.text} bg-white border border-current text-xs`}>
              {block.estado.replace('_', ' ')}
            </span>
          </div>
        </div>
        <div className="card-content space-y-4">
          {/* Origin */}
          <div>
            <div className="text-xs font-semibold text-neutral-500 uppercase mb-2">
              ORIGEN (Source)
            </div>
            <div className="comparison-section">
              <div className="hash-full text-success-600 mb-2">
                {formatHash(block.origen?.hash)}
              </div>
              <div className="flex justify-between text-xs">
                <span className="text-neutral-600">Registros:</span>
                <span className="font-semibold">
                  {block.origen?.registros?.toLocaleString() || '0'}
                </span>
              </div>
              <div className="flex justify-between text-xs">
                <span className="text-neutral-600">Monto:</span>
                <span className="font-semibold text-primary-600">
                  ${block.origen?.monto?.toLocaleString('es-CL', {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2,
                  }) || '0.00'}
                </span>
              </div>
            </div>
          </div>

          {/* Comparison Arrow */}
          <div className="text-center">
            {hashesMatch ? (
              <div className="text-success-600 text-2xl">⇅</div>
            ) : (
              <div className="text-danger-600 text-2xl animate-pulse-subtle">⇅</div>
            )}
          </div>

          {/* Destination */}
          <div>
            <div className="text-xs font-semibold text-neutral-500 uppercase mb-2">
              DESTINO (Destination)
            </div>
            <div className="comparison-section">
              <div
                className={`hash-full mb-2 ${
                  hashesMatch ? 'text-success-600' : 'text-danger-600'
                }`}
              >
                {formatHash(block.destino?.hash)}
              </div>
              <div className="flex justify-between text-xs">
                <span className="text-neutral-600">Registros:</span>
                <span className="font-semibold">
                  {block.destino?.registros?.toLocaleString() || '0'}
                </span>
              </div>
              <div className="flex justify-between text-xs">
                <span className="text-neutral-600">Monto:</span>
                <span className="font-semibold text-primary-600">
                  ${block.destino?.monto?.toLocaleString('es-CL', {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2,
                  }) || '0.00'}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  };

  return (
    <div className="space-y-6 fade-in">
      {/* Page Header */}
      <div className="section-header">
        <div>
          <h1 className="section-title">Hash Comparison</h1>
          <p className="section-subtitle">
            Comparación de integridad origen vs destino
          </p>
        </div>
      </div>

      {/* Stats Header */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="card p-4 text-center">
          <div className="text-2xl font-bold text-neutral-900">
            {data.totalPeriodos}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Total Periodos</div>
        </div>
        <div className="card p-4 text-center border-2 border-success-200">
          <div className="text-2xl font-bold text-success-600">
            {data.sincronizados}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Sincronizados</div>
        </div>
        <div className="card p-4 text-center border-2 border-danger-200">
          <div className="text-2xl font-bold text-danger-600">
            {data.corruptos}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Corruptos</div>
        </div>
        <div className="card p-4 text-center border-2 border-warning-200">
          <div className="text-2xl font-bold text-warning-600">
            {data.faltanEnDestino}
          </div>
          <div className="text-xs text-neutral-500 mt-1">Faltan en Destino</div>
        </div>
      </div>

      {/* Hash Blocks Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {data.bloques.map((block) => (
          <BlockCard key={block.periodo} block={block} />
        ))}
      </div>

      {/* Legend */}
      <div className="card">
        <div className="card-content">
          <div className="flex flex-wrap items-center gap-6 text-sm">
            <div className="flex items-center gap-2">
              <span className="text-success-600 text-xl">✓</span>
              <span>SINCRONIZADO - Hashes coinciden</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-danger-600 text-xl">⚠</span>
              <span>CORRUPTO - Hashes diferentes</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-warning-600 text-xl">○</span>
              <span>FALTA EN DESTINO - Bloque no existe</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
