import { http, ApiEnvelope } from './http';

export interface SyncHeartbeat {
  State: string | null;
  LastRunAt: string | null;
  Detail: string | null;
}

export interface SyncCursor {
  TableName: string;
  GroupName: string;
  IntervalSec: number;
  Enabled: number;
  LastModified: string | null;
  LastId: string | null;
  LastSuccessAt: string | null;
  LastCount: number | null;
}

export interface SyncLog {
  TableName: string;
  RunType: string;
  RowsRead: number;
  RowsUpserted: number;
  RowsSkipped: number;
  Errors: number;
  StartedAt: string;
  Message: string | null;
}

export interface SyncRowErrors {
  Pending: number | null;
  DeadLetter: number | null;
}

export interface SyncCommand {
  Id: string;
  Command: string;
  Param: string | null;
  Status: string;
  CreatedAt: string;
  FinishedAt: string | null;
  Result: string | null;
}

export interface SyncStatus {
  heartbeat: SyncHeartbeat | null;
  lastReconcile: SyncLog | null;
  cursors: SyncCursor[];
  recentLogs: SyncLog[];
  rowErrors: SyncRowErrors | null;
  commands: SyncCommand[];
}

export interface SyncRowError {
  ID: string;
  TABLE_NAME: string;
  ROW_ID: string;
  RETRY_COUNT: number;
  NEXT_RETRY_AT: string | null;
  DEAD_LETTER: number;
  ERROR: string;
  CREATED_AT: string;
}

export async function getSyncStatus(): Promise<SyncStatus> {
  const res = await http.get<ApiEnvelope<SyncStatus>>('/sync/status');
  return res.data.data;
}

export async function getRowErrors(dead = false, limit = 50): Promise<SyncRowError[]> {
  const res = await http.get<ApiEnvelope<SyncRowError[]>>(
    `/sync/row-errors?dead=${dead}&limit=${limit}`
  );
  return res.data.data;
}

export async function triggerReconcile(): Promise<{ commandId: string; message: string }> {
  const res = await http.post<ApiEnvelope<{ commandId: string; message: string }>>(
    '/sync/reconcile', {}
  );
  return res.data.data;
}

export async function triggerRetryRowErrors(): Promise<{ commandId: string }> {
  const res = await http.post<ApiEnvelope<{ commandId: string }>>('/sync/retry-row-errors', {});
  return res.data.data;
}

export async function triggerExtractMedia(): Promise<{ commandId: string; message: string }> {
  const res = await http.post<ApiEnvelope<{ commandId: string; message: string }>>(
    '/sync/extract-media', {}
  );
  return res.data.data;
}

export async function requeueRowError(id: string): Promise<void> {
  await http.post<ApiEnvelope<unknown>>(`/sync/row-errors/${id}/requeue`, {});
}

export async function ignoreRowError(id: string): Promise<void> {
  await http.post<ApiEnvelope<unknown>>(`/sync/row-errors/${id}/ignore`, {});
}

// ---------- Phase 7+ : cấu hình bảng sync (W_SYNC_TABLE_POLICY) ----------

export interface SyncTablePolicy {
  TableName: string;
  GroupName: string;
  IntervalSec: number;
  Enabled: boolean | number;
  Priority: number;
  BatchSize: number;
}

export async function getSyncTables(): Promise<SyncTablePolicy[]> {
  const res = await http.get<ApiEnvelope<SyncTablePolicy[]>>('/sync/tables');
  return res.data.data;
}

export async function toggleSyncTable(tableName: string, enabled: boolean): Promise<void> {
  await http.post<ApiEnvelope<unknown>>(`/sync/tables/${tableName}/toggle`, enabled);
}
