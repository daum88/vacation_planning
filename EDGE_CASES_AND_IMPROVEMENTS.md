# Edge Cases ja Puuduvad Funktsioonid

## Kriitilised Edge Case-id (TULEB PARANDADA)

### 1. Kuupäevade Valdamine

#### ❌ Mineviku kuupäevad
**Probleem:** Praegu saab luua puhkusetaotluse, mis algas eile või aasta tagasi.
**Lahendus:**
```javascript
// Frontend validation
if (new Date(formData.startDate) < new Date().setHours(0,0,0,0)) {
  setError('Alguskuupäev ei saa olla minevikus');
  return;
}
```

```csharp
// Backend validation
if (dto.StartDate.Date < DateTime.Today)
{
    return BadRequest(new { message = "Alguskuupäev ei saa olla minevikus." });
}
```

#### ❌ StartDate === EndDate
**Probleem:** Praegu on StartDate >= EndDate keelatud, aga ühe päeva puhkus peaks olema lubatud.
**Lahendus:** Muuta validatsiooni `StartDate > EndDate` peale.

#### ❌ Liiga pikad puhkused
**Probleem:** Keegi võib sisestada 365+ päeva puhkuse.
**Lahendus:** Lisa max päevade arv (nt 60 päeva).

```csharp
var daysCount = (dto.EndDate.Date - dto.StartDate.Date).Days;
if (daysCount > 60)
{
    return BadRequest(new { message = "Puhkus ei saa olla pikem kui 60 päeva." });
}
```

### 2. Timezone Probleemid

#### ❌ UTC vs Local Time
**Probleem:** Backend kasutab UTC, frontend võib kasutada local time.
**Lahendus:**
```javascript
// Frontend - saada alati alguse päev 00:00:00 ja lõpu päev 23:59:59
const requestData = {
  startDate: new Date(formData.startDate + 'T00:00:00').toISOString(),
  endDate: new Date(formData.endDate + 'T23:59:59').toISOString(),
  comment: formData.comment,
};
```

### 3. Kattuvate Perioodide Kontroll

#### ❌ Täpselt samad kuupäevad
**Probleem:** Kui üritan luua täpselt sama perioodi uuesti?
**Test:** Praegune kood peaks seda blokeerima, aga tuleks testida.

#### ⚠️ Piiriäärsed juhud
```
Olemasolev: 1.juuli - 10.juuli
Uus taotlus: 10.juuli - 20.juuli (algab täpselt lõpukuupäeval)
```
**Küsimus:** Kas see peaks olema lubatud või mitte? Praegu peaks olema lubatud.

### 4. Andmebaaside Piiranguud

#### ❌ Concurrency probleemid
**Probleem:** Kui kaks kasutajat muudavad sama taotlust samal ajal?
**Lahendus:** Lisa RowVersion/Timestamp field Entity Framework concurrency kontrolli jaoks.

