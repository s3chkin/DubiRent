# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy everything from repository
COPY . .

# Go to the correct project folder
WORKDIR /src/DubiRent-Asp.net/DubiRent

# Restore dependencies
RUN dotnet restore DubiRent.csproj

# Publish the application
RUN dotnet publish DubiRent.csproj -c Release -o /app/publish


# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "DubiRent.dll"]
