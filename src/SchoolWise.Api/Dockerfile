FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/SchoolWise.Domain/SchoolWise.Domain.csproj src/SchoolWise.Domain/
COPY src/SchoolWise.Application/SchoolWise.Application.csproj src/SchoolWise.Application/
COPY src/SchoolWise.Infrastructure/SchoolWise.Infrastructure.csproj src/SchoolWise.Infrastructure/
COPY src/SchoolWise.Api/SchoolWise.Api.csproj src/SchoolWise.Api/
RUN dotnet restore src/SchoolWise.Api/SchoolWise.Api.csproj

COPY src/ src/
RUN dotnet publish src/SchoolWise.Api/SchoolWise.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
COPY --from=build --chown=app:app /app/publish .
USER app
HEALTHCHECK --interval=10s --timeout=3s --start-period=30s --retries=12 \
    CMD curl --fail --silent http://localhost:10000/ready || exit 1
ENTRYPOINT ["dotnet", "SchoolWise.Api.dll"]
