FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Scholaris.sln .
COPY src/ ./src/
RUN dotnet restore src/Scholaris.Api/Scholaris.Api.csproj
RUN dotnet publish src/Scholaris.Api/Scholaris.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Scholaris.Api.dll"]
