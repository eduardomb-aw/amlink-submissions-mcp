# AmLink Submissions MCP - Docker Setup

This project contains both an MCP Server and MCP Client that can be run using Docker and Docker Compose.

## Prerequisites

- Docker Desktop installed and running
- .NET 10.0 SDK (for local development)

## Services

- **amlink-mcp-server**: The MCP server running on port 7072
- **amlink-mcp-client**: The web client application running on port 5000

## Running with Docker Compose

### Development Mode
```bash
# Build and run both services
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build

# Run in detached mode
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Production Mode
```bash
# Build and run both services
docker-compose up --build

# Run in detached mode
docker-compose up --build -d
```

## Accessing the Applications

- **MCP Client Web UI**: http://localhost:5000
- **MCP Server**: http://localhost:7072

## Configuration

### Environment Variables

The following environment variables are configured in docker-compose files:

- `McpServer__Url`: URL for the MCP server (automatically set to internal Docker network URL)
- `IdentityServer__RedirectUri`: OAuth redirect URI (set to match the client port)
- `ASPNETCORE_ENVIRONMENT`: Set to Development for local development

### Secrets

For production use, ensure you have the proper Identity Server client secret configured. You can:

1. Set it as an environment variable in docker-compose:
   ```yaml
   environment:
     - IdentityServer__ClientSecret=your-actual-secret
   ```

2. Use Docker secrets (recommended for production)

3. Mount a configuration file with the secrets

## Development

For local development without Docker:

1. Run the MCP Server:
   ```bash
   cd src/amlink-submissions-mcp-server
   dotnet run
   ```

2. Run the MCP Client:
   ```bash
   cd src/amlink-submissions-mcp-client
   dotnet run
   ```

## Troubleshooting

- If you get port conflicts, change the ports in docker-compose.yml
- Ensure Docker Desktop is running before executing docker-compose commands
- Check logs with `docker-compose logs [service-name]` for specific service issues