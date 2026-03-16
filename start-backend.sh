#!/bin/bash

echo "Käivitan backendit..."
echo "Backend URL: https://localhost:5001"
echo "Swagger UI: https://localhost:5001/swagger"
echo ""

cd backend
dotnet run
