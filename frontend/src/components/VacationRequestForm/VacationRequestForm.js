import React, { useState, useEffect } from 'react';
import { vacationRequestsApi } from '../../api/api';
import { formatDateForInput, calculateDays } from '../../utils/dateUtils';
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
  }, [formData.startDate, formData.endDate]);

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

    try {
      const requestData = {
        startDate: new Date(formData.startDate).toISOString(),
        endDate: new Date(formData.endDate).toISOString(),
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
      setError(
        err.response?.data?.message ||
          'Viga taotluse salvestamisel. Palun kontrollige sisestatud andmeid.'
      );
    } finally {
      setLoading(false);
    }
  };

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
            required
          />
        </div>

        <div className="form-group">
          <label htmlFor="endDate">Lõppkuupäev *</label>
          <input
            type="date"
            id="endDate"
            name="endDate"
            value={formData.endDate}
            onChange={handleChange}
            required
          />
        </div>

        {daysCount > 0 && (
          <div className="days-count">
            <strong>Päevade arv:</strong> {daysCount} päeva
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
          <button type="submit" disabled={loading} className="btn btn-primary">
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
