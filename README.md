# Online Judge Backend — LeetCode Compiler API

A high-performance, multi-language code execution backend built with **ASP.NET Core 8** and **Docker container pooling**. Designed to power the GateTutor coding platform with support for assignment-based coding tests, faculty dashboards, practice tests, and real-time code execution.

---

## 🧰 Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 (C#) |
| Database | SQL Server + Entity Framework Core |
| Code Execution | Docker (pre-pooled containers) |
| Logging | Serilog (console + rolling file) |
| Authentication | JWT Bearer (production) / open (development) |
| Rate Limiting | ASP.NET Core Rate Limiting (production only) |
| Caching | In-Memory Cache |

---

## 🚀 Quick Start (Development)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/) running at `192.168.0.102,1433`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for code execution containers)

### Run the API
```powershell
cd LeetCodeCompiler.API
dotnet run
```

The API will start on:
- **HTTP**: `http://0.0.0.0:5081`
- **HTTPS**: `https://0.0.0.0:7169`
- **Swagger UI**: `http://localhost:5081/swagger` _(Development only)_

### Environment Modes
| Mode | Auth | Rate Limiting | CORS |
|---|---|---|---|
| Development | Disabled (open) | Disabled | Allow all origins |
| Production | JWT required | Enabled | Specific origins only |

Set mode via environment variable:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"   # or "Production"
```

---

## 🐳 Docker Deployment

```bash
# Build and run with docker-compose
docker-compose up --build

# Or build manually
docker build -t leetcode-api .
docker run -p 5081:5081 -p 7169:7169 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  leetcode-api
```

---

## 🗄️ Database

**Connection:** `Server=192.168.0.102,1433;Database=LeetCode;User ID=sa;Password=pass@123;TrustServerCertificate=True;`

### Key Tables
| Table | Purpose |
|---|---|
| `Problems` | DSA coding problems |
| `TestCases` | Input/output test cases per problem |
| `StarterCodes` | Language-specific starter templates |
| `CodingTests` | Test assignments created by faculty |
| `AssignedCodingTests` | Test-to-student assignments |
| `CodingTestAttempts` | Student test sessions |
| `CodingTestSubmissions` | Whole-test submission records |
| `CodingTestQuestionAttempts` | Per-question attempt tracking |
| `PracticeTests` | Practice mode test sessions |
| `UserCodingActivityLog` | Detailed user behavior tracking |

### Apply Migrations
```powershell
dotnet ef database update --project LeetCodeCompiler.API
```

---

## 🔗 Key API Areas

| Area | Base Route |
|---|---|
| Code Execution | `POST /api/CodeExecution` |
| Problems CRUD | `/api/Problems` |
| Domains / Subdomains | `/api/Domain`, `/api/Subdomain` |
| Coding Tests | `/api/CodingTest` |
| Practice Tests | `/api/PracticeTest` |
| Faculty Dashboard | `/api/FacultyDashboard` |
| Faculty Performance | `/api/FacultyPerformance` |
| Student Performance | `/api/StudentPerformance` |
| Activity Tracking | `/api/UserActivity` |
| Health Check | `GET /health` |

Full API docs: see [`API_DOCUMENTATION.md`](LeetCodeCompiler.API/API_DOCUMENTATION.md)

---

## ⚡ Performance

Uses a **pre-pooled Docker container system** to eliminate cold-start overhead:

| Language | Pool Size | Container Image |
|---|---|---|
| Python | 30 | `python:3.9-slim` |
| JavaScript | 20 | `node:18-slim` |
| Java | 15 | `openjdk:17-slim` |
| C++ | 15 | `gcc:latest` |
| C | 5 | `gcc:latest` |

- Execution time: **~0.1–2ms** (vs 22+ seconds before pooling)
- Capacity: **30,000+ concurrent users**

---

## 📁 Project Structure

```
LeetCodeCompiler.API/
├── Controllers/          # API endpoint controllers
├── Services/             # Business logic & execution services
├── Models/               # EF entities & DTOs
├── Data/                 # AppDbContext & migrations
├── Migrations/           # EF Core migration files
├── logs/                 # Serilog rolling log files
├── Program.cs            # App entry point & DI setup
├── appsettings.json      # Base configuration
├── Dockerfile            # Container image definition
├── docker-compose.yml    # Multi-container setup
└── deploy.ps1            # Windows deployment helper script
```

---

## 🔒 Production Security

- JWT Bearer authentication (claims-based role access)
- Rate limiting: 100 req/min global, 10 exec/min per user
- Docker containers run with `--network=none` (no internet access)
- Security headers: `X-Frame-Options`, `X-XSS-Protection`, `X-Content-Type-Options`
- HTTPS enforced in production

---

## 📄 Documentation

| File | Description |
|---|---|
| [`API_DOCUMENTATION.md`](LeetCodeCompiler.API/API_DOCUMENTATION.md) | Full REST API reference |
| [`PROJECT_DOCUMENTATION.md`](LeetCodeCompiler.API/PROJECT_DOCUMENTATION.md) | Architecture & performance deep-dive |
| [`DEPLOYMENT.md`](LeetCodeCompiler.API/DEPLOYMENT.md) | Network deployment guide |
| [`CODING_TEST_API_DOCUMENTATION.md`](LeetCodeCompiler.API/CODING_TEST_API_DOCUMENTATION.md) | CodingTest API reference |
| [`COMPREHENSIVE_TEST_RESULTS_API.md`](LeetCodeCompiler.API/COMPREHENSIVE_TEST_RESULTS_API.md) | Test results & analytics API |

---

**Server:** `192.168.0.102` | **HTTP Port:** `5081` | **HTTPS Port:** `7169`
