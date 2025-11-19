FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["amlink-submissions-mcp-server.csproj", "amlink-submissions-mcp-server/"]
RUN dotnet restore "amlink-submissions-mcp-server/amlink-submissions-mcp-server.csproj"
COPY . amlink-submissions-mcp-server/
WORKDIR "/src/amlink-submissions-mcp-server"
RUN dotnet build "amlink-submissions-mcp-server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "amlink-submissions-mcp-server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "amlink-submissions-mcp-server.dll"]