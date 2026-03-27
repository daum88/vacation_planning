# Puhkusetaotluste haldussüsteem

Ettevõttesisene veebirakendus puhkusetaotluste esitamiseks, kinnitamiseks ja haldamiseks. Loodud React'i (frontend) ja .NET 8 (backend) baasil, andmebaasina SQLite.

---

## Funktsionaalsus

### Töötaja
- Puhkusetaotluse esitamine koos puhkusetüübi, kuupäevade ja kommentaariga
- Tööpäevade automaatne arvestus (riigipühad välistatud)
- Taotluste ajalugu ja staatuse jälgimine
- Taotluse muutmine ja tühistamine (kui veel ootel)
- Kinnitamise delegeerimine asendajale eemaloleku ajaks
- Meeskonnakalender ja aasta-ülevaade
- Teavituskell uute kommentaaride ja staatuse muutuste kohta
- Parooli vahetamine

### Administraator
- Kõikide taotluste vaatamine, kinnitamine ja tagasilükkamine
- Kasutajate haldus: kutsumine, aktiveerimine, admini õigused
- Liitumistaotluste haldus (ise registreerunud kasutajad)
- Puhkusetüüpide haldus (tasustatud/tasustamata, kinnitust nõudev jne)
- Riigipühade haldus (korduvad ja ühekordsed)
- Osakonna täituvuse piirangud ja blokeeritud perioodid
- Auditilogi kõikide süsteemisündmuste kohta
- Taotluste muudatuste ajalugu

### Turvalisus
- JWT autentimine (8h kehtivus)
- BCrypt paroolide räsimine (workFactor 12)
- Päringute piiramine (5 sisselogimist/min, 100 API-päringut/min)
- CORS-piiramine, turvaheadereid, sisendi puhastamine

---

## Tehnoloogiad

| Kiht | Tehnoloogia |
|------|-------------|
| Frontend | React 18, plain CSS, Axios |
| Backend | .NET 8, ASP.NET Core, Entity Framework Core |
| Andmebaas | SQLite |
| Autentimine | JWT Bearer tokens |
| Paroolid | BCrypt.Net |

---

## Käivitamine

### Eeldused
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) ja npm

---

### 1. Backend

```bash
cd backend
dotnet restore
dotnet run
```

Backend käivitub aadressil `http://localhost:5000`.  
Esimesel käivitusel luuakse andmebaas (`vacationrequests.db`) automaatselt koos testandmetega.

> **NB!** Kui oled muutnud andmebaasi skeemi, kustuta `backend/vacationrequests.db` enne uuesti käivitamist.

---

### 2. Frontend

```bash
cd frontend
npm install
npm start
```

Frontend käivitub aadressil `http://localhost:3000`.  
API-päringud suunatakse automaatselt proxy kaudu `localhost:5000`-le.

---

### Testkontod

| Kasutaja | E-post | Parool | Roll |
|----------|--------|--------|------|
| Jüri Juurikas | juri.juurikas@example.com | Password123 | Admin |
| Kati Kask | kati.kask@example.com | Password123 | Admin |
| Mari Maasikas | mari.maasikas@example.com | Password123 | Töötaja |
| Peeter Pihlakas | peeter.pihlakas@example.com | Password123 | Töötaja |
| Liisa Lepp | liisa.lepp@example.com | Password123 | Töötaja |

---

## Projekti struktuur

```
vacation-request-app/
├── backend/
│   ├── Controllers/       # API lõpp-punktid
│   ├── Models/            # Andmemudelid
│   ├── DTOs/              # Andmeedastuse objektid
│   ├── Services/          # Äriloogika
│   ├── Data/              # EF Core kontekst ja seed-andmed
│   ├── Middleware/        # Erindite käsitlemine, logimine
│   └── Program.cs         # Rakenduse käivitamine
└── frontend/
    └── src/
        ├── api/           # Axios API kliendid
        ├── components/    # React komponendid
        ├── hooks/         # Kohandatud React hookid
        └── utils/         # Abifunktsioonid
```

---

## Konfiguratsioon (tootmiseks)

Enne tootmiskeskkonda viimist muuda `backend/appsettings.json`:

```json
{
  "Jwt": {
    "SecretKey": "genereeri-uus-võti-käsuga: openssl rand -base64 64"
  },
  "Cors": {
    "AllowedOrigins": ["https://sinu-domeen.ee"]
  },
  "EmailSettings": {
    "Enabled": true,
    "SmtpHost": "smtp.sinu-server.ee",
    "SmtpPort": 587,
    "FromAddress": "noreply@sinu-domeen.ee"
  }
}
```
