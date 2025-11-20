FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["amlink-submissions-mcp-client.csproj", "amlink-submissions-mcp-client/"]
RUN dotnet restore "amlink-submissions-mcp-client/amlink-submissions-mcp-client.csproj"
COPY . amlink-submissions-mcp-client/
WORKDIR "/src/amlink-submissions-mcp-client"
RUN dotnet build "amlink-submissions-mcp-client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "amlink-submissions-mcp-client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create directory for data protection keys and set permissions
RUN mkdir -p /tmp/dp-keys && chmod 755 /tmp/dp-keys

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=publish /app/publish .

# Ensure static files are properly accessible
RUN chmod -R 755 /app/wwwroot || true

ENTRYPOINT ["dotnet", "amlink-submissions-mcp-client.dll"]