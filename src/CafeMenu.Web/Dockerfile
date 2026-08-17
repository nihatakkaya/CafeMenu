FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
RUN mkdir -p /var/cafemenu/data-protection \
    && chown -R $APP_UID:0 /var/cafemenu

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/CafeMenu.Web/CafeMenu.Web.csproj", "src/CafeMenu.Web/"]
RUN dotnet restore "src/CafeMenu.Web/CafeMenu.Web.csproj"
COPY . .
RUN dotnet publish "src/CafeMenu.Web/CafeMenu.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "CafeMenu.Web.dll"]
