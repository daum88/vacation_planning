import React, { useState, useEffect } from 'react';
import {
  vacationRequestsApi, leaveTypesApi, usersApi,
  calendarApi, blackoutPeriodsApi, departmentCapacityApi
} from '../../api/api';
import { useToast } from '../Toast/Toast';
import { countWorkingDays, countCalendarDays, getHolidaysInRange } from '../../utils/dateUtils';
import DateSuggester from '../DateSuggester/DateSuggester';
import DatePicker from '../DatePicker/DatePicker';
import CustomSelect from '../CustomSelect/CustomSelect';
import EmployeeComboBox from '../EmployeeComboBox/EmployeeComboBox';
import './VacationRequestForm.css';

const VacationRequestForm = ({ onSuccess, editRequest, onCancel }) => {
  const [formData, setFormData] = useState({
    leaveTypeId: 1,
    startDate: '',
    endDate: '',
    comment: '',
    substituteName: '',
  });
  const [errors, setErrors]               = useState({});
  const [loading, setLoading]             = useState(false);
  const [leaveTypes, setLeaveTypes]       = useState([]);
  const [userBalance, setUserBalance]     = useState(null);
  const [userDepartment, setUserDepartment] = useState('');
  const [currentUserId, setCurrentUserId] = useState(1);
  const [conflicts, setConflicts]         = useState(null);
  const [blackouts, setBlackouts]         = useState([]);
  const [capacityWarning, setCapacityWarning] = useState(null);
  const [selectedFiles, setSelectedFiles] = useState([]);
  const toast = useToast();

  useEffect(() => {
    const uid = parseInt(localStorage.getItem('userId') || '1');
    setCurrentUserId(uid);
    fetchLeaveTypes();
    fetchUserBalance(uid);
    fetchBlackouts();
  }, []);

  useEffect(() => {
    if (editRequest) {
      setFormData({
        leaveTypeId: editRequest.leaveTypeId || 1,
        startDate: formatDateStr(editRequest.startDate),
        endDate:   formatDateStr(editRequest.endDate),
        comment:   editRequest.comment || '',
        substituteName: editRequest.substituteName || '',
      });
    } else {
      resetForm();
    }
  }, [editRequest]);

  useEffect(() => {
    if (formData.startDate && formData.endDate) {
      checkConflicts();
      if (userDepartment) checkCapacity();
    } else {
      setConflicts(null);
      setCapacityWarning(null);
    }
  }, [formData.startDate, formData.endDate, userDepartment]); // eslint-disable-line

  const fetchLeaveTypes = async () => {
    try {
      const r = await leaveTypesApi.getAll();
      setLeaveTypes(r.data);
    } catch (e) { console.error(e); }
  };

  const fetchUserBalance = async (uid) => {
    try {
      const [balRes, userRes] = await Promise.all([
        usersApi.getBalance(uid),
        usersApi.getById(uid),
      ]);
      setUserBalance(balRes.data);
      setUserDepartment(userRes.data.department || '');
    } catch (e) { console.error(e); }
  };

  const fetchBlackouts = async () => {
    try {
      const r = await blackoutPeriodsApi.getAll();
      setBlackouts(r.data);
    } catch (e) { console.error(e); }
  };

  const checkConflicts = async () => {
    try {
      const r = await calendarApi.checkConflicts(formData.startDate, formData.endDate, editRequest?.id);
      setConflicts(r.data);
    } catch (e) { console.error(e); }
  };

  const checkCapacity = async () => {
    try {
      const r = await departmentCapacityApi.check(userDepartment, formData.startDate, formData.endDate, currentUserId);
      setCapacityWarning(r.data.hasLimit && r.data.wouldExceed ? r.data : null);
    } catch { setCapacityWarning(null); }
  };

  const formatDateStr = (s) => s ? new Date(s).toISOString().split('T')[0] : '';

  const getActiveBlackouts = () => {
    if (!formData.startDate || !formData.endDate) return [];
    return blackouts.filter(b =>
      new Date(b.startDate) <= new Date(formData.endDate) &&
      new Date(b.endDate)   >= new Date(formData.startDate)
    );
  };

  const validateForm = () => {
    const next = {};
    const today = new Date().toISOString().split('T')[0];

    if (!formData.startDate) { next.startDate = 'Alguskuupäev on kohustuslik'; }
    else if (!editRequest && formData.startDate < today) { next.startDate = 'Alguskuupäev ei saa olla minevikus'; }

    if (!formData.endDate) { next.endDate = 'Lõppkuupäev on kohustuslik'; }

    if (formData.startDate && formData.endDate && formData.endDate < formData.startDate) {
      next.endDate = 'Lõppkuupäev ei saa olla enne alguskuupäeva';
    }

    const cal = countCalendarDays(formData.startDate, formData.endDate);
    if (cal > 90) next.general = 'Puhkus ei saa olla pikem kui 90 kalendripäeva';

    const wDays = countWorkingDays(formData.startDate, formData.endDate);
    const lt = leaveTypes.find(l => l.id === parseInt(formData.leaveTypeId));
    if (lt?.isPaid && userBalance && wDays > userBalance.remainingLeaveDays) {
      next.general = `Pole piisavalt puhkusepäevi. Jääk: ${userBalance.remainingLeaveDays} tööpäeva.`;
    }

    if (lt?.requiresAttachment && selectedFiles.length === 0 && !editRequest) {
      next.files = 'See puhkuse tüüp nõuab manuse lisamist';
    }

    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const setField = (name, value) => {
    setFormData(prev => ({ ...prev, [name]: value }));
    setErrors(prev => { const n = { ...prev }; delete n[name]; delete n.general; return n; });
  };

  const handleFileSelect = (e) => {
    const maxSize = 10 * 1024 * 1024;
    const allowed = ['application/pdf','image/jpeg','image/png','image/gif',
      'application/msword','application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
    Array.from(e.target.files).forEach(f => {
      if (f.size > maxSize) { toast.error(`${f.name}: liiga suur (max 10MB)`); return; }
      if (!allowed.includes(f.type)) { toast.error(`${f.name}: failitüüp pole lubatud`); return; }
      setSelectedFiles(prev => [...prev, f]);
    });
    setErrors(prev => { const n = { ...prev }; delete n.files; return n; });
  };

  const removeFile = (i) => setSelectedFiles(prev => prev.filter((_, idx) => idx !== i));

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) { toast.error('Palun paranda vormis olevad vead'); return; }
    setLoading(true);
    try {
      let reqId;
      if (editRequest) {
        await vacationRequestsApi.update(editRequest.id, formData);
        reqId = editRequest.id;
        toast.success('Taotlus edukalt uuendatud');
      } else {
        const r = await vacationRequestsApi.create(formData);
        reqId = r.data.id;
        toast.success('Taotlus edukalt esitatud');
      }
      for (const f of selectedFiles) {
        try { await vacationRequestsApi.uploadAttachment(reqId, f); }
        catch { toast.warning(`Viga faili ${f.name} üleslaadimisel`); }
      }
      resetForm();
      fetchUserBalance(currentUserId);
      onSuccess();
    } catch (err) {
      const msg = err.response?.data?.message || 'Viga taotluse esitamisel';
      toast.error(msg);
      setErrors({ general: msg });
    } finally { setLoading(false); }
  };

  const resetForm = () => {
    setFormData({ leaveTypeId: 1, startDate: '', endDate: '', comment: '', substituteName: '' });
    setSelectedFiles([]);
    setErrors({});
    setConflicts(null);
    setCapacityWarning(null);
  };

  // Derived
  const workingDays    = countWorkingDays(formData.startDate, formData.endDate);
  const calendarDays   = countCalendarDays(formData.startDate, formData.endDate);
  const holidaysInRange = formData.startDate && formData.endDate
    ? getHolidaysInRange(formData.startDate, formData.endDate) : [];
  const activeBlackouts  = getActiveBlackouts();
  const selectedLeaveType = leaveTypes.find(lt => lt.id === parseInt(formData.leaveTypeId));
  const today = new Date().toISOString().split('T')[0];

  // CustomSelect options for leave types
  const leaveTypeOptions = leaveTypes.map(lt => ({
    value: String(lt.id),
    label: lt.name,
    color: lt.color,
    hint: !lt.isPaid ? 'tasustamata' : lt.requiresAttachment ? 'manus nõutud' : '',
  }));

  return (
    <div className="vacation-request-form-container">
      <div className="form-header">
        <h2>{editRequest ? 'Muuda taotlust' : 'Uus puhkuse taotlus'}</h2>
        <p className="form-subtitle">Planeeri kuupäevad, lisa kontekst ja saada taotlus kinnitamiseks.</p>
      </div>

      <form onSubmit={handleSubmit} className="vacation-request-form">
        {errors.general && <div className="form-error-banner">{errors.general}</div>}

        {/* Blackout warning */}
        {activeBlackouts.length > 0 && (
          <div className="blackout-warning">
            <strong>Blokeeritud periood:</strong> Valitud kuupäevad kattuvad ettevõtte suletud perioodiga:
            {activeBlackouts.map(b => (
              <div key={b.id} className="blackout-item">
                <strong>{b.name}</strong> ({new Date(b.startDate).toLocaleDateString('et-EE')} – {new Date(b.endDate).toLocaleDateString('et-EE')})
                {b.description && <span> — {b.description}</span>}
              </div>
            ))}
          </div>
        )}

        {/* ── Puhkuse liik ─────────────────────── */}
        <section className="form-section">
          <div className="form-section-title">Puhkuse liik</div>
          <div className="form-group">
            <label className="form-label">Puhkuse liik *</label>
            <CustomSelect
              options={leaveTypeOptions}
              value={String(formData.leaveTypeId)}
              onChange={v => setField('leaveTypeId', parseInt(v))}
              disabled={loading}
              error={!!errors.leaveTypeId}
            />
            {selectedLeaveType && (
              <div className="leave-type-info" style={{ borderLeftColor: selectedLeaveType.color }}>
                {selectedLeaveType.description}
                {selectedLeaveType.requiresAttachment && <div className="info-badge">Manus nõutud</div>}
                {!selectedLeaveType.requiresApproval  && <div className="info-badge success">Automaatne kinnitamine</div>}
              </div>
            )}
          </div>
        </section>

        {/* ── Kuupäevad ────────────────────────── */}
        <section className="form-section">
          <div className="form-section-title">Kuupäevad</div>

          {userBalance && (
            <DateSuggester
              remainingDays={userBalance.remainingLeaveDays}
              blackouts={blackouts}
              department={userDepartment}
              onSelect={(start, end) => {
                setFormData(prev => ({ ...prev, startDate: start, endDate: end }));
                setErrors(prev => { const n = { ...prev }; delete n.startDate; delete n.endDate; return n; });
              }}
            />
          )}

          {capacityWarning && (
            <div className="capacity-warning">
              <strong>Osakonna limiit täis:</strong> {capacityWarning.department} osakonnal on juba {capacityWarning.currentCount}/{capacityWarning.maxConcurrent} töötajat puhkusel sellel perioodil.
              Vali teised kuupäevad või konsulteeri juhiga.
            </div>
          )}

          <div className="form-row">
            <div className="form-group">
              <label className="form-label">Alguskuupäev *</label>
              <span className="field-hint">Millal soovid puhkust alustada?</span>
              <DatePicker
                value={formData.startDate}
                onChange={v => setField('startDate', v)}
                minDate={editRequest ? '' : today}
                maxDate={formData.endDate || ''}
                placeholder="Vali alguskuupäev"
                disabled={loading}
                error={!!errors.startDate}
                blackouts={blackouts}
                rangeEnd={formData.endDate}
              />
              {errors.startDate && <span className="error-text">{errors.startDate}</span>}
            </div>
            <div className="form-group">
              <label className="form-label">Lõppkuupäev *</label>
              <span className="field-hint">Mis kuupäevani puhkad?</span>
              <DatePicker
                value={formData.endDate}
                onChange={v => setField('endDate', v)}
                minDate={formData.startDate || today}
                placeholder="Vali lõppkuupäev"
                disabled={loading}
                error={!!errors.endDate}
                blackouts={blackouts}
                rangeStart={formData.startDate}
              />
              {errors.endDate && <span className="error-text">{errors.endDate}</span>}
            </div>
          </div>

          {workingDays > 0 && (
            <div className="days-indicator">
              <span className="days-badge">{workingDays} tööpäeva</span>
              {calendarDays !== workingDays && (
                <span className="days-remaining-preview">{calendarDays} kalendripäeva kokku</span>
              )}
              {holidaysInRange.length > 0 && (
                <span className="days-remaining-preview holiday-note">
                  Sisaldab {holidaysInRange.length} riigipüha: {holidaysInRange.map(h => h.name).join(', ')}
                </span>
              )}
              {selectedLeaveType?.isPaid && userBalance && (
                <span className="days-remaining-preview">
                  Pärast taotlust jääb: <strong>{Math.max(0, userBalance.remainingLeaveDays - workingDays)}</strong> tööpäeva
                </span>
              )}
            </div>
          )}

          {conflicts?.hasConflicts && (
            <div className="conflicts-warning">
              <strong>Meeskonna konfliktid:</strong> {conflicts.conflictCount} kolleegi on sellel perioodil puhkusel.
              <ul>
                {conflicts.conflicts.map((c, i) => (
                  <li key={i}>
                    {c.userName} ({c.department}) — {new Date(c.startDate).toLocaleDateString('et-EE')} kuni {new Date(c.endDate).toLocaleDateString('et-EE')}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </section>

        {/* ── Asendaja ─────────────────────────── */}
        <section className="form-section">
          <div className="form-section-title">Asendaja</div>
          <div className="form-group">
            <label className="form-label">Asendaja äraoleku ajal</label>
            <span className="field-hint">Kes katab sinu tööülesanded puhkuse ajal?</span>
            <EmployeeComboBox
              value={formData.substituteName}
              onChange={v => setField('substituteName', v)}
              disabled={loading}
              excludeUserId={currentUserId}
            />
          </div>
        </section>

        {/* ── Kommentaar ───────────────────────── */}
        <section className="form-section">
          <div className="form-section-title">Kommentaar</div>
          <div className="form-group">
            <label className="form-label">Lisa kommentaar (valikuline)</label>
            <span className="field-hint">Näiteks eripäevade kohta, kiiruse soov vms.</span>
            <textarea
              value={formData.comment}
              onChange={e => setField('comment', e.target.value)}
              className="form-textarea" rows="3" maxLength="500"
              placeholder="Näiteks: Reisile minek 25. juunil, palun kinnita kiiresti."
              disabled={loading}
            />
            <div className="char-count">{formData.comment.length}/500</div>
          </div>
        </section>

        {/* ── Manused ──────────────────────────── */}
        {!editRequest && (
          <section className="form-section">
            <div className="form-section-title">Manused</div>
            <div className="form-group">
              <label className="form-label">Failid{selectedLeaveType?.requiresAttachment ? ' *' : ''}</label>
              <div className="file-upload-area">
                <input type="file" id="fileInput" multiple
                  accept=".pdf,.jpg,.jpeg,.png,.gif,.doc,.docx"
                  onChange={handleFileSelect} className="file-input" disabled={loading} />
                <label htmlFor="fileInput" className="file-upload-button">Lisa failid</label>
                <span className="file-hint">PDF, pildid, Word (max 10 MB)</span>
              </div>
              {errors.files && <span className="error-text">{errors.files}</span>}
              {selectedFiles.length > 0 && (
                <div className="selected-files">
                  {selectedFiles.map((f, i) => (
                    <div key={i} className="file-item">
                      <span className="file-name">{f.name}</span>
                      <span className="file-size">({(f.size / 1024).toFixed(1)} KB)</span>
                      <button type="button" className="file-remove" onClick={() => removeFile(i)} disabled={loading}>×</button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </section>
        )}

        {/* ── Actions ──────────────────────────── */}
        <div className="form-actions">
          <div className="submit-context">
            {workingDays > 0 ? (
              <>
                <div>Kasutab: <strong>{workingDays} tööpäeva</strong>
                  {calendarDays !== workingDays && ` (${calendarDays} kp)`}
                </div>
                {selectedLeaveType?.isPaid && userBalance && (
                  <div>Alles jääb: <strong>{Math.max(0, userBalance.remainingLeaveDays - workingDays)} tööpäeva</strong></div>
                )}
              </>
            ) : (
              <div>Vali kuupäevad, et näha tööpäevade arvu.</div>
            )}
          </div>
          <div className="submit-buttons">
            {editRequest && (
              <button type="button" className="btn-secondary" disabled={loading}
                onClick={() => { resetForm(); onCancel(); }}>
                Tühista
              </button>
            )}
            <button type="submit" className="btn-primary"
              disabled={loading || Object.keys(errors).length > 0}>
              {loading ? 'Salvestamine...' : editRequest ? 'Uuenda taotlust' : 'Taotle puhkust'}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
};

export default VacationRequestForm;
