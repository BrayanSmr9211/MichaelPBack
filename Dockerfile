FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY GestionTareas.sln ./
COPY src/GestionTareas.Domain/GestionTareas.Domain.csproj         src/GestionTareas.Domain/
COPY src/GestionTareas.Application/GestionTareas.Application.csproj src/GestionTareas.Application/
COPY src/GestionTareas.Infrastructure/GestionTareas.Infrastructure.csproj src/GestionTareas.Infrastructure/
COPY src/GestionTareas.API/GestionTareas.API.csproj               src/GestionTareas.API/
RUN dotnet restore

COPY . .
RUN dotnet publish src/GestionTareas.API/GestionTareas.API.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "GestionTareas.API.dll"]
