# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

WORKDIR /src/DubiRent-Asp.net/DubiRent/DubiRent

RUN dotnet restore DubiRent.csproj
RUN dotnet publish DubiRent.csproj -c Release -o /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "DubiRent.dll"]
