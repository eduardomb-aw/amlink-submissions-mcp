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
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "amlink-submissions-mcp-client.dll"]