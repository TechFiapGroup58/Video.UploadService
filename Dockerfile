FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/UploadService.Core/UploadService.Core.csproj",                     "src/UploadService.Core/"]
COPY ["src/UploadService.Infrastructure/UploadService.Infrastructure.csproj", "src/UploadService.Infrastructure/"]
COPY ["src/UploadService.API/UploadService.API.csproj",                       "src/UploadService.API/"]
RUN dotnet restore "src/UploadService.API/UploadService.API.csproj"

COPY . .
RUN dotnet publish "src/UploadService.API/UploadService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && addgroup --system appgroup \
    && adduser --system --ingroup appgroup appuser

USER appuser
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "UploadService.API.dll"]
