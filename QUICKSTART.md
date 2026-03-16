# Kiirjuhend - Rakenduse Käivitamine

## Variant 1: Automatiseeritud Setup (soovitatav)

### 1. Käivita setup skript
```bash
./setup.sh
```

### 2. Käivita backend (uus terminali aken)
```bash
./start-backend.sh
```
Backend käivitub: https://localhost:5001

### 3. Käivita frontend (veel üks uus terminali aken)
```bash
./start-frontend.sh
```
Frontend käivitub: https://localhost:8080 ja avaneb automaatselt brauseris

---

## Variant 2: Manuaalne Setup

### Backend

```bash
cd backend
dotnet restore
dotnet run
```

### Frontend

Uues terminali aknas:
```bash
cd frontend
npm install
npm start
```

---

## Probleemide lahendamine

### HTTPS sertifikaadi hoiatused

Kui brauser näitab HTTPS sertifikaadi hoiatust:
- **Chrome**: Kliki "Advanced" → "Proceed to localhost (unsafe)"
- **Firefox**: Kliki "Advanced" → "Accept the Risk and Continue"
- **Safari**: Kliki "Show Details" → "visit this website"

See on normaalne development keskkonnas, kuna kasutame self-signed sertifikaate.

### Backend ei käivitu

1. Kontrolli, kas .NET SDK on installitud:
```bash
dotnet --version
```

2. Kui .NET pole installitud, laadi alla:
https://dotnet.microsoft.com/download

### Frontend ei käivitu

1. Kontrolli, kas Node.js on installitud:
```bash
node --version
npm --version
```

2. Kui Node.js pole installitud, laadi alla:
https://nodejs.org/

3. Kui node_modules puudub:
```bash
cd frontend
npm install
```

### CORS vead

Veendu, et:
1. Backend töötab aadressil https://localhost:5001
2. Frontend töötab aadressil https://localhost:8080
3. Mõlemad kasutavad HTTPS protokolli

### Port on juba kasutusel

Kui port 5001 või 8080 on juba kasutusel:

**Backend:**
Muuda `backend/Properties/launchSettings.json` failis `applicationUrl` väärtust

**Frontend:**
Muuda `frontend/package.json` failis `start` skripti PORT väärtust

---

## Esimene kasutamine

1. Ava brauser aadressil https://localhost:8080
2. Aktsepteeri HTTPS sertifikaadi hoiatus (development keskkonnas normaalne)
3. Näed puhkusetaotluste vormi
4. Loo oma esimene puhkusetaotlus!

---

## API testimine

Backend pakub Swagger UI-d API testimiseks:
https://localhost:5001/swagger

Siin saad testida kõiki API otspunkte ilma frontendita.

---

## Andmebaasi asukoht

SQLite andmebaas luuakse automaatselt siia:
```
backend/vacationrequests.db
```

Andmebaasi lähtestamiseks kustuta see fail ja taaskäivita backend.
