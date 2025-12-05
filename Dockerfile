# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy everything from repository
COPY . .

# Go to the correct project folder
WORKDIR /src/DubiRent-Asp.net/DubiRent/DubiRent

# Restore dependencies
RUN dotnet restore DubiRent.csproj

# Publish the application
RUN dotnet publish DubiRent.csproj -c Release -o /app/publish


# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

# IMPORTANT: Render uses a dynamic port, so ASP.NET MUST listen on $PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT

ENTRYPOINT ["dotnet", "DubiRent.dll"]
