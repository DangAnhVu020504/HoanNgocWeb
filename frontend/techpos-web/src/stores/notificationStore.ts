import { create } from 'zustand';
import { getUnreadCount } from '../api/notification';

/**
 * Notification store (Phase 7): unread badge poll 30s
 * (SignalR push có thể thêm sau — poll là nền tảng tối thiểu).
 * Timer giữ ngoài state (module-scoped) — set() state trong start/stop gây
 * React error #185 (Maximum update depth) khi component re-render.
 */
let timer: ReturnType<typeof setInterval> | null = null;

interface NotificationState {
  unread: number;
  loadUnread: () => Promise<void>;
  startPolling: () => void;
  stopPolling: () => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
  unread: 0,
  loadUnread: async () => {
    try { set({ unread: await getUnreadCount() }); } catch { /* ignore — badge chỉ là phụ */ }
  },
  startPolling: () => {
    if (timer) return;   // đã polling — không start lại (StrictMode gọi 2 lần)
    timer = setInterval(() => { void useNotificationStore.getState().loadUnread(); }, 30_000);
    void useNotificationStore.getState().loadUnread();
  },
  stopPolling: () => {
    // Không clearInterval khi unmount (bell nằm trong layout — giữ polling toàn phiên).
    // Chỉ dùng cho logout.
  },
}));
