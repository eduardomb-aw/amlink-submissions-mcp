# Deployment Guide

This guide covers deployment options and configurations for the AmLink Submissions MCP application.

## 🏗️ Deployment Options

### 1. Docker Compose (Recommended for Development/Small Production)

#### Quick Start
```bash
# Production deployment
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Development deployment  
docker-compose up -d
```

#### Environment Variables
Create a `.env` file with required variables:
```bash
IDENTITY_SERVER_CLIENT_SECRET=your-secret-here
OPENAI_API_KEY=your-openai-key-here
ASPNETCORE_Kestrel__Certificates__Default__Password=your-cert-password
```

### 2. Kubernetes (Recommended for Production)

#### Prerequisites
- Kubernetes cluster (AKS, EKS, GKE)
- kubectl configured
- Helm (optional)

#### Deployment Steps
```bash
# Create namespace
kubectl create namespace amlink-mcp

# Create secrets
kubectl create secret generic amlink-secrets \
  --from-literal=identity-server-secret="your-secret" \
  --from-literal=openai-api-key="your-key" \
  -n amlink-mcp

# Deploy applications
kubectl apply -f k8s/ -n amlink-mcp
```

### 3. Azure Container Instances

#### ARM Template Deployment
```bash
# Deploy with Azure CLI
az deployment group create \
  --resource-group amlink-mcp-rg \
  --template-file azure/container-instances.json \
  --parameters @azure/parameters.json
```

### 4. AWS ECS/Fargate

#### Task Definition
```bash
# Register task definition
aws ecs register-task-definition \
  --cli-input-json file://aws/task-definition.json

# Create service
aws ecs create-service \
  --cluster amlink-mcp \
  --service-name amlink-mcp-service \
  --task-definition amlink-mcp:1 \
  --desired-count 2
```

## 🔒 SSL/TLS Configuration

### Development (Self-Signed)
```bash
# Generate development certificate
dotnet dev-certs https -ep aspnetapp.pfx -p "password123"
dotnet dev-certs https --trust
```

### Production Options

#### Option 1: Let's Encrypt (Recommended)
```yaml
# docker-compose.yml with Traefik
services:
  traefik:
    image: traefik:v2.10
    command:
      - --certificatesresolvers.letsencrypt.acme.email=admin@example.com
      - --certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json
      - --certificatesresolvers.letsencrypt.acme.httpchallenge.entrypoint=web
    volumes:
      - ./letsencrypt:/letsencrypt
```

#### Option 2: Cloud Load Balancer SSL
- **Azure**: Application Gateway with SSL termination
- **AWS**: ALB with ACM certificates
- **GCP**: Load Balancer with Google-managed certificates

#### Option 3: Custom Certificates
```bash
# Mount custom certificates
volumes:
  - ./certs/certificate.pfx:/app/certificate.pfx:ro
environment:
  - ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certificate.pfx
  - ASPNETCORE_Kestrel__Certificates__Default__Password=certificate-password
```

## 🎯 Environment-Specific Configurations

### Development
```yaml
# docker-compose.override.yml
services:
  amlink-mcp-server:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80;https://+:443
    volumes:
      - ./src:/src:cached  # Hot reload
```

### Staging
```yaml
# docker-compose.staging.yml
services:
  amlink-mcp-server:
    environment:
      - ASPNETCORE_ENVIRONMENT=Staging
      - ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
    deploy:
      resources:
        limits:
          memory: 256M
```

### Production
```yaml
# docker-compose.prod.yml
services:
  amlink-mcp-server:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      resources:
        limits:
          memory: 512M
          cpus: '0.5'
      restart_policy:
        condition: unless-stopped
```

## 📊 Monitoring & Observability

### Health Checks
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:80/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### Logging
```yaml
# Centralized logging with ELK stack
services:
  elasticsearch:
    image: elasticsearch:8.11.0
  kibana:
    image: kibana:8.11.0
  logstash:
    image: logstash:8.11.0
```

### Metrics Collection
```yaml
# Prometheus monitoring
services:
  prometheus:
    image: prom/prometheus
    ports:
      - "9090:9090"
  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
```

## 🚀 CI/CD Integration

### GitHub Actions Deployment
```yaml
# Deploy to production
- name: Deploy to Production
  run: |
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml pull
    docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Azure DevOps Pipeline
```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
      - main

stages:
- stage: Deploy
  jobs:
  - deployment: DeployProduction
    environment: 'production'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: DockerCompose@0
            inputs:
              action: 'Run services'
              dockerComposeFile: 'docker-compose.yml'
```

## 🔧 Troubleshooting

### Common Issues

#### Container Startup Failures
```bash
# Check container logs
docker logs amlink-mcp-server --tail 50

# Check container resource usage
docker stats

# Verify environment variables
docker exec amlink-mcp-server env | grep ASPNETCORE
```

#### SSL Certificate Issues
```bash
# Verify certificate validity
openssl x509 -in certificate.crt -text -noout

# Test certificate in container
docker exec amlink-mcp-server curl -k https://localhost:443/health
```

#### Network Connectivity
```bash
# Test container networking
docker exec amlink-mcp-client curl http://amlink-mcp-server:80/health

# Check port bindings
docker port amlink-mcp-server
```

### Performance Optimization

#### Memory Optimization
```dockerfile
# Use Alpine base images
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

# Set memory limits
ENV DOTNET_GCHeapHardLimit=0x10000000
```

#### CPU Optimization
```yaml
# Resource limits
deploy:
  resources:
    limits:
      cpus: '1.0'
      memory: 1G
    reservations:
      cpus: '0.5'
      memory: 512M
```

## 📋 Pre-deployment Checklist

- [ ] Environment variables configured
- [ ] SSL certificates valid and accessible
- [ ] Database connections tested
- [ ] External API endpoints accessible
- [ ] Resource limits configured
- [ ] Health checks working
- [ ] Monitoring configured
- [ ] Backup strategy in place
- [ ] Rollback plan documented
- [ ] Security scanning passed

## 🆘 Emergency Procedures

### Quick Rollback
```bash
# Stop current version
docker-compose down

# Deploy previous version
docker-compose -f docker-compose.yml up -d --scale amlink-mcp-server=0
docker-compose -f docker-compose.yml up -d --scale amlink-mcp-server=1
```

### Emergency Contacts
- **DevOps Team**: devops@example.com
- **Security Team**: security@example.com
- **On-call Engineer**: +1-555-0123