FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файл проекта (.csproj) и восстанавливаем зависимости отдельно
# Это позволяет Docker кэшировать слой зависимостей, если код меняется, а зависимости нет
COPY ["ConsultantPlatform.csproj", "."]
RUN dotnet restore "./ConsultantPlatform.csproj"

# Копируем остальной исходный код проекта
COPY . .

# Собираем проект в режиме Release
# Вывод будет в /app/build
RUN dotnet build "ConsultantPlatform.csproj" -c Release -o /app/build --no-restore

# --- Этап 2: Публикация ---
# Создаем оптимизированную версию приложения для развертывания
FROM build AS publish
RUN dotnet publish "ConsultantPlatform.csproj" -c Release -o /app/publish --no-restore --no-build

# --- Этап 3: Финальный образ ---
# Используем образ ASP.NET Core Runtime (он меньше, чем SDK)
# Версия должна совпадать с версией SDK
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Копируем опубликованное приложение из этапа 'publish'
COPY --from=publish /app/publish .

# Указываем порт, который будет слушать приложение ВНУТРИ контейнера
# Kestrel по умолчанию слушает 8080 в контейнерах .NET 6+
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Указываем команду для запуска приложения при старте контейнера
# Замени 'ConsultantPlatform.dll' на имя твоей основной DLL-ки
ENTRYPOINT ["dotnet", "ConsultantPlatform.dll"]