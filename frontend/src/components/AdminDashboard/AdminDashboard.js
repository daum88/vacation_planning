import React, { useState, useEffect } from 'react';
import { vacationRequestsApi } from '../../api/api';
import { formatDate } from '../../utils/dateUtils';
import './AdminDashboard.css';

const AdminDashboard = () => {
  const [allRequests, setAllRequests] = useState([]);
  const [pendingRequests, setPendingRequests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [view, setView] = useState('pending'); // 'pending' or 'all'
  const [approvingId, setApprovingId] = useState(null);
  const [adminComment, setAdminComment] = useState('');

  useEffect(() => {
    fetchRequests();
  }, [view]);

  const fetchRequests = async () => {
    try {
      setLoading(true);
      setError(null);
      
      if (view === 'pending') {
        const response = await vacationRequestsApi.getPending();
        setPendingRequests(response.data);
      } else {
        const response = await vacationRequestsApi.getAllAdmin();
        setAllRequests(response.data);
      }
    } catch (err) {
      console.error('Error fetching admin requests:', err);
      setError(err.message || 'Viga andmete laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async (id, approved) => {
    try {
      await vacationRequestsApi.approve(id, {
        approved,
        adminComment: adminComment || null
      });
      
      setAdminComment('');
      setApprovingId(null);
      fetchRequests();
    } catch (err) {
      console.error('Error approving request:', err);
      alert(err.response?.data?.message || 'Viga kinnitamisel');
    }
  };

  const handleDelete = async (id) => {
    if (window.confirm('Kas oled kindel, et soovid selle taotluse kustutada?')) {
      try {
        await vacationRequestsApi.deleteAdmin(id);
        fetchRequests();
      } catch (err) {
        console.error('Error deleting request:', err);
        alert(err.response?.data?.message || 'Viga kustutamisel');
      }
    }
  };

  const getStatusBadge = (status) => {
    const statusMap = {
      'Pending': { text: 'Ootel', class: 'pending' },
      'Approved': { text: 'Kinnitatud', class: 'approved' },
      'Rejected': { text: 'Tagasi lükatud', class: 'rejected' }
    };
    
    const statusInfo = statusMap[status] || statusMap['Pending'];
    return <span className={`status-badge ${statusInfo.class}`}>{statusInfo.text}</span>;
  };

  const requests = view === 'pending' ? pendingRequests : allRequests;

  if (loading) {
    return (
      <div className="admin-dashboard">
        <h2>👔 Admin Juhtpaneel</h2>
        <div className="loading">Laadimine...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="admin-dashboard">
        <h2>👔 Admin Juhtpaneel</h2>
        <div className="error-box">{error}</div>
      </div>
    );
  }

  return (
    <div className="admin-dashboard">
      <div className="admin-header">
        <h2>👔 Admin Juhtpaneel</h2>
        <div className="view-switcher">
          <button
            className={`view-btn ${view === 'pending' ? 'active' : ''}`}
            onClick={() => setView('pending')}
          >
            ⏳ Ootel ({pendingRequests.length})
          </button>
          <button
            className={`view-btn ${view === 'all' ? 'active' : ''}`}
            onClick={() => setView('all')}
          >
            📋 Kõik
          </button>
        </div>
      </div>

      {requests.length === 0 ? (
        <div className="empty-state-admin">
          <p>Ühtegi {view === 'pending' ? 'ootel ' : ''}taotlust ei leitud.</p>
        </div>
      ) : (
        <div className="admin-requests-list">
          {requests.map((request) => (
            <div key={request.id} className="admin-request-card">
              <div className="request-top">
                <div className="request-info">
                  <div className="request-user">
                    Töötaja ID: <strong>{request.userId}</strong>
                  </div>
                  <div className="request-dates-inline">
                    {formatDate(request.startDate)} → {formatDate(request.endDate)}
                    <span className="days-text">({request.daysCount} päeva)</span>
                  </div>
                </div>
                {getStatusBadge(request.status)}
              </div>

              {request.comment && (
                <div className="request-comment-box">
                  <strong>Töötaja kommentaar:</strong>
                  <p>{request.comment}</p>
                </div>
              )}

              {request.adminComment && (
                <div className="admin-comment-box">
                  <strong>Admin kommentaar:</strong>
                  <p>{request.adminComment}</p>
                  <small>Kinnitaja: Admin {request.approvedByUserId} • {formatDate(request.approvedAt)}</small>
                </div>
              )}

              {request.status === 'Pending' && (
                <div className="approval-section">
                  {approvingId === request.id ? (
                    <div className="approval-form">
                      <textarea
                        value={adminComment}
                        onChange={(e) => setAdminComment(e.target.value)}
                        placeholder="Valikuline kommentaar..."
                        rows="2"
                        maxLength="500"
                      />
                      <div className="approval-actions">
                        <button
                          className="btn btn-approve"
                          onClick={() => handleApprove(request.id, true)}
                        >
                          ✓ Kinnita
                        </button>
                        <button
                          className="btn btn-reject"
                          onClick={() => handleApprove(request.id, false)}
                        >
                          ✗ Lükka tagasi
                        </button>
                        <button
                          className="btn btn-cancel"
                          onClick={() => {
                            setApprovingId(null);
                            setAdminComment('');
                          }}
                        >
                          Tühista
                        </button>
                      </div>
                    </div>
                  ) : (
                    <button
                      className="btn btn-review"
                      onClick={() => setApprovingId(request.id)}
                    >
                      📝 Vaata üle
                    </button>
                  )}
                </div>
              )}

              <div className="admin-actions">
                <button
                  className="btn-delete-admin"
                  onClick={() => handleDelete(request.id)}
                >
                  🗑️ Kustuta
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default AdminDashboard;
