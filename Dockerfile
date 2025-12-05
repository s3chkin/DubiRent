# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy everything
COPY . .

# Go to folder where the .csproj is located
WORKDIR /src/DubiRent-Asp.net/DubiRent/DubiRent

# Restore dependencies
RUN dotnet restore

# Publish app
RUN dotnet publish -c Release -o /app/publish


# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "DubiRent.dll"]
