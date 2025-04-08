# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["ConsultantPlatform/ConsultantPlatform.csproj", "ConsultantPlatform/"]
RUN dotnet restore "ConsultantPlatform/ConsultantPlatform.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/ConsultantPlatform"

# Build the application
RUN dotnet build "ConsultantPlatform.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ConsultantPlatform.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80

ENTRYPOINT ["dotnet", "ConsultantPlatform.dll"]
