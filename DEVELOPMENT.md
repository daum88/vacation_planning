# Arendajate Juhend

## Projekti Ülevaade

See on CRUD veebirakendus puhkusetaotluste haldamiseks. Rakendus järgib kolmekihilist arhitektuuri.

## Arhitektuur

```
Frontend (React) <---> Backend (ASP.NET Core) <---> Database (SQLite)
    :8080                    :5001
```

## Tehnilised Otsused

### Backend

**Raamistik:** ASP.NET Core 8.0
- Valik põhjus: Nõue ülesandes
- REST API arhitektuur
- Entity Framework Core ORM

**Validatsioonid:**
1. StartDate < EndDate
2. Kattuvate perioodide kontroll
3. Comment max 500 tähemärki

**CORS konfiguratsioon:**
```csharp
policy.WithOrigins("https://localhost:8080", "http://localhost:8080")
```

### Frontend

**Raamistik:** React 18
- Funktsionaalsed komponendid
- React Hooks (useState, useEffect)
- Axios HTTP klient

**Komponendid:**
- `VacationRequestForm` - CRUD vorm (Create/Update)
- `VacationRequestList` - Taotluste loend
- `App` - Peamine kontainer

**State Management:**
- Lihtne useState (piisav väikese rakenduse jaoks)
- Pole vaja Redux/Context API

### Andmebaas

**SQLite:**
- Lihtne setup (üks fail)
- Pole vaja eraldi server protsessi
- Ideaalne arenduseks ja väikesteks rakendusteks

**Skeem:**
```sql
VacationRequests (
    Id INTEGER PRIMARY KEY,
    UserId INTEGER NOT NULL,
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    Comment TEXT,
    CreatedAt TEXT,
    UpdatedAt TEXT
)
```

## Tulevased Täiustused

### Autentimine
Praegu on UserID hardcoded (väärtus: 1). Tootmises lisada:
- JWT autentimine
- Kasutajate tabel
- Login/Register funktsioonid

### Täiendavad Funktsioonid
- [ ] Puhkusetaotluste kinnitus/tagasilükkamine
- [ ] E-mail teavitused
- [ ] Kalendri vaade
- [ ] Meeskonna puhkuste ülevaade
- [ ] PDF eksport
- [ ] Statistika (kokku päevi jne)
- [ ] Filtreerimine ja otsimine

### Tehnilised Täiustused
- [ ] Unit testid (backend: xUnit, frontend: Jest)
- [ ] Pagination suurte andmehulkade jaoks
- [ ] Optimistic UI updates
- [ ] Error boundary komponendid
- [ ] Loading skeletons
- [ ] Offline tugi (Service Workers)

## Arenduskeskkonna Seadistamine

### VS Code laiendused (soovitatav)
- C# Dev Kit (Microsoft)
- ESLint
- Prettier
- SQLite Viewer

### Debugimine

**Backend:**
```bash
cd backend
dotnet watch run
```
Muudatused rakenduvad automaatselt.

**Frontend:**
```bash
cd frontend
npm start
```
Hot reload on vaikimisi sisse lülitatud.

## API Dokumentatsioon

Swagger UI: https://localhost:5001/swagger

Kõik otspunktid:
- `GET /api/VacationRequests` - Kõik taotlused
- `GET /api/VacationRequests/{id}` - Üks taotlus
- `POST /api/VacationRequests` - Loo uus
- `PUT /api/VacationRequests/{id}` - Uuenda
- `DELETE /api/VacationRequests/{id}` - Kustuta

## Koodinõuded

### C# (Backend)
- Async/await kõigile DB operatsioonidele
- Try-catch error handling
- DTO-d API suhtluseks
- Meaningful muutuja nimed

### JavaScript (Frontend)
- Funktsionaalsed komponendid
- Destructuring
- Arrow functions
- Async/await axios päringuteks
- PropTypes või TypeScript (tulevikus)

## Testimine

### Käsitsi testimine
1. Loo taotlus - kontrollid, kas ilmub loendisse
2. Muuda taotlust - kontrollid, kas muudatused salvestuvad
3. Kustuta taotlus - kontrollid, kas kaob loendist
4. Kattuv periood - proovi luua kattuvat taotlust
5. Vale kuupäevad - proovi end < start

### Automaatsed testid (tulevikus)

**Backend testid:**
```bash
cd backend
dotnet test
```

**Frontend testid:**
```bash
cd frontend
npm test
```

## Deployment

### Production build

**Frontend:**
```bash
cd frontend
npm run build
```
Optimeeritud build tekib `frontend/build/` kausta.

**Backend:**
```bash
cd backend
dotnet publish -c Release -o ./publish
```

### Environment muutujad

**Frontend .env:**
```
REACT_APP_API_URL=https://your-api-domain.com/api
```

**Backend appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/path/to/production.db"
  }
}
```

## Probleemide lahendamine

### Andmebaas on "locked"
SQLite võib lukustada DB faili. Lahendus:
```bash
rm backend/vacationrequests.db
# Taaskäivita backend
```

### CORS vead
Kontrolli `Program.cs` failis CORS policy konfiguratsiooni.

### Port konfliktid
Muuda `launchSettings.json` (backend) või `package.json` (frontend).

## Kontakt

Projektiga seotud küsimuste korral loo Issue GitHubis või võta ühendust meeskonnaga.

---

Edu arendamisel! 🚀
