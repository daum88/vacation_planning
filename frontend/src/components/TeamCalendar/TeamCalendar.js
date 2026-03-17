import React, { useState, useEffect, useMemo } from 'react';
import { calendarApi } from '../../api/api';
import CustomSelect from '../CustomSelect/CustomSelect';
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

    for (let i = 0; i < (startDayOfWeek === 0 ? 6 : startDayOfWeek - 1); i++) {
      days.push(null);
    }

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

  const maxAbsence = useMemo(() => {
    if (!calendarData?.dailyAbsenceCount) return 0;
    return Math.max(0, ...Object.values(calendarData.dailyAbsenceCount));
  }, [calendarData]);

  const upcomingEvents = useMemo(() => {
    if (!calendarData?.events) return [];
    const now = new Date();
    const unique = new Map();

    calendarData.events.forEach(event => {
      const key = `${event.userName}-${event.start}-${event.end}-${event.leaveType}`;
      if (!unique.has(key)) unique.set(key, event);
    });

    return Array.from(unique.values())
      .filter(event => new Date(event.end) >= now)
      .sort((a, b) => new Date(a.start) - new Date(b.start))
      .slice(0, 8);
  }, [calendarData]);

  if (loading) {
    return (
      <div className="team-calendar">
        <div className="loading">Laadimine...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="team-calendar">
        <div className="error-box">{error}</div>
      </div>
    );
  }

  const days = getDaysInMonth();

  return (
    <div className="team-calendar">
      <header className="calendar-hero">
        <div>
          <h2>Meeskonna kalender</h2>
          <p>Näed kiiresti, millal tiimis on puhkuseid ja kus tekib kattuvus.</p>
        </div>

        <div className="calendar-controls">
          <CustomSelect
            className="dept-custom-select"
            options={[
              { value: '', label: 'Kõik osakonnad' },
              ...departments.map(d => ({ value: d, label: d })),
            ]}
            value={selectedDepartment}
            onChange={setSelectedDepartment}
            placeholder="Kõik osakonnad"
          />

          <div className="month-nav">
            <button onClick={() => changeMonth(-1)} className="btn-nav" aria-label="Eelmine kuu">‹</button>
            <h3>{monthNames[selectedMonth.getMonth()]} {selectedMonth.getFullYear()}</h3>
            <button onClick={() => changeMonth(1)} className="btn-nav" aria-label="Järgmine kuu">›</button>
          </div>
        </div>
      </header>

      <section className="calendar-stats">
        <article className="stat-pill">
          <span>Puhkusi selles kuus</span>
          <strong>{calendarData?.events?.length || 0}</strong>
        </article>
        <article className="stat-pill">
          <span>Max puudujate arv päevas</span>
          <strong>{maxAbsence}</strong>
        </article>
        <article className="stat-pill">
          <span>Aktiivne filter</span>
          <strong>{selectedDepartment || 'Kõik osakonnad'}</strong>
        </article>
      </section>

      <section className="calendar-layout">
        <div className="calendar-board">
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
                  <div className="day-top">
                    <div className="day-number">{date.getDate()}</div>
                    {absenceCount > 0 && <div className="absence-badge">{absenceCount}</div>}
                  </div>

                  {events.length > 0 && (
                    <div className="day-events">
                      {events.slice(0, 2).map((event, idx) => (
                        <div
                          key={idx}
                          className="event-indicator"
                          style={{ backgroundColor: event.color }}
                          title={`${event.userName} - ${event.leaveType}`}
                        >
                          <span className="event-avatar">{event.userName.charAt(0)}</span>
                          <span className="event-name">{event.userName.split(' ')[0]}</span>
                        </div>
                      ))}
                      {events.length > 2 && <div className="more-events">+{events.length - 2}</div>}
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </div>

        <aside className="calendar-sidepanel">
          <div className="sidepanel-card">
            <h4>Järgmised puhkused</h4>
            {upcomingEvents.length > 0 ? (
              <div className="upcoming-list">
                {upcomingEvents.map((event, idx) => (
                  <div key={`${event.userName}-${idx}`} className="upcoming-item">
                    <span className="upcoming-color" style={{ backgroundColor: event.color }} />
                    <div className="upcoming-main">
                      <strong>{event.userName}</strong>
                      <span>{event.leaveType}</span>
                    </div>
                    <div className="upcoming-date">
                      {new Date(event.start).toLocaleDateString('et-EE', { day: '2-digit', month: '2-digit' })}
                      {' – '}
                      {new Date(event.end).toLocaleDateString('et-EE', { day: '2-digit', month: '2-digit' })}
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="sidepanel-empty">Selles perioodis puuduvad kinnitatud puhkused.</div>
            )}
          </div>

          <div className="sidepanel-card soft">
            <h4>Kasutus</h4>
            <p>Päevad, kus puudub rohkem inimesi, on kalendris tumedama tooniga märgitud.</p>
          </div>
        </aside>
      </section>
    </div>
  );
};

export default TeamCalendar;
