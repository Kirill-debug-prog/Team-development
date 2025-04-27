FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ConsultantPlatform.csproj", "."]
RUN dotnet restore "./ConsultantPlatform.csproj"

COPY . .

RUN dotnet build "ConsultantPlatform.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "ConsultantPlatform.csproj" -c Release -o /app/publish --no-restore --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

# !!! ИЗМЕНЕНО: Kestrel будет слушать на порту 80, чтобы соответствовать docker-compose.yaml и nginx.conf !!!
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "ConsultantPlatform.dll"]