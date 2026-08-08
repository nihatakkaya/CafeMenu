FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CafeMenu.Api/CafeMenu.Api.csproj", "src/CafeMenu.Api/"]
RUN dotnet restore "src/CafeMenu.Api/CafeMenu.Api.csproj"
COPY . .
RUN dotnet publish "src/CafeMenu.Api/CafeMenu.Api.csproj" -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CafeMenu.Api.dll"]
