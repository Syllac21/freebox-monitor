FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FreeboxMonitor.Worker/FreeboxMonitor.Worker.csproj FreeboxMonitor.Worker/
RUN dotnet restore FreeboxMonitor.Worker/FreeboxMonitor.Worker.csproj

COPY FreeboxMonitor.Worker/ FreeboxMonitor.Worker/
RUN dotnet publish FreeboxMonitor.Worker/FreeboxMonitor.Worker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && update-ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "FreeboxMonitor.Worker.dll"]
