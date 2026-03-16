import React from 'react';
import { formatDate } from '../../utils/dateUtils';
import './VacationRequestList.css';

const VacationRequestList = ({ requests, onEdit, onDelete, loading }) => {
  if (loading) {
    return (
      <div className="list-container">
        <h2>Minu puhkusetaotlused</h2>
        <div className="loading">Laadimine...</div>
      </div>
    );
  }

  if (requests.length === 0) {
    return (
      <div className="list-container">
        <h2>Minu puhkusetaotlused</h2>
        <div className="empty-state">
          <p>Ühtegi puhkusetaotlust pole veel esitatud.</p>
          <p>Loo oma esimene taotlus ülaltoodud vormis!</p>
        </div>
      </div>
    );
  }

  return (
    <div className="list-container">
      <h2>Minu puhkusetaotlused</h2>
      <div className="requests-grid">
        {requests.map((request) => (
          <div key={request.id} className={`request-card status-${request.status?.toLowerCase() || 'pending'}`}>
            <div className="request-header">
              <div className="request-dates">
                <div className="date-item">
                  <span className="date-label">Algus</span>
                  <span className="date-value">{formatDate(request.startDate)}</span>
                </div>
                <div className="date-separator">→</div>
                <div className="date-item">
                  <span className="date-label">Lõpp</span>
                  <span className="date-value">{formatDate(request.endDate)}</span>
                </div>
              </div>
              <div className="card-badges">
                <div className="days-badge">
                  {request.daysCount} {request.daysCount === 1 ? 'päev' : 'päeva'}
                </div>
                {request.status && (
                  <div className={`status-badge-small ${request.status.toLowerCase()}`}>
                    {request.status === 'Pending' && '⏳ Ootel'}
                    {request.status === 'Approved' && '✓ Kinnitatud'}
                    {request.status === 'Rejected' && '✗ Tagasi lükatud'}
                  </div>
                )}
              </div>
            </div>

            {request.comment && (
              <div className="request-comment">
                <strong>Kommentaar:</strong>
                <p>{request.comment}</p>
              </div>
            )}

            {request.adminComment && (
              <div className="admin-response">
                <strong>Admin vastus:</strong>
                <p>{request.adminComment}</p>
                {request.approvedAt && (
                  <small>Vastatud: {formatDate(request.approvedAt)}</small>
                )}
              </div>
            )}

            <div className="request-meta">
              <small>Loodud: {formatDate(request.createdAt)}</small>
              {request.updatedAt !== request.createdAt && (
                <small>Uuendatud: {formatDate(request.updatedAt)}</small>
              )}
            </div>

            <div className="request-actions">
              {request.status === 'Pending' && (
                <button
                  onClick={() => onEdit(request)}
                  className="btn btn-edit"
                >
                  Muuda
                </button>
              )}
              <button
                onClick={() => onDelete(request.id)}
                className="btn btn-delete"
              >
                Kustuta
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default VacationRequestList;
