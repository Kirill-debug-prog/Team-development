# Используем SDK образ для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["ConsultantPlatform.csproj", "."]
RUN dotnet restore "./ConsultantPlatform.csproj"

# Копируем ВЕСЬ остальной код проекта
COPY . .

# dotnet publish включает build, если не указан --no-build.
# Сохраняем результат в папку /app/publish (НА ЭТОМ ЭТАПЕ build)
RUN dotnet publish "ConsultantPlatform.csproj" -c Release -o /app/publish

# Используем Runtime образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Копируем ОПУБЛИКОВАННЫЕ файлы из папки /app/publish с ЭТАПА build
COPY --from=build /app/publish .

# Определяем порт, на котором слушает ASP.NET приложение (по умолчанию 80)
ENV ASPNETCORE_URLS=http://+:80

# Объявляем порт, который слушает контейнер
EXPOSE 80

# Точка входа в приложение
ENTRYPOINT ["dotnet", "ConsultantPlatform.dll"]