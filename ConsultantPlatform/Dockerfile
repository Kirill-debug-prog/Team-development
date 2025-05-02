
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ConsultantPlatform.csproj", "."]
RUN dotnet restore "./ConsultantPlatform.csproj"

COPY . .

RUN dotnet publish "ConsultantPlatform.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80

ENTRYPOINT ["dotnet", "ConsultantPlatform.dll"]