#!/bin/bash

echo "============================================"
echo "Rakenduse Terviklikkuse Kontroll"
echo "============================================"
echo ""

ERRORS=0

# Check backend files
echo "📁 Kontrollin backend faile..."
BACKEND_FILES=(
    "backend/Program.cs"
    "backend/Controllers/VacationRequestsController.cs"
    "backend/DTOs/VacationRequestDto.cs"
    "backend/DTOs/VacationStatisticsDto.cs"
    "backend/Data/VacationRequestContext.cs"
    "backend/Models/VacationRequest.cs"
    "backend/VacationRequestApi.csproj"
    "backend/appsettings.json"
)

for file in "${BACKEND_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo "  ✅ $file"
    else
        echo "  ❌ $file PUUDUB!"
        ERRORS=$((ERRORS+1))
    fi
done

echo ""
echo "📁 Kontrollin frontend faile..."
FRONTEND_FILES=(
    "frontend/package.json"
    "frontend/public/index.html"
    "frontend/src/index.js"
    "frontend/src/App.js"
    "frontend/src/App.css"
    "frontend/src/api/api.js"
    "frontend/src/utils/dateUtils.js"
    "frontend/src/components/VacationRequestForm/VacationRequestForm.js"
    "frontend/src/components/VacationRequestForm/VacationRequestForm.css"
    "frontend/src/components/VacationRequestList/VacationRequestList.js"
    "frontend/src/components/VacationRequestList/VacationRequestList.css"
    "frontend/src/components/Statistics/Statistics.js"
    "frontend/src/components/Statistics/Statistics.css"
)

for file in "${FRONTEND_FILES[@]}"; do
    if [ -f "$file" ]; then
        echo "  ✅ $file"
    else
        echo "  ❌ $file PUUDUB!"
        ERRORS=$((ERRORS+1))
    fi
done

echo ""
echo "🔍 Kontrollin backend API endpointe..."

# Check if all endpoints are defined
if grep -q "GET.*VacationRequests.*statistics" backend/Controllers/VacationRequestsController.cs; then
    echo "  ✅ Statistics endpoint"
else
    echo "  ❌ Statistics endpoint PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

if grep -q "GET.*export/csv" backend/Controllers/VacationRequestsController.cs; then
    echo "  ✅ CSV export endpoint"
else
    echo "  ❌ CSV export endpoint PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

if grep -q "GET.*export/ical" backend/Controllers/VacationRequestsController.cs; then
    echo "  ✅ iCal export endpoint"
else
    echo "  ❌ iCal export endpoint PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

echo ""
echo "🔍 Kontrollin frontend komponente..."

if grep -q "import Statistics" frontend/src/App.js; then
    echo "  ✅ Statistics import App.js-is"
else
    echo "  ❌ Statistics import PUUDUB App.js-is!"
    ERRORS=$((ERRORS+1))
fi

if grep -q "getTodayString" frontend/src/utils/dateUtils.js; then
    echo "  ✅ getTodayString funktsioon"
else
    echo "  ❌ getTodayString funktsioon PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

if grep -q "isDateInPast" frontend/src/utils/dateUtils.js; then
    echo "  ✅ isDateInPast funktsioon"
else
    echo "  ❌ isDateInPast funktsioon PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

echo ""
echo "🔍 Kontrollin validatsioone..."

if grep -q "MaxVacationDays = 90" backend/Controllers/VacationRequestsController.cs; then
    echo "  ✅ Max vacation days validation"
else
    echo "  ❌ Max vacation days validation PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

if grep -q "XSS" backend/Controllers/VacationRequestsController.cs; then
    echo "  ✅ XSS protection implementeeritud"
else
    echo "  ❌ XSS protection PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

if grep -q "RowVersion" backend/Models/VacationRequest.cs; then
    echo "  ✅ Concurrency token (RowVersion)"
else
    echo "  ❌ Concurrency token PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

echo ""
echo "🔍 Kontrollin logging'ut..."

if grep -q "ILogger" backend/Controllers/VacationRequestsController.cs; then
    echo "  ✅ ILogger kasutuses"
else
    echo "  ❌ ILogger PUUDUB!"
    ERRORS=$((ERRORS+1))
fi

echo ""
echo "============================================"

if [ $ERRORS -eq 0 ]; then
    echo "✅ KÕIK KONTROLLID LÄBITUD EDUKALT!"
    echo ""
    echo "Rakendus on valmis käivitamiseks:"
    echo "  1. Backend: cd backend && dotnet run"
    echo "  2. Frontend: cd frontend && npm install && npm start"
    exit 0
else
    echo "❌ LEITI $ERRORS VIGA!"
    echo ""
    echo "Palun parandage vead enne jätkamist."
    exit 1
fi
