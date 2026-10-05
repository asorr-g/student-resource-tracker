FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY StudentResourceTracker.csproj ./
RUN dotnet restore StudentResourceTracker.csproj
COPY . .
RUN dotnet publish StudentResourceTracker.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./
# The runtime image runs as the non-root "app" user, which needs a writable folder for the data file.
RUN mkdir -p /app/Data && chown app:app /app/Data
USER app
ENV DataFile=/app/Data/tracker.json \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "StudentResourceTracker.dll"]
