FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MedicalCenter.Api/MedicalCenter.Api.csproj", "src/MedicalCenter.Api/"]
COPY ["src/MedicalCenter.Application/MedicalCenter.Application.csproj", "src/MedicalCenter.Application/"]
COPY ["src/MedicalCenter.Domain/MedicalCenter.Domain.csproj", "src/MedicalCenter.Domain/"]
COPY ["src/MedicalCenter.Infrastructure/MedicalCenter.Infrastructure.csproj", "src/MedicalCenter.Infrastructure/"]
COPY ["src/MedicalCenter.Contracts/MedicalCenter.Contracts.csproj", "src/MedicalCenter.Contracts/"]

RUN dotnet restore "src/MedicalCenter.Api/MedicalCenter.Api.csproj"

COPY . .
RUN dotnet publish "src/MedicalCenter.Api/MedicalCenter.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MedicalCenter.Api.dll"]
