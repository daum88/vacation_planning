import React, { useState, useEffect, useRef, useCallback } from 'react';
import { notificationsApi } from '../../api/api';
import { session } from '../../utils/sessionUtils';
import './NotificationBell.css';

const POLL_INTERVAL = 30_000; // 30s

const TYPE_ICON = {
  approved: '✓',
  rejected: '✗',
  comment:  '✉',
  pending:  '⏳',
};

const TYPE_CLASS = {
  approved: 'nb-item-approved',
  rejected: 'nb-item-rejected',
  comment:  'nb-item-comment',
  pending:  'nb-item-pending',
};

export default function NotificationBell() {
  const [items, setItems]         = useState([]);
  const [unread, setUnread]       = useState(0);
  const [open, setOpen]           = useState(false);
  const [loading, setLoading]     = useState(false);
  const panelRef                  = useRef(null);
  const lastSeenRef               = useRef(session.getNotifLastSeen());

  const fetchNotifications = useCallback(async (showLoader = false) => {
    if (showLoader) setLoading(true);
    try {
      const res = await notificationsApi.getMy(lastSeenRef.current || undefined);
      const data = res.data;
      setItems(data.items || []);
      setUnread(data.unreadCount || 0);
    } catch {
      // fail silently — bell shouldn't break the app
    } finally {
      if (showLoader) setLoading(false);
    }
  }, []);

  // Initial load + polling
  useEffect(() => {
    fetchNotifications(true);
    const id = setInterval(() => fetchNotifications(false), POLL_INTERVAL);
    return () => clearInterval(id);
  }, [fetchNotifications]);

  // Close on outside click
  useEffect(() => {
    if (!open) return;
    const handler = (e) => {
      if (panelRef.current && !panelRef.current.contains(e.target)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  const handleOpen = () => {
    setOpen(o => !o);
    if (!open && unread > 0) {
      // Mark all as seen
      const now = new Date().toISOString();
      session.setNotifLastSeen(now);
      lastSeenRef.current = now;
      setUnread(0);
    }
  };

  return (
    <div className="nb-wrapper" ref={panelRef}>
      <button
        className={`nb-bell ${unread > 0 ? 'nb-bell-active' : ''}`}
        onClick={handleOpen}
        title="Teavitused"
        aria-label={`Teavitused${unread > 0 ? ` (${unread} uut)` : ''}`}
      >
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
          <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
        </svg>
        {unread > 0 && (
          <span className="nb-badge">{unread > 9 ? '9+' : unread}</span>
        )}
      </button>

      {open && (
        <div className="nb-panel">
          <div className="nb-panel-head">
            <span className="nb-panel-title">Teavitused</span>
            <button className="nb-panel-close" onClick={() => setOpen(false)}>×</button>
          </div>

          {loading ? (
            <div className="nb-empty">Laadimine...</div>
          ) : items.length === 0 ? (
            <div className="nb-empty">Uusi teavitusi pole.</div>
          ) : (
            <ul className="nb-list">
              {items.map(item => (
                <li key={`${item.type}-${item.id}`} className={`nb-item ${TYPE_CLASS[item.type] || ''}`}>
                  <span className="nb-item-icon" aria-hidden="true">
                    {TYPE_ICON[item.type] || '·'}
                  </span>
                  <div className="nb-item-body">
                    <span className="nb-item-msg">{item.message}</span>
                    <span className="nb-item-time">{fmtRelative(item.createdAt)}</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}

function fmtRelative(iso) {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1)   return 'just nüüd';
  if (mins < 60)  return `${mins} min tagasi`;
  const hrs = Math.floor(mins / 60);
  if (hrs  < 24)  return `${hrs} t tagasi`;
  return new Date(iso).toLocaleDateString('et-EE', { day: '2-digit', month: 'short' });
}
