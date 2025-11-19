# AmLink MCP Debugging Guide

## Docker Compose Files

### 1. **Production Configuration**
```bash
# Start production containers
docker-compose up -d

# Uses production Dockerfiles
# No debugging capabilities
# Optimized for performance
```

### 2. **Debug Configuration**
```bash
# Start debug containers with vsdbg debugger
docker-compose -f docker-compose.debug.yml up -d

# Includes vsdbg debugger for VS Code remote debugging
# Uses debug Dockerfiles
# Container names: amlink-mcp-client, amlink-mcp-server
```

## VS Code Remote Debugging

### Setup Steps:
1. **Start debug containers**:
   ```bash
   docker-compose -f docker-compose.debug.yml up -d
   ```

2. **Set breakpoints** in your C# code (Program.cs, Index.cshtml.cs, etc.)

3. **Start debugging** in VS Code:
   - Press `F5` or go to Run and Debug panel
   - Select "Debug Client Container" or "Debug Server Container"
   - Choose the dotnet process (PID 1) when prompted

4. **Debug away!** Your breakpoints should hit when the code executes

### Available Debug Configurations:
- **Debug Client Container**: Attach to amlink-mcp-client
- **Debug Server Container**: Attach to amlink-mcp-server  
- **Debug Both (Client + Server)**: Debug both simultaneously

## Quick Commands

```bash
# Production
docker-compose up -d
docker-compose down

# Debug  
docker-compose -f docker-compose.debug.yml up -d
docker-compose -f docker-compose.debug.yml down

# View logs
docker logs amlink-mcp-client
docker logs amlink-mcp-server

# Check container status
docker ps
```

## Environment Variables
Set these in your environment or `.env` file:
```
IDENTITY_SERVER_CLIENT_SECRET=your-secret-here
OPENAI_API_KEY=your-openai-key-here
```

## Application URLs
- **Client**: http://localhost:5000
- **Server**: http://localhost:7072
- **Server Metadata**: http://localhost:7072/.well-known/oauth-protected-resource

## VS Code Extensions Needed
- C# Dev Kit
- Docker