import type {
  SystemStatusResponse,
  SyncResponse,
  LedgerResponse,
  HashComparisonResponse,
  DiagnosticsResponse,
  HackResponse,
  ResetResponse,
} from '../types/api';

const API_BASE = 'http://localhost:5000/api';

class ApiClient {
  private async fetch<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${API_BASE}${endpoint}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    });

    if (!response.ok) {
      throw new Error(`API Error: ${response.statusText}`);
    }

    return response.json();
  }

  async getStatus(): Promise<SystemStatusResponse> {
    return this.fetch<SystemStatusResponse>('/Sync/status');
  }

  async sync(): Promise<SyncResponse> {
    return this.fetch<SyncResponse>('/Sync', { method: 'POST' });
  }

  async getLedger(): Promise<LedgerResponse> {
    return this.fetch<LedgerResponse>('/Sync/ledger');
  }

  async getHashes(): Promise<HashComparisonResponse> {
    return this.fetch<HashComparisonResponse>('/Sync/hashes');
  }

  async getDiagnostics(): Promise<DiagnosticsResponse> {
    return this.fetch<DiagnosticsResponse>('/Sync/diagnostics');
  }

  async hack(year: number, month: number): Promise<HackResponse> {
    return this.fetch<HackResponse>(`/Sync/hack/${year}/${month}`, {
      method: 'POST',
    });
  }

  async reset(): Promise<ResetResponse> {
    return this.fetch<ResetResponse>('/Sync/reset', { method: 'POST' });
  }
}

export const api = new ApiClient();
