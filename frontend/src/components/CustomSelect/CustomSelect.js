import React, { useState, useRef, useEffect } from 'react';
import './CustomSelect.css';

/**
 * A styled replacement for native <select>.
 * options: Array of { value, label, color?, hint? } or plain strings.
 * Pass value="" and label="Kõik ..." for an "all" option.
 */
const CustomSelect = ({
  options = [],
  value,
  onChange,
  placeholder = 'Vali...',
  className = '',
  disabled = false,
  error = false,
}) => {
  const [open, setOpen] = useState(false);
  const ref = useRef(null);

  // Normalise option shape
  const normalised = options.map(o =>
    typeof o === 'string' || typeof o === 'number'
      ? { value: String(o), label: String(o) }
      : { ...o, value: String(o.value) }
  );

  const selected = normalised.find(o => o.value === String(value ?? ''));

  useEffect(() => {
    if (!open) return;
    const handler = (e) => {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  const handleKey = (e) => {
    if (e.key === 'Escape') setOpen(false);
    if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); setOpen(o => !o); }
    if (e.key === 'ArrowDown' && open) {
      const idx = normalised.findIndex(o => o.value === String(value ?? ''));
      const next = normalised[Math.min(idx + 1, normalised.length - 1)];
      if (next) onChange(next.value);
    }
    if (e.key === 'ArrowUp' && open) {
      const idx = normalised.findIndex(o => o.value === String(value ?? ''));
      const prev = normalised[Math.max(idx - 1, 0)];
      if (prev) onChange(prev.value);
    }
  };

  return (
    <div
      ref={ref}
      className={[
        'custom-select',
        open ? 'cs-open' : '',
        disabled ? 'cs-disabled' : '',
        error ? 'cs-error' : '',
        className,
      ].filter(Boolean).join(' ')}
      tabIndex={disabled ? -1 : 0}
      onKeyDown={handleKey}
    >
      <div
        className="cs-trigger"
        onClick={() => !disabled && setOpen(o => !o)}
        aria-haspopup="listbox"
        aria-expanded={open}
      >
        <div className="cs-trigger-content">
          {selected?.color && (
            <span className="cs-color-dot" style={{ background: selected.color }} />
          )}
          <span className={selected ? 'cs-value' : 'cs-placeholder'}>
            {selected ? selected.label : placeholder}
          </span>
        </div>
        <svg
          className="cs-chevron"
          width="12" height="12"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.5"
        >
          <path d="M6 9l6 6 6-6" />
        </svg>
      </div>

      {open && (
        <div className="cs-dropdown" role="listbox">
          {normalised.map((opt) => (
            <div
              key={opt.value}
              className={`cs-option ${opt.value === String(value ?? '') ? 'cs-option-active' : ''}`}
              role="option"
              aria-selected={opt.value === String(value ?? '')}
              onClick={() => {
                onChange(opt.value);
                setOpen(false);
              }}
            >
              {opt.color && (
                <span className="cs-color-dot" style={{ background: opt.color }} />
              )}
              <span className="cs-option-label">{opt.label}</span>
              {opt.hint && <span className="cs-option-hint">{opt.hint}</span>}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default CustomSelect;
