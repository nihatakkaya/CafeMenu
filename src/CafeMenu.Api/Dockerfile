FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
RUN mkdir -p /var/cafemenu/media \
    && chown -R $APP_UID:0 /var/cafemenu

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/CafeMenu.Api/CafeMenu.Api.csproj", "src/CafeMenu.Api/"]
RUN dotnet restore "src/CafeMenu.Api/CafeMenu.Api.csproj"
COPY . .
RUN dotnet publish "src/CafeMenu.Api/CafeMenu.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "CafeMenu.Api.dll"]
