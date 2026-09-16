FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FreeboxMonitor.Web/FreeboxMonitor.Web.csproj FreeboxMonitor.Web/
RUN dotnet restore FreeboxMonitor.Web/FreeboxMonitor.Web.csproj

COPY FreeboxMonitor.Web/ FreeboxMonitor.Web/
RUN dotnet publish FreeboxMonitor.Web/FreeboxMonitor.Web.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "FreeboxMonitor.Web.dll"]
