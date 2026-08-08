FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CafeMenu.Web/CafeMenu.Web.csproj", "src/CafeMenu.Web/"]
RUN dotnet restore "src/CafeMenu.Web/CafeMenu.Web.csproj"
COPY . .
RUN dotnet publish "src/CafeMenu.Web/CafeMenu.Web.csproj" -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CafeMenu.Web.dll"]
