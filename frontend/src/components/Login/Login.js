import React, { useState } from 'react';
import axios from 'axios';
import { session } from '../../utils/sessionUtils';
import './Login.css';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5000/api';

const Login = ({ onLoginSuccess, onRegisterClick }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    session.clear(); // Clear any stale session data

    try {
      const response = await axios.post(`${API_BASE_URL}/Auth/login`, { email, password });
      const { token, userId, fullName, isAdmin, department, isTemporaryPassword, isProfileComplete } = response.data;

      session.save({ token, userId, fullName, isAdmin, department, isTemporaryPassword, isProfileComplete });

      if (onLoginSuccess) onLoginSuccess(response.data);
    } catch (err) {
      setError(err.response?.data?.message || 'Vale email või parool.');
    } finally {
      setLoading(false);
    }
  };

  const handleDemoLogin = (demoEmail, demoPassword) => {
    setEmail(demoEmail);
    setPassword(demoPassword);
    setTimeout(() => {
      document.getElementById('login-form').dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    }, 100);
  };

  return (
    <div className="login-page">
      <div className="login-container">
        <div className="login-header">
          <h1>Puhkusetaotlused</h1>
          <p>Logi sisse, et jätkata</p>
        </div>

        <form id="login-form" onSubmit={handleSubmit} className="login-form">
          {error && <div className="login-error">{error}</div>}

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              autoComplete="email"
              placeholder="sinu.nimi@example.com"
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Parool</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoComplete="current-password"
              placeholder="••••••••"
              disabled={loading}
            />
          </div>

          <button type="submit" className="login-button" disabled={loading}>
            {loading ? 'Sisselogimine...' : 'Logi sisse'}
          </button>
        </form>

        {onRegisterClick && (
          <div className="login-register-link">
            <span>Pole kontot?</span>
            <button className="login-register-btn" onClick={onRegisterClick}>
              Registreeru
            </button>
          </div>
        )}

        <div className="demo-accounts">
          <div className="demo-title">Demo kontod (testimiseks):</div>
          <div className="demo-buttons">
            <button 
              className="demo-button"
              onClick={() => handleDemoLogin('juri.juurikas@example.com', 'Password123')}
              disabled={loading}
            >
              Admin (Jüri)
            </button>
            <button 
              className="demo-button"
              onClick={() => handleDemoLogin('mari.maasikas@example.com', 'Password123')}
              disabled={loading}
            >
              Töötaja (Mari)
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;
