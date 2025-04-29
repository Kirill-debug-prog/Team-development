# ���������� SDK ����� ��� ������
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# �������� ���� ������� � ��������������� �����������
COPY ["ConsultantPlatform.csproj", "."]
RUN dotnet restore "./ConsultantPlatform.csproj"

# �������� ���� ��������� ��� �������
COPY . .


# dotnet publish �������� build, ���� �� ������ --no-build.
# ��������� ��������� � ����� /app/publish (�� ���� ����� build)
RUN dotnet publish "ConsultantPlatform.csproj" -c Release -o /app/publish


# ���������� Runtime ����� ��� �������
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# �������� �������������� ����� �� ����� /app/publish � ����� build
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80

# ��������� ����, ������� ������� ���������
EXPOSE 80

# ����� ����� � ����������
ENTRYPOINT ["dotnet", "ConsultantPlatform.dll"]