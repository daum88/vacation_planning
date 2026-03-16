export const formatDate = (dateString) => {
  const date = new Date(dateString);
  return date.toLocaleDateString('et-EE', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  });
};

export const formatDateForInput = (dateString) => {
  const date = new Date(dateString);
  return date.toISOString().split('T')[0];
};

export const calculateDays = (startDate, endDate) => {
  if (!startDate || !endDate) return 0;
  
  const start = new Date(startDate);
  const end = new Date(endDate);
  const diffTime = end - start;
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  
  return diffDays > 0 ? diffDays : 0;
};
