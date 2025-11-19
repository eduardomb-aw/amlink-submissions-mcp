// Azure Container Apps Infrastructure
// This Bicep template deploys the AmLink Submissions MCP to Azure Container Apps

@description('Environment name (e.g., dev, staging, prod)')
param environmentName string = 'dev'

@description('Application name')
param appName string = 'amlink-submissions-mcp'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Container image tag')
param imageTag string = 'latest'

@description('Container registry URL')
param registryUrl string = 'ghcr.io'

@description('Registry username (GitHub actor)')
param registryUsername string = ''

@description('Registry password (GitHub token)')
@secure()
param registryPassword string = ''

@description('Client ID for Identity Server')
param clientId string = 'amlink-submissions-client'

@description('Client secret for Identity Server')
@secure()
param clientSecret string = ''

@description('Submission API URL')
param submissionApiUrl string = 'https://api.example.com/submissions'

@description('Submission API key')
@secure()
param submissionApiKey string = ''

@description('Certificate password')
@secure()
param certPassword string = ''

// Variables
var resourceNamePrefix = '${appName}-${environmentName}'
var clientAppName = '${resourceNamePrefix}-client'
var serverAppName = '${resourceNamePrefix}-server'

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${resourceNamePrefix}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'cae-${resourceNamePrefix}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// MCP Server Container App
resource mcpServerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-${serverAppName}'
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 9080
        allowInsecure: false
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
      registries: [
        {
          server: registryUrl
          username: registryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: [
        {
          name: 'registry-password'
          value: registryPassword
        }
        {
          name: 'client-secret'
          value: clientSecret
        }
        {
          name: 'submission-api-key'
          value: submissionApiKey
        }
        {
          name: 'cert-password'
          value: certPassword
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${registryUrl}/eduardomb-aw/amlink-submissions-mcp-server:${imageTag}'
          name: 'mcp-server'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:9080'
            }
            {
              name: 'IdentityServer__Issuer'
              value: 'https://${serverAppName}.${containerAppsEnvironment.properties.defaultDomain}'
            }
            {
              name: 'IdentityServer__Clients__0__ClientId'
              value: clientId
            }
            {
              name: 'IdentityServer__Clients__0__ClientSecret'
              secretRef: 'client-secret'
            }
            {
              name: 'ExternalApis__SubmissionApi__BaseUrl'
              value: submissionApiUrl
            }
            {
              name: 'ExternalApis__SubmissionApi__ApiKey'
              secretRef: 'submission-api-key'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 9080
              }
              initialDelaySeconds: 30
              periodSeconds: 10
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 9080
              }
              initialDelaySeconds: 5
              periodSeconds: 5
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '30'
              }
            }
          }
        ]
      }
    }
  }
}

// MCP Client Container App
resource mcpClientApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'ca-${clientAppName}'
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
      registries: [
        {
          server: registryUrl
          username: registryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: [
        {
          name: 'registry-password'
          value: registryPassword
        }
        {
          name: 'client-secret'
          value: clientSecret
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${registryUrl}/eduardomb-aw/amlink-submissions-mcp-client:${imageTag}'
          name: 'mcp-client'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'McpClient__ServerUrl'
              value: 'https://${mcpServerApp.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'IdentityServer__Authority'
              value: 'https://${mcpServerApp.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'IdentityServer__ClientId'
              value: clientId
            }
            {
              name: 'IdentityServer__ClientSecret'
              secretRef: 'client-secret'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 30
              periodSeconds: 10
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 5
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

// Outputs
output serverUrl string = 'https://${mcpServerApp.properties.configuration.ingress.fqdn}'
output clientUrl string = 'https://${mcpClientApp.properties.configuration.ingress.fqdn}'
output resourceGroupName string = resourceGroup().name
output environmentName string = environmentName