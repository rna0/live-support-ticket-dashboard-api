# Live Support Ticket Dashboard API

## Overview

The Live Support Ticket Dashboard API is a robust, enterprise-grade backend service designed for customer support teams. Built with ASP.NET Core 8.0, this API provides real-time ticket management capabilities, agent collaboration features, and comprehensive support workflow management. The system enables support teams to efficiently handle customer inquiries, track ticket progress, and maintain high-quality customer service standards.

## Features

- **Real-time Ticket Management**: Create, update, and track support tickets with real-time synchronization using SignalR
- **Agent Management**: Complete agent registration, authentication, and session management system
- **Live Chat Integration**: Real-time messaging system for agent-customer communication
- **JWT Authentication**: Secure authentication and authorization with refresh token support
- **PostgreSQL Database**: Robust data persistence with optimized queries and migrations
- **RESTful API Design**: Clean, well-documented API endpoints following REST principles
- **Docker Support**: Containerized deployment with Docker Compose for easy setup
- **Health Checks**: Built-in health monitoring for API and database connections
- **CORS Configuration**: Cross-origin resource sharing support for frontend integration
- **Input Validation**: Comprehensive request validation with custom validation services
- **Swagger Documentation**: Interactive API documentation with OpenAPI specification

## Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: PostgreSQL with Npgsql driver
- **Real-time Communication**: SignalR
- **Authentication**: JWT Bearer tokens with BCrypt password hashing
- **Containerization**: Docker & Docker Compose
- **Documentation**: Swagger/OpenAPI
- **Testing**: xUnit (via LiveSupportDashboard.Tests project)
- **Architecture**: Clean Architecture with Domain-Driven Design principles

## Project Structure

```
├── LiveSupportDashboard/                 # Main API project
│   ├── Controllers/                      # API controllers
│   │   ├── AgentController.cs           # Agent management endpoints
│   │   ├── SessionsController.cs        # Session management endpoints
│   │   └── TicketsController.cs         # Ticket management endpoints
│   ├── Hubs/                            # SignalR hubs
│   │   └── LiveSupportHub.cs            # Real-time communication hub
│   ├── Infrastructure/                   # Data access layer
│   │   ├── Repositories/                # Repository implementations
│   │   ├── Services/                    # Infrastructure services
│   │   └── Queries/                     # SQL query files
│   ├── Services/                        # Business logic services
│   │   ├── Implementations/             # Service implementations
│   │   ├── Interfaces/                  # Service contracts
│   │   └── Validations/                 # Custom validation services
│   └── Program.cs                       # Application entry point
├── LiveSupportDashboard.Domain/         # Domain layer
│   ├── Contracts/                       # DTOs and request/response models
│   ├── Enums/                          # Domain enumerations
│   ├── Models/                         # Domain models
│   └── [Domain Entities].cs           # Core business entities
├── LiveSupportDashboard.Tests/         # Test project
├── db/                                 # Database scripts
│   └── init.sql                       # Database initialization script
└── docker-compose.yml                 # Docker composition file
```

## Getting Started

### Prerequisites

- **.NET 8.0 SDK** or higher
- **PostgreSQL 15** (or use Docker Compose)
- **Docker & Docker Compose** (recommended for easy setup)

### Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd live-support-ticket-dashboard-api
   ```

2. **Start the services**
   ```bash
   docker-compose up -d
   ```

   This will start:
   - PostgreSQL database on port 5432
   - API service on port 51 (mapped to container port 8080)

3. **Verify the setup**
   - API Health Check: `http://localhost:51/health`
   - Swagger Documentation: `http://localhost:51/swagger`
   - SignalR Hub: `ws://localhost:51/signalr/hubs`

### Manual Setup

1. **Database Setup**
   ```bash
   # Install PostgreSQL and create database
   createdb live_support_dashboard

   # Run initialization script
   psql -d live_support_dashboard -f db/init.sql
   ```

