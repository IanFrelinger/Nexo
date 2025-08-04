# Nexo Docker Directory

This directory contains all Docker-related files for containerization and deployment of the Nexo project.

## 📁 Directory Structure

### 🐳 Docker Compose Files
- **docker-compose.yml** - Main application containerization
- **docker-compose.test-environments.yml** - Testing environment orchestration

### 🏗️ Dockerfiles
- **Dockerfile.unity-test** - Unity testing environment container

## 🚀 Usage Examples

### Running the Main Application
```bash
# Start the main application
docker-compose -f docker/docker-compose.yml up

# Start in detached mode
docker-compose -f docker/docker-compose.yml up -d

# Stop the application
docker-compose -f docker/docker-compose.yml down
```

### Running Test Environments
```bash
# Start all test environments
docker-compose -f docker/docker-compose.test-environments.yml up

# Run specific test environment
docker-compose -f docker/docker-compose.test-environments.yml up dotnet8-linux

# Stop test environments
docker-compose -f docker/docker-compose.test-environments.yml down
```

### Building Unity Test Container
```bash
# Build Unity test container
docker build -f docker/Dockerfile.unity-test -t nexo-unity-test .

# Run Unity test container
docker run --rm nexo-unity-test
```

## 📋 File Details

### Docker Compose Files

#### `docker-compose.yml`
- **Purpose**: Main application containerization
- **Services**: Core Nexo application services
- **Usage**: `docker-compose -f docker/docker-compose.yml up`
- **Features**:
  - Application containerization
  - Service orchestration
  - Development environment setup

#### `docker-compose.test-environments.yml`
- **Purpose**: Testing environment orchestration
- **Services**: Multiple test environments
- **Usage**: `docker-compose -f docker/docker-compose.test-environments.yml up`
- **Environments**:
  - dotnet8-linux
  - dotnet7-linux
  - dotnet6-linux
  - unity-linux
  - performance-linux
  - mono-linux
  - legacy-linux

### Dockerfiles

#### `Dockerfile.unity-test`
- **Purpose**: Unity testing environment container
- **Base Image**: Unity-specific base image
- **Usage**: `docker build -f docker/Dockerfile.unity-test -t nexo-unity-test .`
- **Features**:
  - Unity runtime environment
  - Testing framework setup
  - Performance monitoring tools

## 🔧 Docker Development

### Adding New Docker Files
1. Place Docker files in this directory
2. Follow naming convention: `Dockerfile.{purpose}` or `docker-compose.{purpose}.yml`
3. Update this README with file documentation
4. Include usage examples and features

### Docker Guidelines
- Use multi-stage builds when possible
- Optimize for layer caching
- Include health checks
- Use specific base image versions
- Document environment variables

## 🧪 Test Environment Details

### Available Test Environments
The `docker-compose.test-environments.yml` provides 7 different test environments:

1. **dotnet8-linux**: Latest .NET 8.0 environment
2. **dotnet7-linux**: .NET 7.0 environment
3. **dotnet6-linux**: .NET 6.0 environment
4. **unity-linux**: Unity development environment
5. **performance-linux**: Performance testing environment
6. **mono-linux**: Mono runtime environment
7. **legacy-linux**: Legacy .NET Framework environment

### Environment Features
- **Cross-platform testing**: Test across different .NET versions
- **Performance monitoring**: Built-in performance analysis tools
- **Unity integration**: Full Unity development environment
- **Legacy support**: .NET Framework and Mono compatibility

## 📊 Current Status

- **Docker Compose Files**: 2 files (main, test-environments)
- **Dockerfiles**: 1 file (unity-test)
- **Total Docker Files**: 3 files
- **Test Environments**: 7 environments

## 🔄 Recent Updates

- **Organization**: Moved all Docker files from root directory to organized structure
- **Documentation**: Added comprehensive Docker documentation
- **Structure**: Created logical categorization and usage examples

## 🚨 Troubleshooting

### Common Issues
1. **Port conflicts**: Check if ports are already in use
2. **Volume permissions**: Ensure proper file permissions for mounted volumes
3. **Memory limits**: Increase Docker memory allocation for large builds
4. **Network issues**: Check Docker network configuration

### Debug Commands
```bash
# Check container logs
docker-compose -f docker/docker-compose.yml logs

# Inspect running containers
docker ps

# Check container resource usage
docker stats
```

---

**Last Updated**: January 27, 2025  
**Next Review**: February 3, 2025 