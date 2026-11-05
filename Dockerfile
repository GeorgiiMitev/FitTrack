FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["FitTrack/FitTrack/FitTrack.csproj", "FitTrack/FitTrack/"]
RUN dotnet restore "FitTrack/FitTrack/FitTrack.csproj"

COPY . .
WORKDIR /src/FitTrack/FitTrack
RUN dotnet publish "FitTrack.csproj" --configuration Release --output /app/publish /p:UseAppHost=false \
    && rm -f /app/publish/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "FitTrack.dll"]