# Multi-stage build for AcxiomCRM Enterprise
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project definitions
COPY AcxiomCRM.sln .
COPY src/AcxiomCRM.Domain/AcxiomCRM.Domain.csproj src/AcxiomCRM.Domain/
COPY src/AcxiomCRM.Infrastructure/AcxiomCRM.Infrastructure.csproj src/AcxiomCRM.Infrastructure/
COPY src/AcxiomCRM.Application/AcxiomCRM.Application.csproj src/AcxiomCRM.Application/
COPY src/AcxiomCRM.Web/AcxiomCRM.Web.csproj src/AcxiomCRM.Web/
COPY tests/AcxiomCRM.Tests/AcxiomCRM.Tests.csproj tests/AcxiomCRM.Tests/

# Restore dependencies
RUN dotnet restore AcxiomCRM.sln

# Copy remaining source code
COPY . .

# Run automated tests during CI/CD build
RUN dotnet test tests/AcxiomCRM.Tests/AcxiomCRM.Tests.csproj --configuration Release --no-restore

# Publish Web Application
RUN dotnet publish src/AcxiomCRM.Web/AcxiomCRM.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose ports
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AcxiomCRM.Web.dll"]
