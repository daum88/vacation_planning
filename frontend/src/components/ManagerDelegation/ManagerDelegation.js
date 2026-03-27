import React, { useState, useEffect } from 'react';
import { delegationsApi, usersApi } from '../../api/api';
import { useToast } from '../Toast/Toast';
import './ManagerDelegation.css';

function fmtDate(iso) {
  return new Date(iso).toLocaleDateString('et-EE', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

export default function ManagerDelegation() {
  const [delegations, setDelegations] = useState([]);
  const [users, setUsers]             = useState([]);
  const [form, setForm]               = useState(null);
  const [loading, setLoading]         = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(null);
  const toast = useToast();

  const today = new Date().toISOString().split('T')[0];

  useEffect(() => {
    fetchDelegations();
    usersApi.getAll().then(r => setUsers(r.data || [])).catch(() => {});
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const fetchDelegations = async () => {
    try {
      const r = await delegationsApi.getMy();
      setDelegations(r.data || []);
    } catch { toast.error('Viga delegeerimiste laadimisel'); }
  };

  const handleCreate = async () => {
    if (!form?.delegateId || !form.startDate || !form.endDate) {
      toast.error('Täida kõik kohustuslikud väljad');
      return;
    }
    if (form.endDate < form.startDate) {
      toast.error('Lõppkuupäev peab olema alguskuupäevast hiljem');
      return;
    }
    setLoading(true);
    try {
      await delegationsApi.create({
        delegateId: parseInt(form.delegateId),
        startDate:  form.startDate,
        endDate:    form.endDate,
        reason:     form.reason || undefined,
      });
      toast.success('Delegeerimine loodud');
      setForm(null);
      await fetchDelegations();
    } catch (err) {
      toast.error(err?.response?.data?.message || 'Viga delegeerimise loomisel');
    } finally { setLoading(false); }
  };

  const handleCancel = async (id) => {
    try {
      await delegationsApi.cancel(id);
      toast.success('Delegeerimine tühistatud');
      setConfirmCancel(null);
      await fetchDelegations();
    } catch { toast.error('Viga tühistamisel'); }
  };

  return (
    <div className="md-root">
      <div className="md-header">
        <div>
          <div className="md-title">Kinnitamise delegeerimine</div>
          <div className="md-desc">
            Minu eemaloleku ajal kinnitab puhkusetaotlused valitud asendaja.
          </div>
        </div>
        {!form && (
          <button className="md-btn md-btn-primary" onClick={() => setForm({ delegateId: '', startDate: today, endDate: '', reason: '' })}>
            Lisa delegeerimine
          </button>
        )}
      </div>

      {form && (
        <div className="md-form">
          <div className="md-form-title">Uus delegeerimine</div>
          <div className="md-form-grid">
            <div className="md-field">
              <label className="md-label">Asendaja *</label>
              <select className="md-select" value={form.delegateId} onChange={e => setForm(f => ({ ...f, delegateId: e.target.value }))}>
                <option value="">Vali kasutaja...</option>
                {users.filter(u => u.isActive).map(u => (
                  <option key={u.id} value={u.id}>{u.fullName || `${u.firstName} ${u.lastName}`} — {u.department}</option>
                ))}
              </select>
            </div>
            <div className="md-field">
              <label className="md-label">Algus *</label>
              <input className="md-input" type="date" min={today} value={form.startDate} onChange={e => setForm(f => ({ ...f, startDate: e.target.value }))} />
            </div>
            <div className="md-field">
              <label className="md-label">Lõpp *</label>
              <input className="md-input" type="date" min={form.startDate || today} value={form.endDate} onChange={e => setForm(f => ({ ...f, endDate: e.target.value }))} />
            </div>
            <div className="md-field md-field-full">
              <label className="md-label">Põhjus (valikuline)</label>
              <input className="md-input" type="text" placeholder="nt. Puhkus, komandeering..." value={form.reason} onChange={e => setForm(f => ({ ...f, reason: e.target.value }))} />
            </div>
          </div>
          <div className="md-form-actions">
            <button className="md-btn md-btn-primary" onClick={handleCreate} disabled={loading}>
              {loading ? 'Salvestamine...' : 'Salvesta'}
            </button>
            <button className="md-btn md-btn-ghost" onClick={() => setForm(null)}>Tühista</button>
          </div>
        </div>
      )}

      {delegations.length === 0 ? (
        <div className="md-empty">Aktiivseid delegeerimisi pole.</div>
      ) : (
        <div className="md-list">
          {delegations.map(d => (
            <div key={d.id} className={`md-item ${d.isCurrentlyActive ? 'md-item-active' : ''}`}>
              <div className="md-item-left">
                {d.isCurrentlyActive && <span className="md-active-dot" />}
                <div>
                  <div className="md-item-name">{d.delegateName}</div>
                  <div className="md-item-dates">{fmtDate(d.startDate)} – {fmtDate(d.endDate)}</div>
                  {d.reason && <div className="md-item-reason">{d.reason}</div>}
                </div>
              </div>
              <div className="md-item-right">
                {d.isCurrentlyActive && <span className="md-status-chip">Aktiivne</span>}
                {confirmCancel === d.id ? (
                  <div className="md-confirm">
                    <span>Tühistada?</span>
                    <button className="md-btn md-btn-danger-xs" onClick={() => handleCancel(d.id)}>Jah</button>
                    <button className="md-btn md-btn-ghost-xs" onClick={() => setConfirmCancel(null)}>Ei</button>
                  </div>
                ) : (
                  <button className="md-btn md-btn-ghost-xs" onClick={() => setConfirmCancel(d.id)}>Tühista</button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
