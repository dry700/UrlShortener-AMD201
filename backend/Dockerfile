FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY UrlShortener.sln ./
COPY UrlShortener.API/UrlShortener.API.csproj UrlShortener.API/
COPY UrlShortener.Tests/UrlShortener.Tests.csproj UrlShortener.Tests/

# Restore downloads all packages into the Linux container's cache
RUN dotnet restore UrlShortener.sln

# Copy all source code
COPY . .

# Run tests (uses the packages already restored above)
RUN dotnet test UrlShortener.Tests/UrlShortener.Tests.csproj --no-restore --verbosity normal

# Publish (uses the packages already restored above)
RUN dotnet publish UrlShortener.API/UrlShortener.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "UrlShortener.API.dll"]