# Этот этап используется при запуске из VS в быстром режиме
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# 1. COPY правильный для контекста корня
COPY ./EasyLink/EasyLink.csproj ./EasyLink/
# 2. ИСПРАВЛЕНИЕ: Указываем путь к файлу, так как он лежит в подпапке EasyLink
RUN dotnet restore "./EasyLink/EasyLink.csproj"
COPY . .
# Сборка ссылается на правильный путь (это было верно)
RUN dotnet build "./EasyLink/EasyLink.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EasyLink/EasyLink.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# 3. ИСПРАВЛЕНИЕ: Путь к DLL должен быть относительно рабочей директории /app
ENTRYPOINT ["dotnet", "EasyLink.dll"]