```csharp
public class VacationRequest
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

#### ❌ SQLite lukustused
**Probleem:** SQLite ei toeta hästi concurrent writes.
**Lahendus:** Tootmises kasuta PostgreSQL või SQL Server.

### 5. Input Validatsioon

#### ❌ SQL Injection
**Praegune staatus:** EF Core kasutab parameterized queries, seega kaitstud ✅

#### ❌ XSS (Cross-Site Scripting)
**Probleem:** Kui keegi sisestab kommentaari:
```
<script>alert('XSS')</script>
```
**Lahendus:** React escapib automaatselt, aga lisa backend-is sanitization:

```csharp
// Install package: HtmlSanitizer
var sanitizer = new HtmlSanitizer();
vacationRequest.Comment = sanitizer.Sanitize(dto.Comment);
```

#### ❌ Liiga pikk kommentaar
**Praegune staatus:** Frontend limiteerib 500 tähemärki, backend ka ✅
**Aga:** Mis siis kui keegi saadab otse API-sse?
**Test:** Curl päring 1000 tähemärgiga.

### 6. Network ja Error Handling

#### ❌ Backend on maha
**Praegune:** Frontend crashib või näitab generic errori.
**Lahendus:**
```javascript
try {
  const response = await vacationRequestsApi.getAll();
  setRequests(response.data);
} catch (error) {
  if (!error.response) {
    setError('Server ei vasta. Palun kontrollige võrguühendust.');
  } else if (error.response.status === 500) {
    setError('Serveri viga. Palun proovige hiljem uuesti.');
  }
}
```

#### ❌ Aeglane ühendus
**Probleem:** Pole timeoutide.
**Lahendus:**
```javascript
const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000, // 10 sekundit
});
```

#### ❌ Network retry
**Praegune:** Kui päring ebaõnnestub, pole retry loogikat.

### 7. UI/UX Edge Cases

#### ❌ Tühjade väljadega submit
**Praegune staatus:** HTML5 required atribuut hoiab ära ✅
**Aga:** Kui keegi disablib JavaScripti?

#### ❌ Kiire double-click submit nupul
**Probleem:** Võib luua duplikaattaotluse.
**Lahendus:** Lisa loading state ja disable button:
```javascript
<button type="submit" disabled={loading || !formData.startDate || !formData.endDate}>
```
**Praegune staatus:** Osaliselt implementeeritud ✅

#### ❌ Browser back button
**Probleem:** Kui muudan taotlust ja vajutan back, kaob vorm?
**Lahendus:** Lisa "Kas olete kindel?" dialog kui vorm on muudetud.

### 8. Andmete Terviklikkus

#### ❌ Orphaned records
**Probleem:** Kui UserID 1 kustutatakse, aga tema taotlused jäävad?
**Lahendus:** Lisa Foreign Key constraint ja CASCADE delete.

#### ❌ Valed kuupäeva formaadid
**Probleem:** Erinevad browserid võivad date inputi erinevalt käsitleda.
**Lahendus:** Lisa backend-is range check:
```csharp
if (dto.StartDate.Year < 2020 || dto.StartDate.Year > 2100)
{
    return BadRequest(new { message = "Vigane kuupäev." });
}
```

---

## Puuduvad Funktsioonid (Prioriteediga)

### 🔴 KRIITILISED (Tootmiseks vajalikud)

#### 1. Error Logging
**Praegu:** Vead kaovad.
**Lahendus:**
```csharp
// Install: Serilog
Log.Error(ex, "Error creating vacation request for user {UserId}", userId);
```

#### 2. API Rate Limiting
**Praegu:** Keegi võib spammida API-d.
**Lahendus:** Lisa AspNetCoreRateLimit package.

#### 3. HTTPS Certificate (Production)
**Praegu:** Development self-signed cert.
**Lahendus:** Let's Encrypt või ettevõtte cert.

#### 4. Environment Configuration
**Praegu:** Hardcoded URLs.
**Lahendus:**
```javascript
const API_URL = process.env.REACT_APP_API_URL || 'https://localhost:5001/api';
```

#### 5. Database Migrations
**Praegu:** EnsureCreated() pole tootmiseks sobiv.
**Lahendus:**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 🟡 OLULISED (Kasutajakogemuse parandamiseks)

#### 1. Loading Indicators
**Praegune:** Lihtne "Laadimine..." tekst.
**Täiustus:** Skeleton loaders, progress bars.

#### 2. Success Notifications
**Praegu:** Vorm lihtsalt tühjeneb.
**Täiustus:** Toast notifications:
```javascript
// Install: react-toastify
toast.success('Puhkusetaotlus edukalt loodud!');
```

#### 3. Confirmation Dialogs
**Praegu:** window.confirm() on kole.
**Täiustus:** Custom modal komponendid.

#### 4. Form Validation Messages
**Praegu:** Generic error messages.
**Täiustus:** Field-specific validation errors.

#### 5. Undo Delete
**Praegu:** Kustutatud = igaveseks kadunud.
**Täiustus:** Soft delete + undo võimalus.

#### 6. Sorting ja Filtering
**Praegu:** Ainult chronological järjestus.
**Täiustus:**
- Sortimise nupud (kuupäev, päevade arv)
- Filter tulevaste/möödunud puhkuste vahel
- Otsingufunktsionaalsus

#### 7. Pagination
**Praegu:** Kõik taotlused laaditakse korraga.
**Probleem:** 1000 taotlusega?
**Täiustus:**
```csharp
[HttpGet]
public async Task<ActionResult> GetVacationRequests(
    [FromQuery] int page = 1, 
    [FromQuery] int pageSize = 10)
