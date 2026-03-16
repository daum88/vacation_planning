import React, { useState, useEffect } from 'react';
import { vacationRequestsApi, leaveTypesApi, usersApi, calendarApi, blackoutPeriodsApi, departmentCapacityApi } from '../../api/api';
import { useToast } from '../Toast/Toast';
import { countWorkingDays, countCalendarDays, getHolidaysInRange } from '../../utils/dateUtils';
import DateSuggester from '../DateSuggester/DateSuggester';
import './VacationRequestForm.css';

const VacationRequestForm = ({ onSuccess, editRequest, onCancel }) => {
  const [formData, setFormData] = useState({
    leaveTypeId: 1,
    startDate: '',
    endDate: '',
    comment: '',
    substituteName: '',
  });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [leaveTypes, setLeaveTypes] = useState([]);
  const [userBalance, setUserBalance] = useState(null);
  const [userDepartment, setUserDepartment] = useState('');
  const [conflicts, setConflicts] = useState(null);
  const [blackouts, setBlackouts] = useState([]);
  const [capacityWarning, setCapacityWarning] = useState(null);
  const [selectedFiles, setSelectedFiles] = useState([]);
  const toast = useToast();

  useEffect(() => {
    fetchLeaveTypes();
    fetchUserBalance();
    fetchBlackouts();
  }, []);

  useEffect(() => {
    if (editRequest) {
      setFormData({
        leaveTypeId: editRequest.leaveTypeId || 1,
        startDate: formatDateForInput(editRequest.startDate),
        endDate: formatDateForInput(editRequest.endDate),
        comment: editRequest.comment || '',
        substituteName: editRequest.substituteName || '',
      });
    } else {
      resetForm();
    }
  }, [editRequest]);

  useEffect(() => {
    if (formData.startDate && formData.endDate) {
      checkConflicts();
      checkCapacity();
    }
  }, [formData.startDate, formData.endDate]);

  const fetchLeaveTypes = async () => {
    try {
      const response = await leaveTypesApi.getAll();
      setLeaveTypes(response.data);
    } catch (err) {
      console.error('Error fetching leave types:', err);
    }
  };

  const fetchUserBalance = async () => {
    try {
      const userId = localStorage.getItem('userId') || '1';
      const [balanceRes, userRes] = await Promise.all([
        usersApi.getBalance(userId),
        usersApi.getById(userId),
      ]);
      setUserBalance(balanceRes.data);
      setUserDepartment(userRes.data.department || '');
    } catch (err) {
      console.error('Error fetching balance:', err);
    }
  };

  const fetchBlackouts = async () => {
    try {
      const response = await blackoutPeriodsApi.getAll();
      setBlackouts(response.data);
    } catch (err) {
      console.error('Error fetching blackouts:', err);
    }
  };

  const checkConflicts = async () => {
    try {
      const response = await calendarApi.checkConflicts(
        formData.startDate,
        formData.endDate,
        editRequest?.id
      );
      setConflicts(response.data);
    } catch (err) {
      console.error('Error checking conflicts:', err);
    }
  };

  const checkCapacity = async () => {
    if (!userDepartment) return;
    try {
      const userId = parseInt(localStorage.getItem('userId') || '1');
      const res = await departmentCapacityApi.check(
        userDepartment,
        formData.startDate,
        formData.endDate,
        userId
      );
      if (res.data.hasLimit && res.data.wouldExceed) {
        setCapacityWarning(res.data);
      } else {
        setCapacityWarning(null);
      }
    } catch {
      setCapacityWarning(null);
    }
  };

  const formatDateForInput = (dateString) => {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
  };

  const getActiveBlackouts = () => {
    if (!formData.startDate || !formData.endDate) return [];
    const start = new Date(formData.startDate);
    const end = new Date(formData.endDate);
    return blackouts.filter(b => {
      const bs = new Date(b.startDate);
      const be = new Date(b.endDate);
      return bs <= end && be >= start;
    });
  };

  const validateForm = () => {
    const newErrors = {};
    const today = new Date().toISOString().split('T')[0];

    if (!formData.startDate) {
      newErrors.startDate = 'Alguskuupäev on kohustuslik';
    } else if (!editRequest && formData.startDate < today) {
      newErrors.startDate = 'Alguskuupäev ei saa olla minevikus';
    }

    if (!formData.endDate) {
      newErrors.endDate = 'Lõppkuupäev on kohustuslik';
    }

    if (formData.startDate && formData.endDate && formData.endDate < formData.startDate) {
      newErrors.endDate = 'Lõppkuupäev ei saa olla enne alguskuupäeva';
    }

    const calDays = countCalendarDays(formData.startDate, formData.endDate);
    if (calDays > 90) {
      newErrors.general = 'Puhkus ei saa olla pikem kui 90 päeva';
    }

    const wDays = countWorkingDays(formData.startDate, formData.endDate);
    const selectedLeaveType = leaveTypes.find(lt => lt.id === parseInt(formData.leaveTypeId));
    if (selectedLeaveType?.isPaid && userBalance && wDays > userBalance.remainingLeaveDays) {
      newErrors.general = `Pole piisavalt puhkusepäevi. Jääk: ${userBalance.remainingLeaveDays} tööpäeva.`;
    }

    if (selectedLeaveType?.requiresAttachment && selectedFiles.length === 0 && !editRequest) {
      newErrors.files = 'See puhkuse tüüp nõuab manuse lisamist';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
    setErrors(prev => {
      const next = { ...prev };
      delete next[name];
      delete next.general;
      return next;
    });
  };

  const handleFileSelect = (e) => {
    const files = Array.from(e.target.files);
    const maxSize = 10 * 1024 * 1024;
    const allowedTypes = [
      'application/pdf', 'image/jpeg', 'image/png', 'image/gif',
      'application/msword',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    ];
    const validFiles = files.filter(file => {
      if (file.size > maxSize) { toast.error(`${file.name} on liiga suur (max 10MB)`); return false; }
      if (!allowedTypes.includes(file.type)) { toast.error(`${file.name} failitüüp pole lubatud`); return false; }
      return true;
    });
    setSelectedFiles(prev => [...prev, ...validFiles]);
    setErrors(prev => { const next = { ...prev }; delete next.files; return next; });
  };

  const removeFile = (index) => {
    setSelectedFiles(prev => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) { toast.error('Palun paranda vormis olevad vead'); return; }
    setLoading(true);
    try {
      let requestId;
      if (editRequest) {
        await vacationRequestsApi.update(editRequest.id, formData);
        requestId = editRequest.id;
        toast.success('Taotlus edukalt uuendatud');
      } else {
        const response = await vacationRequestsApi.create(formData);
        requestId = response.data.id;
        toast.success('Taotlus edukalt esitatud');
      }
      if (selectedFiles.length > 0) {
        for (const file of selectedFiles) {
          try { await vacationRequestsApi.uploadAttachment(requestId, file); }
          catch (err) { toast.warning(`Viga faili ${file.name} üleslaadimisel`); }
        }
      }
      resetForm();
      fetchUserBalance();
      onSuccess();
    } catch (error) {
      const errorMessage = error.response?.data?.message || 'Viga taotluse esitamisel';
      toast.error(errorMessage);
      setErrors({ general: errorMessage });
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({ leaveTypeId: 1, startDate: '', endDate: '', comment: '', substituteName: '' });
    setSelectedFiles([]);
    setErrors({});
    setConflicts(null);
  };

  const handleCancelEdit = () => { resetForm(); onCancel(); };

  const workingDays = countWorkingDays(formData.startDate, formData.endDate);
  const calendarDays = countCalendarDays(formData.startDate, formData.endDate);
  const holidaysInRange = formData.startDate && formData.endDate
    ? getHolidaysInRange(formData.startDate, formData.endDate)
    : [];
  const activeBlackouts = getActiveBlackouts();
  const selectedLeaveType = leaveTypes.find(lt => lt.id === parseInt(formData.leaveTypeId));

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

        <section className="form-section">
          <div className="form-section-title">Puhkuse liik</div>
          <div className="form-group">
            <label htmlFor="leaveTypeId" className="form-label">Puhkuse liik *</label>
            <select
              id="leaveTypeId" name="leaveTypeId"
              value={formData.leaveTypeId} onChange={handleChange}
              className={`form-select ${errors.leaveTypeId ? 'error' : ''}`}
              disabled={loading}
            >
              {leaveTypes.map(type => (
                <option key={type.id} value={type.id}>
                  {type.name}{!type.isPaid ? ' (tasustamata)' : ''}{type.requiresAttachment ? ' (nõuab manust)' : ''}
                </option>
              ))}
            </select>
            {selectedLeaveType && (
              <div className="leave-type-info" style={{ borderLeftColor: selectedLeaveType.color }}>
                {selectedLeaveType.description}
                {selectedLeaveType.requiresAttachment && <div className="info-badge">Manus nõutud</div>}
                {!selectedLeaveType.requiresApproval && <div className="info-badge success">Automaatne kinnitamine</div>}
              </div>
            )}
          </div>
        </section>

        <section className="form-section">
          <div className="form-section-title">Kuupäevad</div>

          {userBalance && (
            <DateSuggester
              remainingDays={userBalance.remainingLeaveDays}
              blackouts={blackouts}
              onSelect={(start, end) => {
                setFormData(prev => ({ ...prev, startDate: start, endDate: end }));
                setErrors(prev => { const n = { ...prev }; delete n.startDate; delete n.endDate; return n; });
              }}
            />
          )}

          {/* Capacity warning */}
          {capacityWarning && (
            <div className="capacity-warning">
              <strong>Osakonna limiit täis:</strong> {capacityWarning.department} osakonnal on juba {capacityWarning.currentCount}/{capacityWarning.maxConcurrent} inimest puhkusel sellel perioodil.
              Vali teised kuupäevad või konsulteeri juhiga.
            </div>
          )}

          <div className="form-row">
            <div className="form-group">
              <label htmlFor="startDate" className="form-label">Alguskuupäev *</label>
              <span className="field-hint">Millal soovid puhkust alustada?</span>
              <input type="date" id="startDate" name="startDate"
                value={formData.startDate} onChange={handleChange}
                className={`form-input ${errors.startDate ? 'error' : ''}`}
                disabled={loading} required />
              {errors.startDate && <span className="error-text">{errors.startDate}</span>}
            </div>
            <div className="form-group">
              <label htmlFor="endDate" className="form-label">Lõppkuupäev *</label>
              <span className="field-hint">Mis kuupäevani puhkad?</span>
              <input type="date" id="endDate" name="endDate"
                value={formData.endDate} onChange={handleChange}
                className={`form-input ${errors.endDate ? 'error' : ''}`}
                disabled={loading} required />
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
                  Pärast taotlust jääb: {Math.max(0, userBalance.remainingLeaveDays - workingDays)} tööpäeva
                </span>
              )}
            </div>
          )}

          {conflicts && conflicts.hasConflicts && (
            <div className="conflicts-warning">
              <strong>Meeskonna konfliktid:</strong>
              <p>{conflicts.conflictCount} kolleegi on sellel perioodil puhkusel:</p>
              <ul>
                {conflicts.conflicts.map((c, idx) => (
                  <li key={idx}>
                    {c.userName} ({c.department}) — {new Date(c.startDate).toLocaleDateString('et-EE')} kuni {new Date(c.endDate).toLocaleDateString('et-EE')}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </section>

        <section className="form-section">
          <div className="form-section-title">Asendaja</div>
          <div className="form-group">
            <label htmlFor="substituteName" className="form-label">Asendaja nimi</label>
            <span className="field-hint">Kes katab sinu tööd äraoleku ajal?</span>
            <input type="text" id="substituteName" name="substituteName"
              value={formData.substituteName} onChange={handleChange}
              className="form-input"
              placeholder="Nimi või 'Pole asendajat'"
              maxLength={200}
              disabled={loading} />
          </div>
        </section>

        <section className="form-section">
          <div className="form-section-title">Kommentaar</div>
          <div className="form-group">
            <label htmlFor="comment" className="form-label">Lisa kommentaar (valikuline)</label>
            <span className="field-hint">Lisa märkus juhile — näiteks eripäevade kohta.</span>
            <textarea id="comment" name="comment"
              value={formData.comment} onChange={handleChange}
              className="form-textarea" rows="3" maxLength="500"
              placeholder="Näiteks: Reisile minek 25. juunil, palun kinnita kiiresti."
              disabled={loading} />
            <div className="char-count">{formData.comment.length}/500</div>
          </div>
        </section>

        {!editRequest && (
          <section className="form-section">
            <div className="form-section-title">Manused</div>
            <div className="form-group">
              <label className="form-label">
                Failid {selectedLeaveType?.requiresAttachment && '*'}
              </label>
              <div className="file-upload-area">
                <input type="file" id="fileInput" multiple
                  accept=".pdf,.jpg,.jpeg,.png,.gif,.doc,.docx"
                  onChange={handleFileSelect} className="file-input" disabled={loading} />
                <label htmlFor="fileInput" className="file-upload-button">Lisa failid</label>
                <span className="file-hint">PDF, pildid, Word (max 10MB)</span>
              </div>
              {errors.files && <span className="error-text">{errors.files}</span>}
              {selectedFiles.length > 0 && (
                <div className="selected-files">
                  {selectedFiles.map((file, index) => (
                    <div key={index} className="file-item">
                      <span className="file-name">{file.name}</span>
                      <span className="file-size">({(file.size / 1024).toFixed(1)} KB)</span>
                      <button type="button" onClick={() => removeFile(index)}
                        className="file-remove" disabled={loading}>×</button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </section>
        )}

        <div className="form-actions">
          <div className="submit-context">
            {workingDays > 0 ? (
              <>
                <div>Kasutab: <strong>{workingDays} tööpäeva</strong>{calendarDays !== workingDays && ` (${calendarDays} kp kokku)`}</div>
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
              <button type="button" onClick={handleCancelEdit}
                className="btn-secondary" disabled={loading}>Tühista</button>
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
