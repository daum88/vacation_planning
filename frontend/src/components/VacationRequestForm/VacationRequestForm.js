import React, { useState, useEffect } from 'react';
import { vacationRequestsApi, leaveTypesApi, usersApi, calendarApi } from '../../api/api';
import { useToast } from '../Toast/Toast';
import './VacationRequestForm.css';

const VacationRequestForm = ({ onSuccess, editRequest, onCancel }) => {
  const [formData, setFormData] = useState({
    leaveTypeId: 1,
    startDate: '',
    endDate: '',
    comment: ''
  });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [leaveTypes, setLeaveTypes] = useState([]);
  const [userBalance, setUserBalance] = useState(null);
  const [conflicts, setConflicts] = useState(null);
  const [selectedFiles, setSelectedFiles] = useState([]);
  const toast = useToast();

  useEffect(() => {
    fetchLeaveTypes();
    fetchUserBalance();
  }, []);

  useEffect(() => {
    if (editRequest) {
      setFormData({
        leaveTypeId: editRequest.leaveTypeId || 1,
        startDate: formatDateForInput(editRequest.startDate),
        endDate: formatDateForInput(editRequest.endDate),
        comment: editRequest.comment || ''
      });
    } else {
      resetForm();
    }
  }, [editRequest]);

  useEffect(() => {
    if (formData.startDate && formData.endDate) {
      checkConflicts();
    }
  }, [formData.startDate, formData.endDate]);

  const fetchLeaveTypes = async () => {
    try {
      const response = await leaveTypesApi.getAll();
      setLeaveTypes(response.data);
    } catch (err) {
      console.error('Error fetching leave types:', err);
      toast.error('Viga puhkusetüüpide laadimisel');
    }
  };

  const fetchUserBalance = async () => {
    try {
      const userId = localStorage.getItem('userId') || '1';
      const response = await usersApi.getBalance(userId);
      setUserBalance(response.data);
    } catch (err) {
      console.error('Error fetching balance:', err);
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

  const formatDateForInput = (dateString) => {
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
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

    const daysCount = calculateDays();
    if (daysCount > 90) {
      newErrors.general = 'Puhkus ei saa olla pikem kui 90 päeva';
    }

    // Check balance for paid leave
    const selectedLeaveType = leaveTypes.find(lt => lt.id === parseInt(formData.leaveTypeId));
    if (selectedLeaveType?.isPaid && userBalance && daysCount > userBalance.remainingLeaveDays) {
      newErrors.general = `Sul ei ole piisavalt puhkusepäevi. Jääk: ${userBalance.remainingLeaveDays} päeva.`;
    }

    // Check if attachment is required
    if (selectedLeaveType?.requiresAttachment && selectedFiles.length === 0 && !editRequest) {
      newErrors.files = 'See puhkuse tüüp nõuab manuse lisamist';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const calculateDays = () => {
    if (!formData.startDate || !formData.endDate) return 0;
    const start = new Date(formData.startDate);
    const end = new Date(formData.endDate);
    return Math.floor((end - start) / (1000 * 60 * 60 * 24)) + 1;
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
    // Clear error for this field
    setErrors(prev => ({
      ...prev,
      [name]: undefined,
      general: undefined
    }));
  };

  const handleFileSelect = (e) => {
    const files = Array.from(e.target.files);
    const maxSize = 10 * 1024 * 1024; // 10MB
    const allowedTypes = [
      'application/pdf',
      'image/jpeg',
      'image/png',
      'image/gif',
      'application/msword',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    ];

    const validFiles = files.filter(file => {
      if (file.size > maxSize) {
        toast.error(`${file.name} on liiga suur (max 10MB)`);
        return false;
      }
      if (!allowedTypes.includes(file.type)) {
        toast.error(`${file.name} failitüüp ei ole lubatud`);
        return false;
      }
      return true;
    });

    setSelectedFiles(prev => [...prev, ...validFiles]);
    setErrors(prev => ({ ...prev, files: undefined }));
  };

  const removeFile = (index) => {
    setSelectedFiles(prev => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validateForm()) {
      toast.error('Palun paranda vormis olevad vead');
      return;
    }

    setLoading(true);

    try {
      let requestId;

      if (editRequest) {
        await vacationRequestsApi.update(editRequest.id, formData);
        requestId = editRequest.id;
        toast.success('Taotlus edukalt uuendatud! ✓');
      } else {
        const response = await vacationRequestsApi.create(formData);
        requestId = response.data.id;
        toast.success('Taotlus edukalt esitatud! ✓');
      }

      // Upload files if any
      if (selectedFiles.length > 0) {
        for (const file of selectedFiles) {
          try {
            await vacationRequestsApi.uploadAttachment(requestId, file);
          } catch (err) {
            console.error('Error uploading file:', err);
            toast.warning(`Viga faili ${file.name} üleslaadimisel`);
          }
        }
      }

      resetForm();
      fetchUserBalance(); // Refresh balance
      onSuccess();
    } catch (error) {
      console.error('Error submitting request:', error);
      const errorMessage = error.response?.data?.message || 'Viga taotluse esitamisel';
      toast.error(errorMessage);
      setErrors({ general: errorMessage });
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({
      leaveTypeId: 1,
      startDate: '',
      endDate: '',
      comment: ''
    });
    setSelectedFiles([]);
    setErrors({});
    setConflicts(null);
  };

  const handleCancelEdit = () => {
    resetForm();
    onCancel();
  };

  const daysCount = calculateDays();
  const selectedLeaveType = leaveTypes.find(lt => lt.id === parseInt(formData.leaveTypeId));

  return (
    <div className="vacation-request-form-container">
      <div className="form-header">
        <h2>{editRequest ? 'Muuda taotlust' : 'Uus puhkusetaotlus'}</h2>
        {userBalance && (
          <div className="balance-widget">
            <div className="balance-item">
              <span className="balance-label">Jääk:</span>
              <span className="balance-value">{userBalance.remainingLeaveDays} päeva</span>
            </div>
            <div className="balance-details">
              <small>Aastane: {userBalance.annualLeaveDays} | Kasutatud: {userBalance.usedLeaveDays}</small>
            </div>
          </div>
        )}
      </div>

      <form onSubmit={handleSubmit} className="vacation-request-form">
        {errors.general && (
          <div className="form-error-banner">
            {errors.general}
          </div>
        )}

        <div className="form-group">
          <label htmlFor="leaveTypeId" className="form-label">
            Puhkuse liik *
          </label>
          <select
            id="leaveTypeId"
            name="leaveTypeId"
            value={formData.leaveTypeId}
            onChange={handleChange}
            className={`form-select ${errors.leaveTypeId ? 'error' : ''}`}
            disabled={loading}
          >
            {leaveTypes.map(type => (
              <option key={type.id} value={type.id}>
                {type.name} {!type.isPaid && '(tasustamata)'} {type.requiresAttachment && '(nõuab manust)'}
              </option>
            ))}
          </select>
          {selectedLeaveType && (
            <div className="leave-type-info" style={{ borderLeftColor: selectedLeaveType.color }}>
              {selectedLeaveType.description}
              {selectedLeaveType.requiresAttachment && (
                <div className="info-badge">Manus nõutud</div>
              )}
              {!selectedLeaveType.requiresApproval && (
                <div className="info-badge success">Automaatne kinnitamine</div>
              )}
            </div>
          )}
          {errors.leaveTypeId && <span className="error-text">{errors.leaveTypeId}</span>}
        </div>

        <div className="form-row">
          <div className="form-group">
            <label htmlFor="startDate" className="form-label">
              Alguskuupäev *
            </label>
            <input
              type="date"
              id="startDate"
              name="startDate"
              value={formData.startDate}
              onChange={handleChange}
              className={`form-input ${errors.startDate ? 'error' : ''}`}
              disabled={loading}
              required
            />
            {errors.startDate && <span className="error-text">{errors.startDate}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="endDate" className="form-label">
              Lõppkuupäev *
            </label>
            <input
              type="date"
              id="endDate"
              name="endDate"
              value={formData.endDate}
              onChange={handleChange}
              className={`form-input ${errors.endDate ? 'error' : ''}`}
              disabled={loading}
              required
            />
            {errors.endDate && <span className="error-text">{errors.endDate}</span>}
          </div>
        </div>

        {daysCount > 0 && (
          <div className="days-indicator">
            <span className="days-badge">Valitud: {daysCount} {daysCount === 1 ? 'päev' : 'päeva'}</span>
            {selectedLeaveType?.isPaid && userBalance && (
              <span className="days-remaining-preview">
                Pärast taotlust jääb: {Math.max(0, userBalance.remainingLeaveDays - daysCount)} päeva
              </span>
            )}
          </div>
        )}

        {conflicts && conflicts.hasConflicts && (
          <div className="conflicts-warning">
            <strong>Meeskonna konfliktid:</strong>
            <p>{conflicts.conflictCount} inimest on sellel perioodil juba puhkusel:</p>
            <ul>
              {conflicts.conflicts.map((conflict, idx) => (
                <li key={idx}>
                  {conflict.userName} ({conflict.department}) - {new Date(conflict.startDate).toLocaleDateString('et-EE')} kuni {new Date(conflict.endDate).toLocaleDateString('et-EE')}
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className="form-group">
          <label htmlFor="comment" className="form-label">
            Lisa kommentaar (valikuline)
          </label>
          <textarea
            id="comment"
            name="comment"
            value={formData.comment}
            onChange={handleChange}
            className="form-textarea"
            rows="3"
            maxLength="500"
            placeholder="Lisa täiendav info..."
            disabled={loading}
          />
          <div className="char-count">{formData.comment.length}/500</div>
        </div>

        {!editRequest && (
          <div className="form-group">
            <label className="form-label">
              Manused {selectedLeaveType?.requiresAttachment && '*'}
            </label>
            <div className="file-upload-area">
              <input
                type="file"
                id="fileInput"
                multiple
                accept=".pdf,.jpg,.jpeg,.png,.gif,.doc,.docx"
                onChange={handleFileSelect}
                className="file-input"
                disabled={loading}
              />
              <label htmlFor="fileInput" className="file-upload-button">
                Lisa failid
              </label>
              <span className="file-hint">PDF, pildid, Word (max 10MB)</span>
            </div>
            {errors.files && <span className="error-text">{errors.files}</span>}
            
            {selectedFiles.length > 0 && (
              <div className="selected-files">
                {selectedFiles.map((file, index) => (
                  <div key={index} className="file-item">
                    <span className="file-name">{file.name}</span>
                    <span className="file-size">({(file.size / 1024).toFixed(1)} KB)</span>
                    <button
                      type="button"
                      onClick={() => removeFile(index)}
                      className="file-remove"
                      disabled={loading}
                    >
                      ×
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        <div className="form-actions">
          {editRequest && (
            <button
              type="button"
              onClick={handleCancelEdit}
              className="btn-secondary"
              disabled={loading}
            >
              Tühista
            </button>
          )}
          <button
            type="submit"
            className="btn-primary"
            disabled={loading || Object.keys(errors).length > 0}
          >
            {loading ? 'Salvestamine...' : editRequest ? 'Uuenda taotlust' : 'Taotle puhkust'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default VacationRequestForm;
