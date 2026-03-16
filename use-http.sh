#!/bin/bash

# Quick script to switch to HTTP (no SSL warnings)

echo "Switching to HTTP mode..."

# Update frontend .env
cat > frontend/.env << 'ENVEOF'
REACT_APP_API_URL=http://localhost:5001/api
PORT=8080
HTTPS=false
ENVEOF

echo "✅ Frontend configured for HTTP"
echo ""
echo "Now run:"
echo "  Terminal 1: ./start-backend.sh"
echo "  Terminal 2: ./start-frontend.sh"
echo ""
echo "Access: http://localhost:8080 (no HTTPS warning!)"
