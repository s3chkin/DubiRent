# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy everything
COPY . .

# Go into project folder
WORKDIR /src/DubiRent

# Restore dependencies
RUN dotnet restore

# Build project
RUN dotnet publish -c Release -o /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

# Expose default ASP.NET port
EXPOSE 8080

# Run the app
ENTRYPOINT ["dotnet", "DubiRent.dll"]
