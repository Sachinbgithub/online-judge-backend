# LeetCode Compiler API - Network Deployment Guide

This guide explains how to deploy the LeetCode Compiler API on different systems and access it over the network.

## Prerequisites

- .NET 8.0 SDK installed on the target system
- SQL Server (or SQL Server Express) installed and configured
- Network access between devices

## Method 1: Direct .NET Deployment

### Step 1: Build and Run
```bash
# Navigate to the API directory
cd LeetCodeCompiler.API

# Build the application
dotnet build --configuration Release

# Run the application
dotnet run --configuration Release --urls "http://0.0.0.0:5081;https://0.0.0.0:7169"
```

### Step 2: Find Your IP Address
```bash
# On Windows
ipconfig

# On Linux/Mac
ifconfig
# or
ip addr
```

### Step 3: Configure Firewall
Allow incoming connections on ports 5081 and 7169:

**Windows:**
```powershell
# Open PowerShell as Administrator
New-NetFirewallRule -DisplayName "LeetCode API HTTP" -Direction Inbound -Protocol TCP -LocalPort 5081 -Action Allow
New-NetFirewallRule -DisplayName "LeetCode API HTTPS" -Direction Inbound -Protocol TCP -LocalPort 7169 -Action Allow
```

**Linux:**
```bash
sudo ufw allow 5081
sudo ufw allow 7169
```

## Method 2: Docker Deployment

### Step 1: Build and Run with Docker
```bash
# Build and run with docker-compose
docker-compose up --build

# Or build and run manually
docker build -t leetcode-api .
docker run -p 5081:5081 -p 7169:7169 leetcode-api
```

## Method 3: Production Deployment

### Step 1: Publish the Application
```bash
dotnet publish --configuration Release --output ./publish
```

### Step 2: Deploy to Target System
Copy the `publish` folder to your target system and run:
```bash
dotnet LeetCodeCompiler.API.dll --urls "http://0.0.0.0:5081;https://0.0.0.0:7169"
```

## Accessing the API

### Local Network Access
Once deployed, the API will be available at:
- **HTTP**: `http://[YOUR_IP_ADDRESS]:5081`
- **HTTPS**: `https://[YOUR_IP_ADDRESS]:7169`
- **Swagger UI**: `http://[YOUR_IP_ADDRESS]:5081/swagger`

### Internet Access (Optional)
To make the API accessible over the internet:

1. **Port Forwarding**: Configure your router to forward ports 5081 and 7169 to your server
2. **Dynamic DNS**: Use a service like No-IP or DuckDNS for a consistent domain name
3. **SSL Certificate**: Obtain a valid SSL certificate for HTTPS access

## Database Configuration

### Local SQL Server
The current configuration uses a local SQL Server instance. For network deployment, you may need to:

1. **Enable TCP/IP** in SQL Server Configuration Manager
2. **Configure SQL Server to listen on all interfaces**
3. **Update connection string** in `Program.cs` if using a remote database

### Example Remote Database Connection
```csharp
// Update the connection string in Program.cs
options.UseSqlServer("Server=YOUR_SERVER_IP;Database=LeetCode;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;");
```

## Security Considerations

1. **CORS Configuration**: Update CORS policy to allow specific origins instead of `AllowAnyOrigin()`
2. **Authentication**: Implement proper authentication for production use
3. **HTTPS**: Always use HTTPS in production
4. **Firewall**: Only open necessary ports
5. **Database Security**: Use strong passwords and limit database access

## Troubleshooting

### Common Issues

1. **Connection Refused**: Check firewall settings and ensure ports are open
2. **Database Connection**: Verify SQL Server is running and accessible
3. **CORS Errors**: Update CORS policy to allow your frontend domain
4. **SSL Certificate**: For HTTPS, ensure valid certificates are configured

### Testing Connectivity
```bash
# Test HTTP endpoint
curl http://[YOUR_IP_ADDRESS]:5081/swagger

# Test HTTPS endpoint
curl https://[YOUR_IP_ADDRESS]:7169/swagger
```

## Environment Variables

You can configure the application using environment variables:

```bash
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS=http://0.0.0.0:5081;https://0.0.0.0:7169
```

## Monitoring and Logs

The application logs are available in the console output. For production, consider:
- Logging to files
- Using a logging service like Serilog
- Implementing health checks
- Setting up monitoring with tools like Application Insights 