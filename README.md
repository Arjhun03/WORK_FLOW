# Smart Workflow & Approval Management System

A full-stack, enterprise-grade web application for designing and managing **dynamic, database-driven approval workflows**. Built with **ASP.NET Core Web API (.NET 10)**, **MongoDB Atlas**, and **React 19 (TypeScript + Vite + Tailwind CSS)**.

Workflows in this application are completely dynamic—administrators can create, modify, version, and reorder multi-stage approval pipelines directly from the UI without changing any backend source code.

---

## 🏛️ System Architecture

```text
                 ┌──────────────────────┐
                 │       Vercel         │
                 │                      │
                 │ React 19 + TypeScript│
                 │  Vite + Tailwind CSS │
                 └──────────┬───────────┘
                            │
                            │ HTTPS REST API + JWT
                            ↓
                 ┌──────────────────────┐
                 │  .NET Web API Server │
                 │       C# (.NET 10)   │
                 │                      │
                 │ Authentication       │
                 │ Dynamic Workflow Eng.│
                 │ RBAC Authorization   │
                 │ State Transitions    │
                 └──────────┬───────────┘
                            │
                            │ Official MongoDB.Driver
                            ↓
                 ┌──────────────────────┐
                 │    MongoDB Atlas     │
                 │                      │
                 │ • Users              │
                 │ • Roles              │
                 │ • Workflows          │
                 │ • Requests           │
                 │ • Approvals          │
                 │ • Comments           │
                 │ • Notifications      │
                 │ • Audit Logs         │
                 └──────────────────────┘
```

---

## 📁 Monorepo Structure

```text
smart-workflow-system/
│
├── client/
│   ├── src/
│   │   ├── components/         # Reusable UI elements
│   │   ├── pages/              # View pages (Dashboard, Workflows, Requests, Approvals, Users, Audit)
│   │   ├── layouts/            # Responsive AppLayout with mobile navigation
│   │   ├── hooks/              # Custom query hooks
│   │   ├── services/           # Axios API services
│   │   ├── context/            # AuthContext (JWT, roles, user profile)
│   │   ├── types/              # TypeScript types and interfaces
│   │   ├── routes/             # ProtectedRoute guards (Role/Approver)
│   │   ├── App.tsx             # Root router configuration
│   │   ├── main.tsx            # Entry point
│   │   └── index.css           # Modern glassmorphism & Tailwind styles
│   ├── public/
│   ├── vercel.json             # Vercel SPA routing rewrite configuration
│   ├── package.json
│   ├── vite.config.ts
│   └── .env.example
│
├── server/
│   └── SmartWorkflow.Api/
│       ├── Controllers/        # Auth, Users, Workflows, Requests, Approvals, Comments, Notifications, Audit, Dashboard
│       ├── Models/             # MongoDB entities (BaseEntity, User, Role, Workflow, Request, Approval, etc.)
│       ├── DTOs/               # Strongly-typed API contracts & standard ApiResponse<T>
│       ├── Services/           # WorkflowEngine, AuthService, RequestService, ApprovalService, etc.
│       ├── Interfaces/         # Service and Repository contracts
│       ├── Repositories/       # Generic MongoRepository and typed entity repositories
│       ├── Middleware/         # GlobalExceptionMiddleware
│       ├── Authentication/     # JwtService with claims handling
│       ├── Database/           # MongoDbContext, Index initialization, and DatabaseSeeder
│       ├── Dockerfile          # Multi-stage production container build
│       ├── Program.cs          # Pipeline, JWT auth, Swagger UI with Bearer support, CORS
│       └── appsettings.json
│
├── .gitignore
├── .env.example
└── README.md
```

---

