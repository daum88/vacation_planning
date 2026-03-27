import React, { useState, useEffect } from 'react';
import App from './App';
import Login from './components/Login/Login';
import Registration from './components/Registration/Registration';
import CompleteProfile from './components/CompleteProfile/CompleteProfile';
import { session } from './utils/sessionUtils';

function AppWithAuth() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [needsProfileComplete, setNeedsProfileComplete] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [authKey, setAuthKey] = useState(0);
  const [view, setView] = useState('login'); // 'login' | 'register'

  useEffect(() => {
    const authed = session.isAuthenticated();
    setIsAuthenticated(authed);
    if (authed) {
      setNeedsProfileComplete(session.isTempPassword() || !session.isProfileComplete());
    }
    setIsLoading(false);
  }, []);

  const handleLoginSuccess = (data) => {
    setIsAuthenticated(true);
    // Check if user needs to complete their profile (invited via temp password)
    if (data?.isTemporaryPassword || data?.isProfileComplete === false) {
      setNeedsProfileComplete(true);
    } else {
      setNeedsProfileComplete(false);
      setAuthKey(prev => prev + 1);
    }
  };

  const handleProfileComplete = () => {
    setNeedsProfileComplete(false);
    setAuthKey(prev => prev + 1);
  };

  const handleLogout = () => {
    session.clear();
    setIsAuthenticated(false);
    setNeedsProfileComplete(false);
    setView('login');
    setAuthKey(prev => prev + 1);
  };

  if (isLoading) {
    return (
      <div style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '100vh',
        fontFamily: 'var(--font-body)',
        color: 'var(--color-text-secondary)'
      }}>
        Laadimine...
      </div>
    );
  }

  if (!isAuthenticated) {
    if (view === 'register') {
      return (
        <Registration
          onLoginClick={() => setView('login')}
        />
      );
    }
    return (
      <Login
        onLoginSuccess={handleLoginSuccess}
        onRegisterClick={() => setView('register')}
      />
    );
  }

  // Authenticated but needs to complete profile (invited via temp password)
  if (needsProfileComplete) {
    return <CompleteProfile onComplete={handleProfileComplete} />;
  }

  return <App key={authKey} onLogout={handleLogout} />;
}

export default AppWithAuth;
