import React, { useState, useEffect } from 'react';
import { vacationRequestsApi } from '../../api/api';
import { formatDateForInput, calculateDays, getTodayString, isDateInPast } from '../../utils/dateUtils';
import './VacationRequestForm.css';

const VacationRequestForm = ({ onSuccess, editRequest, onCancel }) => {
  const [formData, setFormData] = useState({
    startDate: '',
    endDate: '',
    comment: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [daysCount, setDaysCount] = useState(0);
  const [validationErrors, setValidationErrors] = useState({});

  useEffect(() => {
    if (editRequest) {
      setFormData({
        startDate: formatDateForInput(editRequest.startDate),
        endDate: formatDateForInput(editRequest.endDate),
        comment: editRequest.comment || '',
      });
    }
  }, [editRequest]);

  useEffect(() => {
    const days = calculateDays(formData.startDate, formData.endDate);
    setDaysCount(days);

    // Real-time validation
    const errors = {};

    if (formData.startDate && isDateInPast(formData.startDate) && !editRequest) {
      errors.startDate = 'Alguskuupäev ei saa olla minevikus';
    }

    if (formData.startDate && formData.endDate) {
      const start = new Date(formData.startDate);
      const end = new Date(formData.endDate);
      
      if (start > end) {
        errors.endDate = 'Lõppkuupäev peab olema pärast või võrdne alguskuupäevaga';
      }

      if (days > 90) {
        errors.endDate = 'Puhkus ei saa olla pikem kui 90 päeva';
      }
    }

    setValidationErrors(errors);
  }, [formData.startDate, formData.endDate, editRequest]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    // Check for validation errors
    if (Object.keys(validationErrors).length > 0) {
      setError('Palun parandage vormi vead enne salvestamist.');
      setLoading(false);
      return;
    }

    try {
      // Send dates with time set to start of day for start date and end of day for end date
      const requestData = {
        startDate: new Date(formData.startDate + 'T00:00:00').toISOString(),
        endDate: new Date(formData.endDate + 'T23:59:59').toISOString(),
        comment: formData.comment,
      };

      if (editRequest) {
        await vacationRequestsApi.update(editRequest.id, requestData);
      } else {
        await vacationRequestsApi.create(requestData);
      }

      setFormData({
        startDate: '',
        endDate: '',
        comment: '',
      });
      onSuccess();
    } catch (err) {
      console.error('Form submission error:', err);
      setError(
        err.response?.data?.message ||
          'Viga taotluse salvestamisel. Palun kontrollige sisestatud andmeid.'
      );
    } finally {
      setLoading(false);
    }
  };

  const hasValidationErrors = Object.keys(validationErrors).length > 0;
  const today = getTodayString();

  return (
    <div className="form-container">
      <h2>{editRequest ? 'Muuda puhkusetaotlust' : 'Uus puhkusetaotlus'}</h2>
      <form onSubmit={handleSubmit} className="vacation-form">
        <div className="form-group">
          <label htmlFor="startDate">Alguskuupäev *</label>
          <input
            type="date"
            id="startDate"
            name="startDate"
            value={formData.startDate}
            onChange={handleChange}
            min={editRequest ? undefined : today}
            required
            className={validationErrors.startDate ? 'error' : ''}
          />
          {validationErrors.startDate && (
            <span className="field-error">{validationErrors.startDate}</span>
          )}
        </div>

        <div className="form-group">
          <label htmlFor="endDate">Lõppkuupäev *</label>
          <input
            type="date"
            id="endDate"
            name="endDate"
            value={formData.endDate}
            onChange={handleChange}
            min={formData.startDate || (editRequest ? undefined : today)}
            required
            className={validationErrors.endDate ? 'error' : ''}
          />
          {validationErrors.endDate && (
            <span className="field-error">{validationErrors.endDate}</span>
          )}
        </div>

        {daysCount > 0 && (
          <div className={`days-count ${daysCount > 90 ? 'error' : ''}`}>
            <strong>Päevade arv:</strong> {daysCount} {daysCount === 1 ? 'päev' : 'päeva'}
          </div>
        )}

        <div className="form-group">
          <label htmlFor="comment">Kommentaar</label>
          <textarea
            id="comment"
            name="comment"
            value={formData.comment}
            onChange={handleChange}
            rows="4"
            maxLength="500"
            placeholder="Sisesta täiendav info..."
          />
          <small>{formData.comment.length}/500 tähemärki</small>
        </div>

        {error && <div className="error-message">{error}</div>}

        <div className="form-actions">
          <button 
            type="submit" 
            disabled={loading || hasValidationErrors || !formData.startDate || !formData.endDate} 
            className="btn btn-primary"
          >
            {loading ? 'Salvestamine...' : editRequest ? 'Uuenda' : 'Loo taotlus'}
          </button>
          {editRequest && (
            <button type="button" onClick={onCancel} className="btn btn-secondary">
              Tühista
            </button>
          )}
        </div>
      </form>
    </div>
  );
};

export default VacationRequestForm;
