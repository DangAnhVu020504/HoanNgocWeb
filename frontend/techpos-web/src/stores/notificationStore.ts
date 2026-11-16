import { create } from 'zustand';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { getUnreadCount } from '../api/notification';

/**
 * Notification store (Phase 7–8): badge unread.
 * - SignalR push realtime (/hubs/notifications) + poll 30s dự phòng.
 * - Hub/timer giữ ngoài state (module-scoped) — set() state trong start/stop gây
 *   React error #185 (Maximum update depth) khi component re-render.
 */
let timer: ReturnType<typeof setInterval> | null = null;
let hub: HubConnection | null = null;

interface NotificationState {
  unread: number;
  loadUnread: () => Promise<void>;
  startPolling: () => void;
  stopPolling: () => void;
}

const startRealtime = (): void => {
  if (hub || !localStorage.getItem('techpos_token')) return;
  const connection = new HubConnectionBuilder()
    .withUrl('/hubs/notifications', {
      accessTokenFactory: () => localStorage.getItem('techpos_token') ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on('notification', () => {
    void useNotificationStore.getState().loadUnread();
  });
  connection.onreconnected(() => {
    void useNotificationStore.getState().loadUnread();
  });

  hub = connection;
  connection.start().catch(() => { hub = null; });
};

export const useNotificationStore = create<NotificationState>((set) => ({
  unread: 0,
  loadUnread: async () => {
    try { set({ unread: await getUnreadCount() }); } catch { /* ignore — badge chỉ là phụ */ }
  },
  startPolling: () => {
    startRealtime();
    if (timer) return;   // đã polling — không start lại (StrictMode gọi 2 lần)
    timer = setInterval(() => { void useNotificationStore.getState().loadUnread(); }, 30_000);
    void useNotificationStore.getState().loadUnread();
  },
  stopPolling: () => {
    // Không clearInterval khi unmount (bell nằm trong layout — giữ polling toàn phiên).
    // Chỉ dùng cho logout.
    if (timer) { clearInterval(timer); timer = null; }
    if (hub) { void hub.stop(); hub = null; }
  },
}));
