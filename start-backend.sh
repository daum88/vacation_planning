#!/bin/bash

echo "Käivitan backendit..."
echo "Backend URL: http://localhost:5000"
echo "Swagger UI: http://localhost:5000/swagger"
echo ""

cd backend
dotnet run --launch-profile http
