# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar manifiestos y lockfiles primero para aprovechar la caché de restauración.
COPY Directory.Build.props Encuestas.slnx ./
COPY src/Encuestas.Model/Encuestas.Model.csproj src/Encuestas.Model/packages.lock.json src/Encuestas.Model/
COPY src/Encuestas.Data/Encuestas.Data.csproj src/Encuestas.Data/packages.lock.json src/Encuestas.Data/
COPY src/Encuestas.Web/Encuestas.Web.csproj src/Encuestas.Web/packages.lock.json src/Encuestas.Web/
RUN dotnet restore src/Encuestas.Web/Encuestas.Web.csproj --locked-mode

# Copiar el resto y publicar.
COPY . .
RUN dotnet publish src/Encuestas.Web/Encuestas.Web.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore \
    && mkdir -p /keys

# Etapa de ejecución: imagen chiseled (sin shell ni gestor de paquetes, usuario no root).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .
# Directorio de las claves de Data Protection (se monta como volumen en docker-compose).
COPY --from=build --chown=$APP_UID:$APP_UID /keys /keys
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DataProtection__KeysPath=/keys
EXPOSE 8080
# Sin curl en la imagen: la propia app consulta /health/live.
HEALTHCHECK --interval=15s --timeout=5s --start-period=30s --retries=3 \
    CMD ["dotnet", "Encuestas.Web.dll", "healthcheck"]
ENTRYPOINT ["dotnet", "Encuestas.Web.dll"]
