import React from 'react';
import './StatusBadge.css';

export const STATUS_MAP = {
  Pending:   { label: 'Ootel',           cls: 'sb-pending'   },
  Approved:  { label: 'Kinnitatud',      cls: 'sb-approved'  },
  Rejected:  { label: 'Tagasi lükatud', cls: 'sb-rejected'  },
  Withdrawn: { label: 'Tagasi võetud',  cls: 'sb-withdrawn' },
};

const StatusBadge = ({ status }) => {
  const { label, cls } = STATUS_MAP[status] ?? STATUS_MAP.Pending;
  return <span className={`sb-badge ${cls}`}>{label}</span>;
};

export default StatusBadge;
