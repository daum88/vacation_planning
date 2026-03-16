import React, { useMemo, useState } from 'react';
import { formatDate } from '../../utils/dateUtils';
import { vacationRequestsApi } from '../../api/api';
import { useToast } from '../Toast/Toast';
import './VacationRequestList.css';

const VacationRequestList = ({ requests, onEdit, onDelete, onWithdraw, loading }) => {
  const [expandedRequest, setExpandedRequest] = useState(null);
  const [auditLogs, setAuditLogs] = useState({});
  const [loadingAudit, setLoadingAudit] = useState({});
  const [confirmDelete, setConfirmDelete] = useState(null);
  const [confirmWithdraw, setConfirmWithdraw] = useState(null);
  const toast = useToast();

  const summary = useMemo(() => {
    const pending = requests.filter(r => r.status === 'Pending').length;
    const approved = requests.filter(r => r.status === 'Approved').length;
    const rejected = requests.filter(r => r.status === 'Rejected').length;
    return { pending, approved, rejected };
  }, [requests]);

  const toggleExpand = (requestId) => {
    if (expandedRequest === requestId) {
      setExpandedRequest(null);
    } else {
      setExpandedRequest(requestId);
      if (!auditLogs[requestId]) {
        fetchAuditLogs(requestId);
      }
    }
  };

  const fetchAuditLogs = async (requestId) => {
    try {
      setLoadingAudit(prev => ({ ...prev, [requestId]: true }));
      const response = await vacationRequestsApi.getAuditLogs(requestId);
      setAuditLogs(prev => ({ ...prev, [requestId]: response.data }));
    } catch (err) {
      console.error('Error fetching audit logs:', err);
      toast.error('Viga auditi logide laadimisel');
    } finally {
      setLoadingAudit(prev => ({ ...prev, [requestId]: false }));
    }
  };

  const handleDownloadAttachment = async (requestId, attachmentId, fileName) => {
    try {
      const response = await vacationRequestsApi.downloadAttachment(requestId, attachmentId);
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      toast.success(`Fail ${fileName} allalaaditud ✓`);
    } catch (err) {
      console.error('Error downloading attachment:', err);
      toast.error('Viga faili allalaadimisel');
    }
  };

  const handleDeleteAttachment = async (requestId, attachmentId) => {
    try {
      await vacationRequestsApi.deleteAttachment(requestId, attachmentId);
      toast.success('Manus kustutatud');
    } catch (err) {
      console.error('Error deleting attachment:', err);
      toast.error('Viga manuse kustutamisel');
    }
  };

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
        <div className="list-header">
          <h2>Minu puhkusetaotlused</h2>
          <p>Siia tekib sinu taotluste ajalugu koos staatustega.</p>
        </div>
        <div className="empty-state">
          <h3>Siin on hetkel vaikne</h3>
          <p>Sul ei ole veel ühtegi puhkusetaotlust.</p>
          <p>Loo esimene taotlus ülal oleva vormiga.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="list-container">
      <div className="list-header">
        <h2>Minu puhkusetaotlused ({requests.length})</h2>
        <p>Hoia silm peal, mis on ootel ja mis on kinnitatud.</p>
      </div>

      <div className="list-summary-row">
        <div className="summary-chip pending">{summary.pending} ootel</div>
        <div className="summary-chip approved">{summary.approved} kinnitatud</div>
        <div className="summary-chip rejected">{summary.rejected} tagasi lükatud</div>
      </div>

      <div className="requests-grid">
        {requests.map((request) => (
          <div key={request.id} className={`request-card status-${request.status?.toLowerCase() || 'pending'}`}>
            {/* Header with dates and badges */}
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
                    {request.status === 'Pending' && 'Ootel'}
                    {request.status === 'Approved' && 'Kinnitatud'}
                    {request.status === 'Rejected' && 'Tagasi lükatud'}
                    {request.status === 'Withdrawn' && 'Tagasi võetud'}
                  </div>
                )}
              </div>
            </div>

            {/* Leave Type */}
            {request.leaveTypeName && (
              <div className="leave-type-badge" style={{ backgroundColor: request.leaveTypeColor || '#007AFF' }}>
                {request.leaveTypeName}
              </div>
            )}

            {/* Comment */}
            {request.comment && (
              <div className="request-comment">
                <strong>Kommentaar:</strong>
                <p>{request.comment}</p>
              </div>
            )}

            {/* Admin Response */}
            {request.adminComment && (
              <div className="admin-response">
                <strong>Admin vastus:</strong>
                <p>{request.adminComment}</p>
                {request.approvedAt && (
                  <small>
                    {request.approvedByName} • {formatDate(request.approvedAt)}
                  </small>
                )}
              </div>
            )}

            {/* Attachments */}
            {request.attachments && request.attachments.length > 0 && (
              <div className="attachments-section">
                <strong>Manused ({request.attachments.length}):</strong>
                <div className="attachments-list">
                  {request.attachments.map(attachment => (
                    <div key={attachment.id} className="attachment-item">
                      <span className="attachment-icon">Fail</span>
                      <span className="attachment-name">{attachment.fileName}</span>
                      <span className="attachment-size">
                        ({(attachment.fileSize / 1024).toFixed(1)} KB)
                      </span>
                      <div className="attachment-actions">
                        <button
                          onClick={() => handleDownloadAttachment(request.id, attachment.id, attachment.fileName)}
                          className="btn-attachment-action"
                          title="Laadi alla"
                        >
↓
                        </button>
                        {request.canDelete && (
                          <button
                            onClick={() => handleDeleteAttachment(request.id, attachment.id)}
                            className="btn-attachment-delete"
                            title="Kustuta"
                          >
×
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Metadata */}
            <div className="request-meta">
              <small>Loodud: {formatDate(request.createdAt)}</small>
              {request.updatedAt !== request.createdAt && (
                <small>Uuendatud: {formatDate(request.updatedAt)}</small>
              )}
            </div>

            {/* Actions */}
            <div className="request-actions">
              {request.canEdit && (
                <button onClick={() => onEdit(request)} className="btn btn-edit">
                  Muuda
                </button>
              )}
              {request.canWithdraw && (
                confirmWithdraw === request.id ? (
                  <>
                    <span className="confirm-inline-label">Võta tagasi?</span>
                    <button onClick={() => { onWithdraw(request.id); setConfirmWithdraw(null); }} className="btn btn-withdraw">Jah</button>
                    <button onClick={() => setConfirmWithdraw(null)} className="btn btn-cancel-inline">Ei</button>
                  </>
                ) : (
                  <button onClick={() => setConfirmWithdraw(request.id)} className="btn btn-withdraw">
                    Võta tagasi
                  </button>
                )
              )}
              {request.canDelete && (
                confirmDelete === request.id ? (
                  <>
                    <span className="confirm-inline-label">Kustuta?</span>
                    <button onClick={() => { onDelete(request.id); setConfirmDelete(null); }} className="btn btn-delete">Jah</button>
                    <button onClick={() => setConfirmDelete(null)} className="btn btn-cancel-inline">Ei</button>
                  </>
                ) : (
                  <button onClick={() => setConfirmDelete(request.id)} className="btn btn-delete">
                    Kustuta
                  </button>
                )
              )}
              <button onClick={() => toggleExpand(request.id)} className="btn btn-expand">
                {expandedRequest === request.id ? 'Vähenda' : 'Rohkem'}
              </button>
            </div>

            {/* Expanded Section - Audit Trail */}
            {expandedRequest === request.id && (
              <div className="expanded-section">
                <h4>Auditi logi</h4>
                {loadingAudit[request.id] ? (
                  <div className="loading-audit">Laadimine...</div>
                ) : auditLogs[request.id] && auditLogs[request.id].length > 0 ? (
                  <div className="audit-trail">
                    {auditLogs[request.id].map(log => (
                      <div key={log.id} className="audit-entry">
                        <div className="audit-header">
                          <strong>{log.userName}</strong>
                          <span className="audit-action">{log.action}</span>
                        </div>
                        {log.details && <div className="audit-details">{log.details}</div>}
                        <div className="audit-timestamp">
                          {formatDate(log.timestamp)}
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="no-audit">Auditi logid puuduvad</div>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

export default VacationRequestList;
