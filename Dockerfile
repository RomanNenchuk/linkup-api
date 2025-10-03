# ----------------------
# 1. Build stage
# ----------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# копіюємо sln і всі проекти
COPY *.sln .
COPY src/ ./src/

# відновлення залежностей
RUN dotnet restore ./src/Web/Web.csproj

# збірка та публікація
RUN dotnet publish ./src/Web/Web.csproj -c Release -o /app/publish

# ----------------------
# 2. Runtime stage
# ----------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# копіюємо результати збірки
COPY --from=build /app/publish .

# задаємо точку входу
ENTRYPOINT ["dotnet", "Web.dll"]
