# AmLink MCP Debugging Guide

## Overview

This project supports seamless debugging of both MCP Server and MCP Client using VS Code's native Docker debugging capabilities. The debug configuration automatically uses development Dockerfiles with debug symbols and hot reload support.

## Docker Compose Configuration

### Automatic Development/Debug Mode

The project uses `docker-compose.override.yml` which is **automatically loaded** by Docker Compose in development:

```bash
# This automatically loads both docker-compose.yml + docker-compose.override.yml
# Uses debug Dockerfiles with hot reload and debugging enabled
docker-compose up -d
```

### Production Mode (No Debugging)

```bash
# Explicitly skip the override file for production builds
docker-compose -f docker-compose.yml up -d

# Or use the production configuration
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## VS Code Remote Debugging Setup

### Prerequisites

- **VS Code Extensions**:
  - C# Dev Kit (ms-dotnettools.csdevkit)
  - Docker (ms-azuretools.vscode-docker)
- **Docker Desktop** running
- **Environment Variables** (see Environment Variables section below)

### Quick Debug Workflow

1. **Start containers in debug mode** (uses override automatically):
   ```bash
   docker-compose up -d
   ```

2. **Verify containers are running**:
   ```bash
   docker ps
   # Should show: amlink-mcp-server and amlink-mcp-client
   ```

3. **Attach debugger in VS Code**:
   - Open Command Palette (`Ctrl+Shift+P`)
   - Type: "Docker: Attach Visual Studio Code"
   - Select the container you want to debug:
     - `amlink-mcp-server` for server debugging
     - `amlink-mcp-client` for client debugging
   - VS Code will open a new window attached to the container

4. **Set breakpoints** in your C# code

5. **Trigger the code path** (make API calls, navigate web pages, etc.)

6. **Debug!** Your breakpoints will hit and you can inspect variables, step through code, etc.

### Alternative: Launch.json Configuration

The project includes a `.vscode/launch.json` with Docker attach configuration:

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
1. **Start containers**: `docker-compose up -d`
2. **Open Run and Debug panel** (`Ctrl+Shift+D`)
3. **Select**: "Containers .NET Attach (Preview)"
4. **Press F5** and choose the container to debug

**💡 Concurrent Debugging**: This configuration can be run in **multiple concurrent instances**, allowing you to debug both the MCP Server and MCP Client simultaneously. Simply:
- Start the first debug session and attach to `amlink-mcp-server`
- Start a second debug session and attach to `amlink-mcp-client`
- Both debuggers will run independently, letting you set breakpoints and debug across both containers at the same time

## Debug Features

### Hot Reload Support

- **Source code changes** are automatically detected and reloaded
- **File watcher** enabled with `DOTNET_USE_POLLING_FILE_WATCHER=true`
- **Automatic restarts** on breaking changes with `DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true`

### Volume Mappings

The debug configuration includes volume mappings for:
- **Source code**: Live sync between host and container
- **VS Code debugger**: `~/.vsdbg:/remote_debugger:rw`
- **NuGet cache**: `~/.nuget/packages:/root/.nuget/packages:ro`
- **HTTPS certificates**: `~/.aspnet/https:/https:ro`

### Debug-Specific Environment

- **Configuration**: Debug builds with symbols
- **Environment**: `ASPNETCORE_ENVIRONMENT=Development`
- **Auto-restart**: Disabled (`restart: "no"`) for development
- **Default secrets**: Development-safe defaults for required environment variables

## Container Information

### Service Names and Ports

| Service | Container Name | HTTP Port | HTTPS Port | Purpose |
|---------|----------------|-----------|------------|----------|
| Server | amlink-mcp-server | 8080 | 8443 | MCP Server API |
| Client | amlink-mcp-client | 5000 | 5001 | Web Client UI |

### Application URLs

- **Client Web UI**: <http://localhost:5000> or <https://localhost:5001>
- **Server API**: <http://localhost:8080> or <https://localhost:8443>
- **Server Metadata**: <http://localhost:8080/.well-known/oauth-protected-resource>
- **Health Check**: <http://localhost:8080/health>

## Environment Variables

### Required Variables

Create a `.env` file in the project root or set environment variables:

```env
# Required for OAuth authentication
IDENTITY_SERVER_CLIENT_SECRET=your-actual-client-secret

# Required for OpenAI integration
OPENAI_API_KEY=your-openai-api-key
```

### Development Defaults

The debug configuration provides safe defaults for development:
- **CLIENT_SECRET**: Development placeholder (replace for real testing)
- **OPENAI_API_KEY**: Dummy value (replace for LLM features)

## Common Debugging Commands

```bash
# Start debug environment (automatic override loading)
docker-compose up -d

# View real-time logs
docker-compose logs -f
docker-compose logs -f amlink-mcp-server  # Server only
docker-compose logs -f amlink-mcp-client   # Client only

# Restart after config changes
docker-compose restart

# Rebuild containers with code changes
docker-compose up -d --build

# Stop debug environment
docker-compose down

# Check container status
docker ps

# Access container shell for debugging
docker exec -it amlink-mcp-server /bin/bash
docker exec -it amlink-mcp-client /bin/bash
```

## Production vs Development

| Aspect | Development (Override) | Production (Base Only) |
|--------|------------------------|------------------------|
| **Dockerfiles** | Dockerfile.debug | Dockerfile |
| **Build Config** | Debug with symbols | Release optimized |
| **Hot Reload** | Enabled | Disabled |
| **Auto-restart** | Disabled | `unless-stopped` |
| **Volume Mapping** | Source code sync | Data only |
| **Default Secrets** | Development safe | Required from environment |
| **Debugger Support** | Full VS Code integration | None |

## Troubleshooting

### Debugger Won't Attach

1. **Verify containers are running**: `docker ps`
2. **Check VS Code extensions**: Ensure C# Dev Kit and Docker extensions are installed
3. **Restart containers**: `docker-compose restart`
4. **Check logs**: `docker-compose logs` for any startup errors

### Port Conflicts

```bash
# Check what's using ports
netstat -ano | findstr :5000
netstat -ano | findstr :8080

# Kill conflicting processes or change ports in docker-compose.override.yml
```

### Environment Variable Issues

```bash
# Verify environment variables are loaded
docker exec amlink-mcp-server env | grep -i identity
docker exec amlink-mcp-server env | grep -i openai

# Check if .env file is properly formatted (no spaces around =)
cat .env
```

### Hot Reload Not Working

1. **Verify file watcher**: Check that `DOTNET_USE_POLLING_FILE_WATCHER=true` is set
2. **Check volume mounts**: Source code should be mapped to container
3. **File permissions**: Ensure Docker has access to project files
4. **Restart containers**: `docker-compose restart` if settings changed

### Code Changes Not Reflecting

```bash
# Force rebuild with latest code
docker-compose down
docker-compose up -d --build

# Check if volumes are properly mounted
docker inspect amlink-mcp-server | grep -A 10 Mounts
```