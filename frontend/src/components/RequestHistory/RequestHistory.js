import React, { useState, useEffect } from 'react';
import { requestHistoryApi } from '../../api/api';
import './RequestHistory.css';

const EVENT_LABELS = {
  created:        { label: 'Esitatud',      cls: 'rh-created'  },
  status_changed: { label: 'Staatus',        cls: 'rh-status'   },
  edited:         { label: 'Muudetud',       cls: 'rh-edited'   },
  comment_added:  { label: 'Kommentaar',     cls: 'rh-comment'  },
};

function relTime(iso) {
  const diff = Math.floor((Date.now() - new Date(iso)) / 1000);
  if (diff < 60)   return 'just nüüd';
  if (diff < 3600) return `${Math.floor(diff / 60)} min tagasi`;
  if (diff < 86400) return `${Math.floor(diff / 3600)} t tagasi`;
  return new Date(iso).toLocaleDateString('et-EE', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

export default function RequestHistory({ requestId }) {
  const [items, setItems]     = useState([]);
  const [loading, setLoading] = useState(true);
  const [open, setOpen]       = useState(false);

  useEffect(() => {
    if (!open || !requestId) return;
    setLoading(true);
    requestHistoryApi.get(requestId)
      .then(r => setItems(r.data || []))
      .catch(() => setItems([]))
      .finally(() => setLoading(false));
  }, [open, requestId]);

  return (
    <div className="rh-root">
      <button className="rh-toggle" onClick={() => setOpen(o => !o)}>
        <span className="rh-toggle-icon">{open ? '▾' : '▸'}</span>
        Muudatuste ajalugu
        {items.length > 0 && !loading && <span className="rh-count">{items.length}</span>}
      </button>

      {open && (
        <div className="rh-panel">
          {loading ? (
            <div className="rh-empty">Laadimine...</div>
          ) : items.length === 0 ? (
            <div className="rh-empty">Muudatusi pole.</div>
          ) : (
            <ol className="rh-list">
              {items.map(item => {
                const meta = EVENT_LABELS[item.eventType] ?? { label: item.eventType, cls: 'rh-other' };
                return (
                  <li key={item.id} className="rh-item">
                    <div className={`rh-dot ${meta.cls}`} />
                    <div className="rh-body">
                      <div className="rh-row">
                        <span className={`rh-badge ${meta.cls}`}>{meta.label}</span>
                        {item.actorName && (
                          <span className="rh-actor">
                            {item.actorName}
                            {item.actorIsAdmin && <span className="rh-admin-tag">admin</span>}
                          </span>
                        )}
                        <span className="rh-time">{relTime(item.createdAt)}</span>
                      </div>
                      {item.description && <div className="rh-desc">{item.description}</div>}
                      {(item.oldValue || item.newValue) && (
                        <div className="rh-change">
                          {item.oldValue && <span className="rh-old">{item.oldValue}</span>}
                          {item.oldValue && item.newValue && <span className="rh-arrow">→</span>}
                          {item.newValue && <span className="rh-new">{item.newValue}</span>}
                        </div>
                      )}
                    </div>
                  </li>
                );
              })}
            </ol>
          )}
        </div>
      )}
    </div>
  );
}
