import React, { useState, useEffect, useRef } from 'react';
import { vacationRequestsApi } from '../../api/api';
import { useToast } from '../Toast/Toast';
import './CommentThread.css';

const CommentThread = ({ requestId, isAdmin }) => {
  const [comments, setComments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [text, setText] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const bottomRef = useRef(null);
  const toast = useToast();

  useEffect(() => {
    load();
  }, [requestId]); // eslint-disable-line

  useEffect(() => {
    if (comments.length > 0) {
      bottomRef.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }
  }, [comments]);

  const load = async () => {
    setLoading(true);
    try {
      const res = await vacationRequestsApi.getComments(requestId);
      setComments(res.data);
    } catch (err) {
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!text.trim()) return;
    setSubmitting(true);
    try {
      const res = await vacationRequestsApi.postComment(requestId, text.trim());
      setComments(prev => [...prev, res.data]);
      setText('');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Viga kommentaari saatmisel');
    } finally {
      setSubmitting(false);
    }
  };

  const formatTime = (isoStr) => {
    const d = new Date(isoStr);
    const today = new Date();
    const isToday = d.toDateString() === today.toDateString();
    if (isToday) return d.toLocaleTimeString('et-EE', { hour: '2-digit', minute: '2-digit' });
    return d.toLocaleDateString('et-EE', { day: '2-digit', month: 'short' }) +
      ' ' + d.toLocaleTimeString('et-EE', { hour: '2-digit', minute: '2-digit' });
  };

  return (
    <div className="comment-thread">
      <div className="comment-thread-title">
        Kirjavahetus
        {comments.length > 0 && <span className="comment-count">{comments.length}</span>}
      </div>

      {loading ? (
        <div className="comment-loading">Laadimine...</div>
      ) : comments.length === 0 ? (
        <div className="comment-empty">Kommentaare pole. Küsi täpsustust või lisa info.</div>
      ) : (
        <div className="comment-list">
          {comments.map(c => (
            <div
              key={c.id}
              className={`comment-bubble ${c.isAdmin ? 'admin' : 'employee'}`}
            >
              <div className="comment-meta">
                <span className="comment-author">
                  {c.authorName}
                  {c.isAdmin && <span className="comment-role-tag">admin</span>}
                </span>
                <span className="comment-time">{formatTime(c.createdAt)}</span>
              </div>
              <div className="comment-text">{c.text}</div>
            </div>
          ))}
          <div ref={bottomRef} />
        </div>
      )}

      <form className="comment-form" onSubmit={handleSubmit}>
        <textarea
          className="comment-input"
          value={text}
          onChange={e => setText(e.target.value)}
          placeholder={isAdmin ? 'Kirjuta töötajale...' : 'Küsi täpsustust juhilt...'}
          rows={2}
          maxLength={1000}
          disabled={submitting}
          onKeyDown={e => {
            if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSubmit(e); }
          }}
        />
        <div className="comment-form-footer">
          <span className="comment-char">{text.length}/1000</span>
          <button
            type="submit"
            className="comment-submit"
            disabled={submitting || !text.trim()}
          >
            {submitting ? '...' : 'Saada'}
          </button>
        </div>
      </form>
    </div>
  );
};

export default CommentThread;
