import React, { useState, useEffect } from 'react';
import { calendarApi } from '../../api/api';
import './TeamCalendar.css';

const TeamCalendar = () => {
  const [calendarData, setCalendarData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedMonth, setSelectedMonth] = useState(new Date());
  const [selectedDepartment, setSelectedDepartment] = useState('');
  const [departments, setDepartments] = useState([]);

  useEffect(() => {
    fetchDepartments();
  }, []);

  useEffect(() => {
    fetchCalendarData();
  }, [selectedMonth, selectedDepartment]);

  const fetchDepartments = async () => {
    try {
      const response = await calendarApi.getDepartments();
      setDepartments(response.data);
    } catch (err) {
      console.error('Error fetching departments:', err);
    }
  };

  const fetchCalendarData = async () => {
    try {
      setLoading(true);
      setError(null);

      const startDate = new Date(selectedMonth.getFullYear(), selectedMonth.getMonth(), 1);
      const endDate = new Date(selectedMonth.getFullYear(), selectedMonth.getMonth() + 1, 0);

      const response = await calendarApi.getTeamCalendar(
        startDate.toISOString().split('T')[0],
        endDate.toISOString().split('T')[0],
        selectedDepartment || undefined
      );

      setCalendarData(response.data);
    } catch (err) {
      console.error('Error fetching calendar:', err);
      setError(err.message || 'Viga kalendri laadimisel');
    } finally {
      setLoading(false);
    }
  };

  const changeMonth = (offset) => {
    setSelectedMonth(new Date(selectedMonth.getFullYear(), selectedMonth.getMonth() + offset, 1));
  };

  const getDaysInMonth = () => {
    const year = selectedMonth.getFullYear();
    const month = selectedMonth.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const daysInMonth = lastDay.getDate();
    const startDayOfWeek = firstDay.getDay();

    const days = [];
    
    // Add empty cells for days before the first of the month
    for (let i = 0; i < (startDayOfWeek === 0 ? 6 : startDayOfWeek - 1); i++) {
      days.push(null);
    }

    // Add all days of the month
    for (let day = 1; day <= daysInMonth; day++) {
      days.push(new Date(year, month, day));
    }

    return days;
  };

  const getEventsForDay = (date) => {
    if (!calendarData || !date) return [];
    
    return calendarData.events.filter(event => {
      const eventStart = new Date(event.start);
      const eventEnd = new Date(event.end);
      return date >= eventStart && date <= eventEnd;
    });
  };

  const getAbsenceCount = (date) => {
    if (!calendarData || !date) return 0;
    const dateKey = date.toISOString().split('T')[0];
    return calendarData.dailyAbsenceCount[dateKey] || 0;
  };

  const monthNames = ['Jaanuar', 'Veebruar', 'Märts', 'Aprill', 'Mai', 'Juuni',
                      'Juuli', 'August', 'September', 'Oktoober', 'November', 'Detsember'];
  const weekDays = ['E', 'T', 'K', 'N', 'R', 'L', 'P'];

  if (loading) {
    return (
      <div className="team-calendar">
        <h2>📅 Meeskonna kalender</h2>
        <div className="loading">Laadimine...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="team-calendar">
        <h2>📅 Meeskonna kalender</h2>
        <div className="error-box">{error}</div>
      </div>
    );
  }

  const days = getDaysInMonth();

  return (
    <div className="team-calendar">
      <div className="calendar-header">
        <h2>📅 Meeskonna kalender</h2>
        
        <div className="calendar-controls">
          <select
            value={selectedDepartment}
            onChange={(e) => setSelectedDepartment(e.target.value)}
            className="department-select"
          >
            <option value="">Kõik osakonnad</option>
            {departments.map(dept => (
              <option key={dept} value={dept}>{dept}</option>
            ))}
          </select>

          <div className="month-nav">
            <button onClick={() => changeMonth(-1)} className="btn-nav">
              ←
            </button>
            <h3>{monthNames[selectedMonth.getMonth()]} {selectedMonth.getFullYear()}</h3>
            <button onClick={() => changeMonth(1)} className="btn-nav">
              →
            </button>
          </div>
        </div>
      </div>

      <div className="calendar-grid-wrapper">
        <div className="calendar-weekdays">
          {weekDays.map(day => (
            <div key={day} className="weekday">{day}</div>
          ))}
        </div>

        <div className="calendar-grid">
          {days.map((date, index) => {
            if (!date) {
              return <div key={`empty-${index}`} className="calendar-day empty" />;
            }

            const events = getEventsForDay(date);
            const absenceCount = getAbsenceCount(date);
            const isToday = date.toDateString() === new Date().toDateString();
            const isWeekend = date.getDay() === 0 || date.getDay() === 6;

            return (
              <div
                key={date.toISOString()}
                className={`calendar-day ${isToday ? 'today' : ''} ${isWeekend ? 'weekend' : ''} ${events.length > 0 ? 'has-events' : ''}`}
              >
                <div className="day-number">{date.getDate()}</div>
                
                {absenceCount > 0 && (
                  <div className="absence-badge">{absenceCount} ära</div>
                )}

                {events.length > 0 && (
                  <div className="day-events">
                    {events.slice(0, 3).map((event, idx) => (
                      <div
                        key={idx}
                        className="event-indicator"
                        style={{ backgroundColor: event.color }}
                        title={`${event.userName} - ${event.leaveType}`}
                      >
                        <span className="event-name">{event.userName.split(' ')[0]}</span>
                      </div>
                    ))}
                    {events.length > 3 && (
                      <div className="more-events">+{events.length - 3} veel</div>
                    )}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {calendarData && calendarData.events.length > 0 && (
        <div className="calendar-legend">
          <h4>Puhkusel ({calendarData.events.length}):</h4>
          <div className="legend-items">
            {calendarData.events.map((event, idx) => (
              <div key={idx} className="legend-item">
                <div
                  className="legend-color"
                  style={{ backgroundColor: event.color }}
                />
                <span className="legend-text">
                  {event.userName} ({new Date(event.start).toLocaleDateString('et-EE')} - {new Date(event.end).toLocaleDateString('et-EE')})
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {calendarData && calendarData.events.length === 0 && (
        <div className="empty-calendar">
          Valitud perioodil ei ole kinnitatud puhkuseid.
        </div>
      )}
    </div>
  );
};

export default TeamCalendar;
