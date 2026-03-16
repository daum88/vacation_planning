# Puhkusetaotluste Haldamise Rakendus

Lihtne CRUD veebirakendus ettevõtte töötajate puhkusesoovide haldamiseks.

## Arhitektuur

Rakendus koosneb kolmest komponendist:

1. **Frontend** - React rakendus (Node.js server, port 8080)
2. **Backend** - ASP.NET Core REST API (port 5001)
3. **Andmebaas** - SQLite

## Funktsionaalsus

- ✅ Puhkusesoovide loomine
- ✅ Olemasolevate taotluste vaatamine
- ✅ Taotluste muutmine
- ✅ Taotluste kustutamine
- ✅ Automaatne päevade arvu arvutamine
- ✅ Validatsioon (alguskuupäev peab olema enne lõppkuupäeva)
- ✅ Kontrollimine kattuvate perioodide vältimiseks

## Tehnoloogiad

### Backend
- ASP.NET Core 8.0
- Entity Framework Core
- SQLite
- Swagger UI (API dokumentatsioon)

### Frontend
- React 18
- Axios (HTTP klient)
- CSS3 (responsive disain)

## Eeldused

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) või uuem
- [Node.js](https://nodejs.org/) (v18 või uuem) ja npm

## Paigaldamine ja Käivitamine

### 1. Kloonige repositoorium

```bash
git clone [repository-url]
cd vacation-request-app
```

### 2. Backend käivitamine

```bash
cd backend
dotnet restore
dotnet run
```

Backend käivitub aadressil: `https://localhost:5001`
Swagger UI: `https://localhost:5001/swagger`

### 3. Frontend käivitamine

Avage uus terminali aken:

```bash
cd frontend
npm install
npm start
```

Frontend käivitub aadressil: `https://localhost:8080`

## Kasutamine

1. Avage brauser ja minge aadressile `https://localhost:8080`
2. Täitke vorm uue puhkusetaotluse loomiseks:
   - Valige alguskuupäev
   - Valige lõppkuupäev
   - Lisage vajadusel kommentaar
   - Näete automaatselt arvutatud päevade arvu
3. Vajutage "Loo taotlus" nuppu
4. Kõik teie taotlused kuvatakse allpool
5. Taotlusi saab muuta ("Muuda" nupp) või kustutada ("Kustuta" nupp)

## Projekti struktuur

```
vacation-request-app/
├── backend/
│   ├── Controllers/
│   │   └── VacationRequestsController.cs
│   ├── Data/
│   │   └── VacationRequestContext.cs
│   ├── DTOs/
│   │   └── VacationRequestDto.cs
│   ├── Models/
│   │   └── VacationRequest.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── appsettings.json
│   ├── Program.cs
│   └── VacationRequestApi.csproj
│
└── frontend/
    ├── public/
    │   └── index.html
    ├── src/
    │   ├── api/
    │   │   └── api.js
    │   ├── components/
    │   │   ├── VacationRequestForm/
    │   │   │   ├── VacationRequestForm.js
    │   │   │   └── VacationRequestForm.css
    │   │   └── VacationRequestList/
    │   │       ├── VacationRequestList.js
    │   │       └── VacationRequestList.css
    │   ├── utils/
    │   │   └── dateUtils.js
    │   ├── App.js
    │   ├── App.css
    │   ├── index.js
    │   └── index.css
    └── package.json
```

## API Otspunktid

### GET /api/VacationRequests
Tagastab kõik kasutaja puhkusetaotlused

### GET /api/VacationRequests/{id}
Tagastab konkreetse taotluse

### POST /api/VacationRequests
Loob uue puhkusetaotluse

**Body:**
```json
{
  "startDate": "2026-07-01T00:00:00Z",
  "endDate": "2026-07-15T00:00:00Z",
  "comment": "Suvepuhkus"
}
```

### PUT /api/VacationRequests/{id}
Uuendab olemasolevat taotlust

### DELETE /api/VacationRequests/{id}
Kustutab taotluse

## Andmebaas

SQLite andmebaas (`vacationrequests.db`) luuakse automaatselt backend käivitamisel. Andmebaas sisaldab järgmist tabelit:

**VacationRequests**
- Id (INTEGER, PRIMARY KEY)
- UserId (INTEGER, NOT NULL)
- StartDate (TEXT, NOT NULL)
- EndDate (TEXT, NOT NULL)
- Comment (TEXT, nullable, max 500 tähemärki)
- CreatedAt (TEXT)
- UpdatedAt (TEXT)

## Validatsioonid

- Alguskuupäev peab olema enne lõppkuupäeva
- Samale perioodile ei saa luua kattuvaid taotlusi
- Kommentaar on piiratud 500 tähemärgiga
- Kõik väljad välja arvatud kommentaar on kohustuslikud

## Märkused

- UserID on hetkel hardcoded (väärtus: 1), kuna autentimine ei ole nõutud
- Rakendus kasutab HTTPS-i (self-signed sertifikaadid)
- CORS on konfigureeritud lubama frontend ligipääsu backendile

## Arendamine

### Backend testimine Swagger UI-ga

Külastage `https://localhost:5001/swagger` API testimiseks ilma frontendita.

### Andmebaasi lähtestamine

Kui soovite andmebaasi lähtestada, kustutage fail `backend/vacationrequests.db` ja taaskäivitage backend.

## Litsents

See projekt on loodud testülesandeks.
