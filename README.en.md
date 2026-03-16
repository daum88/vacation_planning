# Vacation Request Management Application

A simple CRUD web application for company employees to manage vacation requests.

## Features

- ✅ Create vacation requests with start date, end date, and optional comment
- ✅ Automatic calculation of days between dates
- ✅ View all submitted vacation requests
- ✅ Edit existing vacation requests
- ✅ Delete vacation requests
- ✅ Validation to prevent invalid date ranges
- ✅ Prevention of overlapping vacation periods

## Quick Start

### Prerequisites
- .NET SDK 8.0 or newer
- Node.js v18 or newer

### Backend Setup
```bash
cd backend
dotnet restore
dotnet run
```
Backend runs on: https://localhost:5001

### Frontend Setup
```bash
cd frontend
npm install
npm start
```
Frontend runs on: https://localhost:8080

## Technology Stack

**Frontend:**
- React 18
- Axios for HTTP requests
- Modern CSS with responsive design

**Backend:**
- ASP.NET Core 8.0
- Entity Framework Core
- REST API with Swagger documentation

**Database:**
- SQLite (auto-created on first run)

## Project Structure

The application follows a three-tier architecture:
1. **Frontend** - React SPA running on Node.js server
2. **Backend** - ASP.NET Core REST API
3. **Database** - SQLite for simple data persistence

For detailed documentation in Estonian, see [README.md](README.md)
