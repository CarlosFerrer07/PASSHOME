#FASE 1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PasswordManager.Core/PasswordManager.Core.csproj PasswordManager.Core/
COPY PasswordManager.DTOs/PasswordManager.DTOs.csproj PasswordManager.DTOs/
COPY PasswordManager.Infrastructure/PasswordManager.Infrastructure.csproj PasswordManager.Infrastructure/
COPY PasswordManager.Api/PasswordManager.Api.csproj PasswordManager.Api/

RUN dotnet restore PasswordManager.Api/PasswordManager.Api.csproj

COPY . .
RUN dotnet publish PasswordManager.Api/PasswordManager.Api.csproj -c Release -o /app/publish

#FASE 2
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "PasswordManager.Api.dll"]