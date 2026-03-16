#!/bin/bash

echo "=========================================="
echo "Puhkusetaotluste Rakendus - Setup Script"
echo "=========================================="
echo ""

# Check prerequisites
echo "Kontrollin eeldusi..."

if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK ei ole installitud. Palun installi .NET SDK 8.0 või uuem."
    echo "   Külasta: https://dotnet.microsoft.com/download"
    exit 1
fi

if ! command -v node &> /dev/null; then
    echo "❌ Node.js ei ole installitud. Palun installi Node.js v18 või uuem."
    echo "   Külasta: https://nodejs.org/"
    exit 1
fi

echo "✅ .NET SDK versioon: $(dotnet --version)"
echo "✅ Node.js versioon: $(node --version)"
echo ""

# Setup frontend
echo "Seadistan frontendit..."
cd frontend
if [ ! -d "node_modules" ]; then
    echo "Installin npm pakette..."
    npm install
    if [ $? -ne 0 ]; then
        echo "❌ npm install ebaõnnestus"
        exit 1
    fi
else
    echo "✅ node_modules juba olemas"
fi
cd ..

# Setup backend
echo ""
echo "Seadistan backendit..."
cd backend
echo "Taastamine NuGet pakette..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ dotnet restore ebaõnnestus"
    exit 1
fi
cd ..

echo ""
echo "=========================================="
echo "✅ Setup valmis!"
echo "=========================================="
echo ""
echo "Rakenduse käivitamiseks:"
echo ""
echo "1. Backend (uues terminali aknas):"
echo "   cd backend && dotnet run"
echo "   Backend: https://localhost:5001"
echo ""
echo "2. Frontend (uues terminali aknas):"
echo "   cd frontend && npm start"
echo "   Frontend: https://localhost:8080"
echo ""
echo "Või kasuta käivitusskripte:"
echo "   ./start-backend.sh"
echo "   ./start-frontend.sh"
echo ""
