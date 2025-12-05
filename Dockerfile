# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the csproj from the actual folder
COPY DubiRent-Asp.net/DubiRent/DubiRent.csproj DubiRent.csproj

# Restore
RUN dotnet restore DubiRent.csproj

# Copy the entire project
COPY DubiRent-Asp.net/DubiRent/ .

# Build
RUN dotnet publish DubiRent.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DubiRent.dll"]
