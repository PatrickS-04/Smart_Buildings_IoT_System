# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY PROG7312_POE/PROG7312_POE.csproj PROG7312_POE/
COPY PROG7312_POE.Shared/PROG7312_POE.Shared.csproj PROG7312_POE.Shared/
RUN dotnet restore PROG7312_POE/PROG7312_POE.csproj

COPY PROG7312_POE/ PROG7312_POE/
COPY PROG7312_POE.Shared/ PROG7312_POE.Shared/
RUN dotnet publish PROG7312_POE/PROG7312_POE.csproj -c Release -o /app/publish

# --- Runtime Stage ---
FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY PROG7312_POE/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80