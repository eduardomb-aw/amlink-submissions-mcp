# AmLink Submissions MCP - Docker Setup

This project contains both an MCP Server and MCP Client that can be run using Docker and Docker Compose with full debugging support for development.

## Prerequisites

- **Docker Desktop** installed and running
- **.NET 10.0 SDK** (for local development without Docker)
- **VS Code** with C# Dev Kit and Docker extensions (for debugging)

## Services

- **amlink-mcp-server**: The MCP server running on ports 8080 (HTTP) / 8443 (HTTPS)
- **amlink-mcp-client**: The web client application running on ports 5000 (HTTP) / 5001 (HTTPS)

## Running with Docker Compose

### Development Mode (Automatic Debug Support)

The project uses `docker-compose.override.yml` which is automatically loaded for development with full debugging capabilities:

```bash
# Start development environment with debugging support
docker-compose up -d

# Build and run both services (if first time or after code changes)
docker-compose up -d --build

# View logs
docker-compose logs -f
docker-compose logs -f amlink-mcp-server  # Server logs only
docker-compose logs -f amlink-mcp-client   # Client logs only

# Stop services
docker-compose down
```

**Development features enabled:**
- Debug builds with symbols
- Hot reload support
- VS Code debugger integration
- Source code volume mapping
- Development-safe environment defaults

### Production Mode

```bash
# Production without debug features (skip override file)
docker-compose -f docker-compose.yml up -d

# Or use explicit production configuration
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## Accessing the Applications

### Development URLs
- **MCP Client Web UI**: <http://localhost:5000> or <https://localhost:5001>
- **MCP Server API**: <http://localhost:8080> or <https://localhost:8443>
- **Server Health Check**: <http://localhost:8080/health>
- **Server Metadata**: <http://localhost:8080/.well-known/oauth-protected-resource>

### Container Names
- **Server Container**: `amlink-mcp-server`
- **Client Container**: `amlink-mcp-client`

## Debugging Support

### VS Code Remote Debugging

1. **Start containers**: `docker-compose up -d`
2. **Attach debugger**:
   - Open Command Palette (`Ctrl+Shift+P`)
   - Type: "Docker: Attach Visual Studio Code"
   - Select container: `amlink-mcp-server` or `amlink-mcp-client`
3. **Set breakpoints** and debug your code

### Launch.json Configuration

The project includes `.vscode/launch.json` for debugging:

```json
{
    "configurations": [
        {
            "name": "Containers .NET Attach (Preview)",
            "type": "docker",
            "request": "attach",
            "platform": "netCore",
            "sourceFileMap": {
                "/src": "${workspaceFolder}"
            }
        }
    ]
}
```

**Usage:**
1. Start containers: `docker-compose up -d`
2. Press `F5` or use Run and Debug panel
3. Select "Containers .NET Attach (Preview)"
4. Choose the container to debug when prompted

**🔄 Multi-Container Debugging**: You can run this configuration **multiple times concurrently** to debug both server and client simultaneously. Each debug session will attach to a different container, allowing full-stack debugging across the entire MCP application.

For detailed debugging instructions, see [DEBUG.md](../DEBUG.md).

## Configuration

### Environment Variables

#### Required Environment Variables

Create a `.env` file in the project root:

```env
# Required for OAuth authentication
IDENTITY_SERVER_CLIENT_SECRET=your-actual-client-secret

# Required for OpenAI integration  
OPENAI_API_KEY=your-openai-api-key
```

#### Automatically Configured Variables

The docker-compose files automatically configure:

- `McpServer__Url`: Internal container communication URL
- `IdentityServer__RedirectUri`: OAuth callback URL matching client port
- `ASPNETCORE_ENVIRONMENT`: Development environment settings
- Hot reload and file watching options
- Debug-specific build configurations

### Development vs Production Configuration

| Feature | Development (Override) | Production (Base) |
|---------|------------------------|-------------------|
| **Build Type** | Debug with symbols | Release optimized |
| **Dockerfiles** | Dockerfile.debug | Dockerfile |
| **Hot Reload** | Enabled | Disabled |
| **Debugger** | VS Code integration | Not available |
| **Auto-restart** | Disabled (dev mode) | `unless-stopped` |
| **Default Secrets** | Safe dev defaults | Must be provided |
| **Volume Mapping** | Source code sync | Data only |

## Local Development (Without Docker)

If you prefer to run without Docker:

### Prerequisites
```bash
# Install .NET 10.0 SDK
winget install Microsoft.DotNet.SDK.Preview

# Generate HTTPS certificates
dotnet dev-certs https --trust
```

### Run Services
```bash
# Terminal 1: Run the MCP Server
cd src/amlink-submissions-mcp-server
dotnet run

# Terminal 2: Run the MCP Client  
cd src/amlink-submissions-mcp-client
dotnet run
```

**Local URLs:**
- Client: <https://localhost:7047> or <http://localhost:5119>
- Server: <https://localhost:7072> or <http://localhost:5194>

## Common Commands

```bash
# Development workflow
docker-compose up -d              # Start with debug support
docker-compose logs -f            # View live logs
docker-compose restart            # Restart after config changes
docker-compose up -d --build      # Rebuild after code changes
docker-compose down              # Stop all services

# Container management
docker ps                        # Check running containers
docker exec -it amlink-mcp-server /bin/bash  # Access server shell
docker exec -it amlink-mcp-client /bin/bash   # Access client shell

# Production deployment
docker-compose -f docker-compose.yml up -d    # Production mode
```

## Troubleshooting

### Port Conflicts
```bash
# Check what's using the ports
netstat -ano | findstr :5000
netstat -ano | findstr :8080

# Change ports in docker-compose.override.yml if needed
```

### Container Issues
```bash
# Check container status
docker ps -a

# View container logs
docker logs amlink-mcp-server
docker logs amlink-mcp-client

# Restart containers
docker-compose restart

# Force rebuild
docker-compose down && docker-compose up -d --build
```

### Environment Variable Issues
```bash
# Verify environment variables are loaded
docker exec amlink-mcp-server env | grep -i identity
docker exec amlink-mcp-client env | grep -i openai

# Check .env file format (no spaces around =)
Get-Content .env
```

### Debugging Issues
1. **Ensure VS Code extensions are installed**: C# Dev Kit, Docker
2. **Verify containers are running**: `docker ps`
3. **Check debugger volumes**: `docker inspect amlink-mcp-server | grep -A 5 Mounts`
4. **Restart containers**: `docker-compose restart`

### Hot Reload Not Working
1. **Check file watcher settings** in docker-compose.override.yml
2. **Verify source code mapping**: Files should sync between host and container
3. **File permissions**: Ensure Docker has access to project directory
4. **Force restart**: `docker-compose restart` if configuration changed
