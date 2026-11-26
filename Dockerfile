FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY EventTicketing.Core/EventTicketing.Core.csproj EventTicketing.Core/
COPY EventTicketing.Data/EventTicketing.Data.csproj EventTicketing.Data/
COPY EventTicketing.Service/EventTicketing.Service.csproj EventTicketing.Service/
COPY EventTicketing.API/EventTicketing.API.csproj EventTicketing.API/
RUN dotnet restore EventTicketing.API/EventTicketing.API.csproj
COPY . .
RUN dotnet publish EventTicketing.API/EventTicketing.API.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
RUN mkdir /app/logs && chown app:app /app/logs
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
USER app
ENTRYPOINT ["dotnet", "EventTicketing.API.dll"]
