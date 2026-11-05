FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish src/GeoTag.Bot/GeoTag.Bot.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/runtime:11.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "GeoTag.Bot.dll"]