2. **Configure Application Settings**

   Create `appsettings.json` in the `LiveSupportDashboard` folder:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=live_support_dashboard;Username=postgres;Password=your_password"
     },
     "Jwt": {
       "Key": "your-secret-key-here-minimum-32-characters",
       "Issuer": "LiveSupportDashboard",
       "Audience": "LiveSupportDashboard",
       "ExpiryInMinutes": 60,
       "RefreshTokenExpiryInDays": 7
     },
     "SignalR": {
       "KeepAliveIntervalSeconds": 15,
       "ClientTimeoutIntervalSeconds": 30
     },
     "Cors": {
       "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
     }
   }
   ```

3. **Build and Run**
   ```bash
   cd LiveSupportDashboard
   dotnet restore
   dotnet build
   dotnet run
   ```

## API Endpoints

### Authentication
- `POST /api/agent/register` - Register new agent
- `POST /api/agent/login` - Agent login
- `POST /api/agent/refresh-token` - Refresh JWT token
- `POST /api/agent/logout` - Agent logout

### Ticket Management
- `GET /api/tickets` - Get tickets with pagination and filtering
- `POST /api/tickets` - Create new ticket
- `GET /api/tickets/{id}` - Get ticket by ID
- `PUT /api/tickets/{id}/status` - Update ticket status
- `PUT /api/tickets/{id}/assign` - Assign ticket to agent

### Session Management
- `GET /api/sessions` - Get active sessions
- `POST /api/sessions` - Create new session
- `GET /api/sessions/{id}` - Get session details

### Real-time Features
- SignalR Hub: `/signalr/hubs` - Real-time notifications and updates

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=live_support_dashboard;Username=postgres;Password=postgres

# JWT Configuration
Jwt__Key=your-secret-key-here-minimum-32-characters
Jwt__Issuer=LiveSupportDashboard
Jwt__Audience=LiveSupportDashboard
Jwt__ExpiryInMinutes=60

# SignalR Configuration
SignalR__KeepAliveIntervalSeconds=15
SignalR__ClientTimeoutIntervalSeconds=30

# CORS Configuration
Cors__AllowedOrigins__0=http://localhost:3000
Cors__AllowedOrigins__1=http://localhost:5173
```

### Docker Environment

The `docker-compose.yml` file includes:
- PostgreSQL database with automatic initialization
- API service with health checks
- Volume persistence for database data
- Network configuration for service communication

## Development

### Running Tests
```bash
cd LiveSupportDashboard.Tests
dotnet test
```

### Database Migrations
```bash
# Add new migration (if using EF Core migrations)
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### Building for Production
```bash
dotnet publish -c Release -o publish
```

## Deployment

### Docker Deployment
```bash
# Build and deploy with Docker Compose
docker-compose up -d --build

# Scale API service
docker-compose up -d --scale api=3
```

### Manual Deployment
1. Build the application: `dotnet publish -c Release`
2. Copy the published files to your server
3. Configure the production `appsettings.json`
4. Set up PostgreSQL database
5. Run the application: `dotnet LiveSupportDashboard.dll`

## Monitoring and Health Checks

The API includes built-in health checks:
- **API Health**: `/health` - Overall application health
- **Database Health**: Included in main health check
- **Dependencies**: PostgreSQL connection verification

## Security Features

- **JWT Authentication**: Secure token-based authentication
- **Password Hashing**: BCrypt for secure password storage
- **CORS Protection**: Configurable cross-origin resource sharing
- **Input Validation**: Comprehensive request validation
- **HTTPS Support**: SSL/TLS encryption support

## Contributing

1. Follow Clean Architecture principles
2. Implement comprehensive unit tests
3. Use meaningful commit messages
4. Follow C# coding conventions
5. Update documentation for new features

## API Documentation

When running the application, visit `/swagger` for interactive API documentation with:
- Complete endpoint documentation
- Request/response schemas
- Authentication requirements
- Example requests and responses

## License

Proprietary - All rights reserved

---

© 2025 Live Support Dashboard API. Last updated: October 5, 2025
