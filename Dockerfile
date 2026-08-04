# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только файлы проектов: слой с restore переживёт правку кода.
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/CockRealSizeBot.Bot/CockRealSizeBot.Bot.csproj src/CockRealSizeBot.Bot/
RUN dotnet restore src/CockRealSizeBot.Bot/CockRealSizeBot.Bot.csproj

COPY src/ src/
RUN dotnet publish src/CockRealSizeBot.Bot/CockRealSizeBot.Bot.csproj \
    -c Release \
    --no-restore \
    -o /app


# ВНИМАНИЕ при смене базового образа: граница суток считается по IANA-зоне
# (Europe/Moscow), а InvariantGlobalization выключен — образу нужны ICU и tzdata.
# В этом (Ubuntu) они есть из коробки, в chiseled и alpine — нет, там
# FindSystemTimeZoneById упадёт на старте.
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

WORKDIR /app
COPY --from=build /app .

# Базовый образ .NET уже содержит непривилегированного пользователя app.
USER $APP_UID

ENTRYPOINT ["dotnet", "CockRealSizeBot.Bot.dll"]