```

#### 8. Kalendri Vaade
**Praegu:** Ainult list view.
**Täiustus:** Integreeri react-calendar või fullcalendar.

#### 9. Mobile Responsive Täiustused
**Praegune:** Põhiline responsive ✅
**Täiustus:**
- Touch gestures (swipe to delete)
- Mobile-optimized date pickers
- PWA tugi

### 🟢 NICE-TO-HAVE (Tulevikus)

#### 1. Multi-language Support
**Praegu:** Ainult eesti keel.
**Täiustus:** i18n library (react-i18next).

#### 2. Dark Mode
**Täiustus:** CSS variables + toggle switch.

#### 3. Export Functionality
- PDF export
- CSV export
- Excel export

#### 4. Statistics Dashboard
- Kokku puhkusepäevi kasutatud
- Järelejäänud puhkusepäevad
- Graafikud ja diagrammid

#### 5. Email Notifications
- Taotlus loodud
- Taotlus kinnitatud/tagasi lükatud
- Tuletused tulevaste puhkuste kohta

#### 6. Calendar Integration
- Google Calendar export
- Outlook calendar export
- iCal format

#### 7. Attachments
- Võimalus lisada failid (nt arstitõend)

#### 8. Comments/History
- Ajalugu kõikidest muudatustest
- Kommentaaride ahel

---

## Testimise Checklist

### Unit Tests

**Backend:**
```csharp
[Fact]
public async Task CreateVacationRequest_WithPastDate_ReturnsBadRequest()
{
    // Arrange
    var dto = new VacationRequestDto 
    { 
        StartDate = DateTime.Today.AddDays(-1),
        EndDate = DateTime.Today.AddDays(5)
    };
    
    // Act
    var result = await _controller.PostVacationRequest(dto);
    
    // Assert
    Assert.IsType<BadRequestObjectResult>(result);
}
```

**Frontend:**
```javascript
test('shows error when start date is after end date', () => {
  render(<VacationRequestForm />);
  
  const startInput = screen.getByLabelText('Alguskuupäev');
  const endInput = screen.getByLabelText('Lõppkuupäev');
  
  fireEvent.change(startInput, { target: { value: '2026-12-31' } });
  fireEvent.change(endInput, { target: { value: '2026-12-01' } });
  
  expect(screen.getByText(/alguskuupäev peab olema/i)).toBeInTheDocument();
});
```

### Integration Tests

1. ✅ Create request → appears in list
2. ✅ Update request → changes reflected
3. ✅ Delete request → removed from list
4. ❌ Overlapping requests → should be blocked
5. ❌ Past date requests → should be blocked
6. ❌ Invalid date range → should show error
7. ❌ Network failure → should show user-friendly error
8. ❌ Concurrent updates → should handle gracefully

### Performance Tests

1. Load test: 100 concurrent users
2. Database: 10,000 vacation requests
3. API response time < 200ms
4. Frontend load time < 2s

### Security Tests

1. ✅ SQL injection (EF Core handles)
2. ❌ XSS attacks
3. ❌ CSRF tokens
4. ❌ Rate limiting
5. ❌ Authentication bypass
6. ❌ Authorization (user can only access own requests)

---

## Prioriteetide Järjekord

### Sprint 1 (Kriitilised parandused)
1. Mineviku kuupäevade blokeerimine
2. StartDate === EndDate lubamine
3. Error logging
4. Database migrations setup
5. Concurrency handling

### Sprint 2 (Kasutajakogemus)
1. Success notifications
2. Loading indicators
3. Better error messages
4. Confirmation dialogs
5. Input sanitization

### Sprint 3 (Täiustused)
1. Pagination
2. Sorting/filtering
3. Unit tests
4. Integration tests
5. Performance optimization

### Sprint 4 (Nice-to-have)
1. Kalendri vaade
2. Export functionality
3. Statistics
4. Email notifications
5. Dark mode

---

## Kokkuvõte

**Praegune kood on hea MVP, aga vajab järgmist:**

✅ **Töötab hästi:**
- Põhifunktsionaalsus (CRUD)
- Responsive disain
- Clean code structure
- Good separation of concerns

❌ **Vajab parandamist:**
- Mineviku kuupäevade valdamine
- Error handling ja logging
- Security hardening
- Testing
- Production readiness

⚠️ **Võiks olla parem:**
- UX/UI polish
- Performance optimization
- Accessibility
- Documentation
