import React, { useState, useEffect, useRef } from 'react';
import { usersApi } from '../../api/api';
import './EmployeeComboBox.css';

/**
 * Searchable combobox for selecting a substitute employee.
 * Stores the selected employee's full name as the value (string).
 * Falls back to free-text for names not in the system.
 */
const EmployeeComboBox = ({ value = '', onChange, disabled = false, excludeUserId }) => {
  const [employees, setEmployees] = useState([]);
  const [query, setQuery]         = useState(value);
  const [open, setOpen]           = useState(false);
  const [focused, setFocused]     = useState(false);
  const ref = useRef(null);

  // Load employees once
  useEffect(() => {
    usersApi.getAll().then(res => {
      setEmployees(res.data.filter(u => u.id !== excludeUserId));
    }).catch(() => {});
  }, [excludeUserId]);

  // Sync input with external value
  useEffect(() => { setQuery(value); }, [value]);

  // Close on outside click
  useEffect(() => {
    if (!open) return;
    const fn = (e) => {
      if (ref.current && !ref.current.contains(e.target)) {
        setOpen(false);
        // Commit typed text as value
        onChange(query);
      }
    };
    document.addEventListener('mousedown', fn);
    return () => document.removeEventListener('mousedown', fn);
  }, [open, query, onChange]);

  const filtered = employees.filter(emp =>
    emp.fullName.toLowerCase().includes(query.toLowerCase()) ||
    emp.department?.toLowerCase().includes(query.toLowerCase())
  ).slice(0, 8);

  const handleInput = (e) => {
    const v = e.target.value;
    setQuery(v);
    onChange(v);
    setOpen(true);
  };

  const handleSelect = (emp) => {
    setQuery(emp.fullName);
    onChange(emp.fullName);
    setOpen(false);
  };

  const handleNoSubstitute = () => {
    setQuery('Pole asendajat');
    onChange('Pole asendajat');
    setOpen(false);
  };

  const handleClear = () => {
    setQuery('');
    onChange('');
  };

  const showDropdown = open && (filtered.length > 0 || query === '');

  return (
    <div ref={ref} className={`ecb-wrapper ${focused ? 'ecb-focused' : ''}`}>
      <div className="ecb-input-row">
        <svg className="ecb-person-icon" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/>
          <circle cx="12" cy="7" r="4"/>
        </svg>
        <input
          type="text"
          className="ecb-input"
          value={query}
          onChange={handleInput}
          onFocus={() => { setFocused(true); setOpen(true); }}
          onBlur={() => setFocused(false)}
          onKeyDown={e => {
            if (e.key === 'Escape') { setOpen(false); }
            if (e.key === 'ArrowDown' && filtered.length > 0) {
              e.preventDefault();
              ref.current?.querySelector('.ecb-option')?.focus();
            }
          }}
          placeholder="Otsi töötajat või sisesta nimi..."
          disabled={disabled}
          autoComplete="off"
        />
        {query && (
          <button type="button" className="ecb-clear" onClick={handleClear} tabIndex={-1}>
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
              <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
        )}
        <svg className="ecb-chevron" width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" style={{ transform: open ? 'rotate(180deg)' : '' }}>
          <path d="M6 9l6 6 6-6"/>
        </svg>
      </div>

      {showDropdown && (
        <div className="ecb-dropdown">
          <div className="ecb-option ecb-no-sub" onClick={handleNoSubstitute}>
            <span className="ecb-option-name">Pole asendajat</span>
          </div>
          {filtered.length > 0 && <div className="ecb-separator" />}
          {filtered.map(emp => (
            <div
              key={emp.id}
              className={`ecb-option ${query === emp.fullName ? 'ecb-active' : ''}`}
              onClick={() => handleSelect(emp)}
              tabIndex={0}
              onKeyDown={e => { if (e.key === 'Enter') handleSelect(emp); }}
            >
              <div className="ecb-avatar">{emp.firstName[0]}{emp.lastName[0]}</div>
              <div className="ecb-option-info">
                <span className="ecb-option-name">{emp.fullName}</span>
                <span className="ecb-option-dept">{emp.department}{emp.position ? ` — ${emp.position}` : ''}</span>
              </div>
            </div>
          ))}
          {query.length > 0 && !employees.some(e => e.fullName.toLowerCase() === query.toLowerCase()) && (
            <>
              <div className="ecb-separator" />
              <div className="ecb-freetext-hint">
                Sisesta vabal kujul: "<strong>{query}</strong>"
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default EmployeeComboBox;