## ⚡ Quick Start (Local Development)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js v18+](https://nodejs.org/) & `npm`
- [MongoDB Atlas Cluster](https://www.mongodb.com/atlas) (or local MongoDB)

### 1. Environment Configuration

Create or update `.env` in the root:

```env
MONGODB_CONNECTION_STRING=mongodb+srv://<username>:<password>@cluster0.mongodb.net/?retryWrites=true&w=majority
MONGODB_DATABASE_NAME=SmartWorkflowDb
JWT_SECRET=YourSuperSecretKeyWithAtLeast32CharactersRequiredForHmacSha256!
JWT_ISSUER=SmartWorkflowApi
JWT_AUDIENCE=SmartWorkflowClient
FRONTEND_URL=http://localhost:5173
```

In `client/.env`:
```env
VITE_API_BASE_URL=/api
```

### 2. Run the Backend API

```bash
cd server/SmartWorkflow.Api
dotnet run --launch-profile http
```
- API will start at: `http://localhost:5155`
- Swagger UI will be available at: `http://localhost:5155/swagger`
- Health check: `http://localhost:5155/api/health`

### 3. Run the Frontend Client

In a second terminal:
```bash
cd client
npm install
npm run dev
```
- Frontend will start at: `http://localhost:5173`

---

## 👥 Seed User Accounts

All default accounts are configured with password: **`password123`**

| Email | Roles | Department | Purpose |
|---|---|---|---|
| `admin@example.com` | `Admin` | Administration | Manage workflows, steps, users & view audit trail |
| `manager@example.com` | `Manager`, `Employee` | Engineering | Direct line manager approval step |
| `employee@example.com` | `Employee` | Engineering | Submits requests, tracks timeline, adds comments |
| `hr@example.com` | `HR`, `Employee` | Human Resources | Approves HR verification steps |
| `it@example.com` | `IT`, `Employee` | Information Technology | Approves equipment & software provisioning |
| `finance@example.com` | `Finance`, `Employee` | Finance | Approves budget & financial sign-offs |

> **Pro Tip**: The Login page features **1-click quick login buttons** for each role to demonstrate the multi-stage approval flow during demos!

---

## 🔄 Dynamic Workflow Engine Highlights

1. **Database-Driven**: Workflows and approval steps are modeled as dynamic BSON documents in MongoDB. No hardcoded `if (type == "Leave")` logic.
2. **Version Snapshots**: When a request is created, the system snapshots the active workflow version. If an admin edits the workflow later, in-flight requests preserve their original approval pathway.
3. **Pluggable Approver Strategies**:
   - `Manager`: Dynamically resolves to the requester's direct line manager (`requester.ManagerId`).
   - `Role`: Automatically routes to any active user possessing that organizational role (`HR`, `IT`, `Finance`).
   - `User`: Assigns a specific designated employee.
   - `DepartmentRole`: Routes to a role holder within the requester's specific department.
4. **State Machine & Validation**: Enforces legal state transitions (`Pending` ➔ `InProgress` ➔ `Completed` / `Rejected` / `Cancelled`). Prevents duplicate approvals and unauthorized actions.
5. **Immutable Audit Logging**: Every state change, approval sign-off, or workflow alteration generates an audit record with timestamp, actor, and old/new state values.
6. **In-App Notifications**: Real-time notifications for pending approvals, approval decisions, rejections, and discussion comments.

---

## ☁️ Deployment Guide

### Deploying Frontend to Vercel
1. Push your repository to GitHub.
2. Import the project in [Vercel](https://vercel.com).
3. Set **Root Directory** to `client`.
4. Configure Build Command: `npm run build` and Output Directory: `dist`.
5. Set Environment Variable:
   ```text
   VITE_API_BASE_URL=https://your-backend-api.onrender.com/api
   ```
6. The included `client/vercel.json` rewrite rule ensures client-side routing works seamlessly on hard refreshes.

### Deploying Backend to Render / Railway / Azure
1. Use the included Dockerfile (`server/SmartWorkflow.Api/Dockerfile`).
2. Set Environment Variables:
   - `MONGODB_CONNECTION_STRING`
   - `MONGODB_DATABASE_NAME`
   - `JWT_SECRET`
   - `JWT_ISSUER`
   - `JWT_AUDIENCE`
   - `FRONTEND_URL` (your Vercel app domain)
3. The health endpoint `GET /api/health` can be used for hosting liveness checks.

---

## 🛡️ MongoDB Atlas Configuration

1. Log into [MongoDB Atlas](https://www.mongodb.com/atlas).
2. Create a cluster (M0 Free Tier or higher).
3. Under **Database Access**, create a user with `Read and write to any database` permissions.
4. Under **Network Access**, add IP address `0.0.0.0/0` (Allow Access from Anywhere) or your deployment server IP.
5. Under **Deployment > Database**, click **Connect > Drivers > C# / .NET**, copy the connection string, and set it as `MONGODB_CONNECTION_STRING`.
