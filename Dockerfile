# 1. Basis-Image für .NET Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 443 5001

ENV CONTAINER_BUILD=true

# Installiere ping im Basis-Image
RUN apt-get update && apt-get install -y iputils-ping
RUN apt-get install -y vim
RUN apt-get install -y nmap
RUN apt-get install -y sqlite3 libsqlite3-dev
RUN apt-get install -y \
    libfontconfig1 \
    libfreetype6 \
    libpng16-16 \
    libjpeg62-turbo \
    libglib2.0-0 \
    libx11-6 \
    libxext6 \
    libxrender1

RUN apt-get install -y curl wget net-tools iproute2 nmap tcpdump file strace lsof htop procps

RUN echo "alias l='ls -al --color'" >> /etc/bash.bashrc
RUN echo "alias ls='ls -a --color'" >> /etc/bash.bashrc

# 2. Build-Umgebung mit .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 3. Code kopieren & Bereinigung von obj/ und bin/
COPY . .
RUN find . -type d \( -name "obj" -o -name "bin" \) -exec rm -rf {} +

# 3a. Restore (NuGet-Pakete wiederherstellen)
RUN dotnet restore "WebApp/railyWebApp/railyWebApp.csproj"
RUN dotnet restore "WebApp/railyWebIndex/railyWebIndex.csproj"

# 4. Projekte als Release bauen
RUN dotnet publish "WebApp/railyWebApp/railyWebApp.csproj" -c Release -o /app/publish/railyWebApp
RUN dotnet publish "WebApp/railyWebIndex/railyWebIndex.csproj" -c Release -o /app/publish/railyWebIndex

# 5. Laufzeit-Image erstellen
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish/ .

# 6. Standardmäßig die railyWebIndex starten, kann überschrieben werden
# WORKDIR /app/
# ENTRYPOINT ["dotnet", "railyWebIndex.dll"]
