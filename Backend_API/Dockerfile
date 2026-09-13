# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Backend_API/Backend_API.csproj Backend_API/
COPY PROG7312_POE.Shared/PROG7312_POE.Shared.csproj PROG7312_POE.Shared/
RUN dotnet restore Backend_API/Backend_API.csproj

COPY Backend_API/ Backend_API/
COPY PROG7312_POE.Shared/ PROG7312_POE.Shared/
RUN dotnet publish Backend_API/Backend_API.csproj -c Release -o /app/publish

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Backend_API.dll"]