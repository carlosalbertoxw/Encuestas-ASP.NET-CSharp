# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar manifiestos primero para aprovechar la caché de restauración.
COPY Directory.Build.props Encuestas.slnx ./
COPY src/Encuestas.Model/Encuestas.Model.csproj src/Encuestas.Model/
COPY src/Encuestas.Data/Encuestas.Data.csproj src/Encuestas.Data/
COPY src/Encuestas.Web/Encuestas.Web.csproj src/Encuestas.Web/
RUN dotnet restore src/Encuestas.Web/Encuestas.Web.csproj

# Copiar el resto y publicar.
COPY . .
RUN dotnet publish src/Encuestas.Web/Encuestas.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa de ejecución (imagen mínima, usuario no root)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Encuestas.Web.dll"]
