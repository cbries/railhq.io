# 📦 Installation Guide

This guide provides detailed instructions for installing and running railhq.io.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Option 1: Docker (Recommended)](#option-1-docker-recommended)
- [Option 2: Manual Installation](#option-2-manual-installation)
- [Gateway Setup](#gateway-setup)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### For Docker Installation (Recommended)

- **Docker** 20.10+ ([Install Docker](https://docs.docker.com/get-docker/))
- **Docker Compose** v2.0+ ([Install Docker Compose](https://docs.docker.com/compose/install/))
- **Git** ([Install Git](https://git-scm.com/downloads))
- **4 GB RAM** minimum (8 GB recommended)
- **2 GB disk space** for Docker images

### For Manual Installation

- **.NET 8.0 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Node.js 18+** (for documentation build, optional)
- **Redis** ([Install Redis](https://redis.io/docs/getting-started/installation/))
- **Git**

---

## Option 1: Docker (Recommended)

Docker is the easiest way to get started. It includes all dependencies and works on Linux, macOS, and Windows.

### Step 1: Clone the Repository

```bash
git clone https://github.com/cbries/railhq.io.git
cd railhq.io
```

### Step 2: Configure Environment

```bash
# Copy the example environment file
cp .env.example .env

# Edit the configuration (optional)
nano .env  # or use your preferred editor
```

**Key settings in `.env`:**

```bash
# Base directory for all data
RAILHQ_DATA_DIR=~/railhq.io

# Local/LAN mode (recommended for home use)
RAILHQ_LOCAL_MODE=true

# Disable HTTPS for local network
RAILHQ_USE_TLS=false
```

### Step 3: Create Data Directories

```bash
# Linux/macOS
mkdir -p ~/railhq.io/{resources,resourcesSetups,resourcesRedis,resourcesRuntime/nginx,resourcesRuntime/certificates,deployment}

# Copy nginx configuration
cp resourcesRuntime/nginx/*.conf ~/railhq.io/resourcesRuntime/nginx/
```

**Windows (PowerShell):**
```powershell
$dataDir = "$env:USERPROFILE\railhq.io"
New-Item -ItemType Directory -Force -Path "$dataDir\resources"
New-Item -ItemType Directory -Force -Path "$dataDir\resourcesSetups"
New-Item -ItemType Directory -Force -Path "$dataDir\resourcesRedis"
New-Item -ItemType Directory -Force -Path "$dataDir\resourcesRuntime\nginx"
New-Item -ItemType Directory -Force -Path "$dataDir\resourcesRuntime\certificates"
New-Item -ItemType Directory -Force -Path "$dataDir\deployment"

# Copy nginx configuration
Copy-Item -Path "resourcesRuntime\nginx\*.conf" -Destination "$dataDir\resourcesRuntime\nginx\"
```

### Step 4: Build and Start

```bash
# Build the Docker image
docker compose build railhq

# Start all services
docker compose up -d

# Check if services are running
docker compose ps
```

**Expected output:**
```
NAME                STATUS
railhq.io-nginx     running
railhq.io-redis     running
railhq.io-index     running
railhq.io-app       running
```

### Step 5: Access the Application

Open your browser and navigate to:

| URL | Description |
|-----|-------------|
| http://localhost | Main application |
| http://localhost:5001 | Direct access to railyWebApp |

**Default Login:**
```
Username: free@railhq.io
Password: railhq.io
```

### Docker Commands Reference

```bash
# Start services
docker compose up -d

# Stop services
docker compose down

# View logs
docker compose logs -f

# View logs for specific service
docker compose logs -f app

# Restart a service
docker compose restart app

# Rebuild and restart
docker compose up -d --build

# Create backup
docker compose run --rm backup

# Restore from backup
docker compose run --rm restoreResources
```

---

## Option 2: Manual Installation

For development or if you prefer not to use Docker.

### Step 1: Install Prerequisites

**Linux (Ubuntu/Debian):**
```bash
# Install .NET 8.0 SDK
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Install Redis
sudo apt install -y redis-server
sudo systemctl enable redis-server
sudo systemctl start redis-server
```

**Windows:**
1. Download and install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Install Redis via [Memurai](https://www.memurai.com/) or run via Docker:
   ```bash
   docker compose -f docker-compose-redis.yml up -d
   ```

**macOS:**
```bash
# Using Homebrew
brew install dotnet-sdk
brew install redis
brew services start redis
```

### Step 2: Clone and Build

```bash
# Clone repository
git clone https://github.com/YOUR_ORG/railhq.io.git
cd railhq.io

# Restore dependencies
dotnet restore

# Build all projects
dotnet build
```

### Step 3: Configure for Development

Create development configuration files:

**WebApp/railyWebIndex/appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Host": {
    "Domain": "localhost",
    "CookieDomain": "",
    "IndexUrl": "http://localhost:1380",
    "WebsiteUrl": "http://localhost:1380/",
    "WikiUrl": "http://localhost:1380/Support"
  }
}
```

**WebApp/railyWebApp/appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Host": {
    "Domain": "localhost",
    "CookieDomain": "",
    "WebsiteUrl": "http://localhost:1380/",
    "WikiUrl": "http://localhost:1380/Support"
  }
}
```

### Step 4: Run the Applications

**Option A: Using VS Code**

Open the project in VS Code and use the provided launch configurations:
- "Debug railyWebIndex"
- "Debug railyWebApp"
- "Debug all Web Projects" (starts both)

**Option B: Command Line**

```bash
# Terminal 1: Start railyWebIndex (Port 1380)
cd WebApp/railyWebIndex
dotnet run

# Terminal 2: Start railyWebApp (Port 5001)
cd WebApp/railyWebApp
dotnet run
```

### Step 5: Access the Application

- **railyWebIndex:** http://localhost:1380
- **railyWebApp:** http://localhost:5001

---

## Gateway Setup

The Gateway connects your command station (ECoS, z21) to railhq.io.

### Option A: Run Gateway from Source

```bash
# Build the Gateway
cd Gateway/railyGateway
dotnet build

# Run the Gateway
dotnet run
```

The Gateway dashboard is available at: http://localhost:8090

### Option B: Build Gateway Package (Linux)

For Debian/Ubuntu systems, you can build a `.deb` package:

```bash
# Install fpm
sudo apt install ruby ruby-dev build-essential
sudo gem install --no-document fpm

# Build and package
cd Gateway/railyGateway
./build-and-package.sh

# Install the package
sudo dpkg -i dist/railhqgateway_*.deb
```

### Gateway Configuration

The Gateway configuration is stored in `railhqGateway.json`:

```json
{
  "language": "de",
  "connection": {
    "isEnabled": true,
    "timeoutSeconds": 10,
    "host": "localhost",
    "port": 5001,
    "username": "your-email@example.com",
    "password": "your-password",
    "autoconnect": true
  },
  "localhost": {
    "listenPort": 8090,
    "listenDevice": "auto"
  },
  "ecos": {
    "isEnabled": true,
    "ip": "192.168.178.129",
    "port": 15471
  },
  "z21": {
    "isEnabled": false,
    "ip": "192.168.0.111",
    "port": 21105
  }
}
```

---

## Configuration

### Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `RAILHQ_DATA_DIR` | Base directory for all data | `~/railhq.io` | Yes |
| `RAILHQ_LOCAL_MODE` | Enable local/LAN mode | `true` | No |
| `RAILHQ_USE_TLS` | Enable HTTPS | `false` | No |
| `TLS_CERT_DIR` | Path to TLS certificates | `${RAILHQ_DATA_DIR}/resourcesRuntime/certificates` | No |
| `TLS_CERT_PW` | TLS certificate password | `` | No |
| `NGINX_CONF` | nginx configuration file | `nginx-http.conf` | No |
| `REDIS_HOST` | Redis hostname | `railhq.io-redis` | No |

### TLS/HTTPS Configuration

For production deployments with HTTPS:

1. **Set environment variables:**
   ```bash
   RAILHQ_USE_TLS=true
   NGINX_CONF=nginx-https.conf
   TLS_CERT_DIR=/path/to/certificates
   ```

2. **Place certificates:**
   - `fullchain.pem` - Certificate chain
   - `privkey.pem` - Private key

3. **Restart services:**
   ```bash
   docker compose down
   docker compose up -d
   ```

---

## Troubleshooting

### Services won't start

```bash
# Check container logs
docker compose logs -f

# Check specific service
docker compose logs -f app

# Verify all containers are running
docker compose ps
```

### Cannot connect to Redis

```bash
# Check Redis is running
docker compose ps redis

# Test Redis connection
docker exec -it railhq.io-redis redis-cli ping
# Should return: PONG
```

### Port already in use

```bash
# Find what's using the port
sudo lsof -i :80
sudo lsof -i :5001

# Stop conflicting service or change port in .env
```

### Permission denied errors

```bash
# Fix directory permissions (Linux)
sudo chown -R $USER:$USER ~/railhq.io
chmod -R 755 ~/railhq.io
```

### Browser shows "Connection refused"

1. Check services are running: `docker compose ps`
2. Check logs for errors: `docker compose logs -f`
3. Verify ports are not blocked by firewall
4. Try accessing directly: http://localhost:5001

### Reset to clean state

```bash
# Stop and remove all containers and volumes
docker compose down -v

# Remove data directory (WARNING: deletes all data!)
rm -rf ~/railhq.io

# Start fresh
# Follow installation steps again
```

---

## Getting Help

- **GitHub Issues:** [Report bugs or request features](https://github.com/YOUR_ORG/railhq.io/issues)
- **Discussions:** [Ask questions](https://github.com/YOUR_ORG/railhq.io/discussions)

---

## Next Steps

After installation:

1. ✅ Change the default password
2. ✅ Configure your command station in the Gateway
3. ✅ Create your first track plan
4. ✅ Add your locomotives
5. 🚂 Have fun operating your model railway!
