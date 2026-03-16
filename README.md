# Puhkusetaotluste Haldamise Rakendus

Täisfunktsionaalne CRUD veebirakendus ettevõtte töötajate puhkusesoovide haldamiseks.

## ⚡ Kiirkäivitus

```bash
# 1. Käivita automaatne setup
./setup.sh

# 2. Käivita backend (uus terminal)
./start-backend.sh
# või
cd backend && dotnet run

# 3. Käivita frontend (veel üks uus terminal)
./start-frontend.sh
# või
cd frontend && npm install && npm start
```

Rakendus avaneb automaatselt aadressil: **https://localhost:8080**

## 🎯 Funktsionaalsus

### ✅ Põhifunktsioonid (CRUD)
- **Create**: Uute puhkusetaotluste loomine
- **Read**: Kõikide taotluste vaatamine kaartidena
- **Update**: Olemasolevate taotluste muutmine
- **Delete**: Taotluste kustutamine kinnitusdialogiga

### ✅ Validatsioonid
- Alguskuupäev ei saa olla minevikus (uute taotluste puhul)
- Ühe päeva puhkus on lubatud (start === end)
- Maksimaalne puhkuse kestus: 90 päeva
- Kattuvate perioodide automaatne blokeerimine
- Real-time vorm validatsioon visuaalse tagasisidega
- XSS kaitse kommentaaride väljal

### ✅ Statistika & Analüüs
- **Dashboard**: Visuaalne ülevaade puhkustest
  - Kokku päevi (kogu aeg)
  - Kokku taotlusi
  - Päevi käesoleval aastal
  - Tulevaste puhkuste arv
  - Järgmise puhkuse kuupäev
- **Kuude kaupa jaotus**: Viimased 12 kuud graafiliselt
- **Toggle vaade**: Lihtne vahetus taotluste ja statistika vahel

### ✅ Ekspordi Funktsioonid
- **CSV eksport**:
  - Kõik taotlused ühes failis
  - Excel'is avatav
  - Eesti keelsed päised
- **iCalendar (.ics) eksport**:
  - Google Calendar import
  - Outlook import
  - Apple Calendar import
  - RFC 5545 standard formaat

### ✅ Kasutajakogemus
- Responsive disain (mobile, tablet, desktop)
- Automaatne päevade arvu arvutamine
- Loading state'id
- Error handling koos retry funktsiooniga
- Smooth animatsioonid ja hover efektid
- Empty state kui taotlusi pole

## 📋 Nõuded

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) või uuem
- [Node.js](https://nodejs.org/) v18 või uuem ja npm

## 🏗️ Arhitektuur

```
┌─────────────────┐      HTTPS       ┌─────────────────┐      ┌──────────┐
│  React Frontend │ ◄────REST API───► │ ASP.NET Backend │ ◄───►│  SQLite  │
│   Port 8080     │                   │   Port 5001     │      │    DB    │
└─────────────────┘                   └─────────────────┘      └──────────┘
```

### Backend (ASP.NET Core 8.0)
- **REST API** kontroller kõikide operatsioonidega
- **Entity Framework Core** ORM
- **SQLite** andmebaas (fail: `vacationrequests.db`)
- **Swagger UI** API dokumentatsioon
- **ILogger** structured logging
- **CORS** konfigureeritud frontendile

### Frontend (React 18)
- **Functional components** koos Hooks'idega
- **Axios** HTTP klient
- **CSS3** responsive disain ilma frameworkita
- **Modern JavaScript** (ES6+)

## 🚀 Käivitamine

## 🚀 Käivitamine

### Automaatne Setup (Soovitatav)

```bash
# 1. Käivita setup skript
./setup.sh

# 2. Käivita backend
./start-backend.sh  # või: cd backend && dotnet run

# 3. Käivita frontend (uus terminal)
./start-frontend.sh  # või: cd frontend && npm start
```

### Manuaalne Setup

#### Backend

```bash
cd backend
dotnet restore
dotnet run
```

Backend käivitub aadressil: `https://localhost:5001`
Swagger UI: `https://localhost:5001/swagger`

#### Backend

```bash
cd backend
dotnet restore
dotnet run
```

**Backend käivitub:**
- API: `https://localhost:5001`
- Swagger: `https://localhost:5001/swagger`

#### Frontend

Avage uus terminal:

```bash
cd frontend
npm install
npm start
```

**Frontend käivitub:**
- URL: `https://localhost:8080`
- Avaneb automaatselt brauseris

## 📖 Kasutamine

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
