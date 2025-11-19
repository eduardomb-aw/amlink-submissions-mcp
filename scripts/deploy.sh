#!/bin/bash

# Deploy from Container Registry Script
# This script helps deploy the application using published container images

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Default values
REGISTRY="ghcr.io"
IMAGE_TAG="latest"
ENV_FILE=".env.prod"
COMPOSE_FILE="docker-compose.registry.yml"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_usage() {
    echo "Usage: $0 [OPTIONS] COMMAND"
    echo ""
    echo "Commands:"
    echo "  deploy    Deploy the application using published images"
    echo "  stop      Stop the running application"
    echo "  logs      Show application logs"
    echo "  status    Show application status"
    echo "  cleanup   Remove containers and networks"
    echo ""
    echo "Options:"
    echo "  -r, --registry REGISTRY    Container registry (default: ghcr.io)"
    echo "  -t, --tag TAG             Image tag (default: latest)"
    echo "  -e, --env-file FILE       Environment file (default: .env.prod)"
    echo "  -h, --help                Show this help message"
}

check_prerequisites() {
    echo -e "${BLUE}Checking prerequisites...${NC}"
    
    # Check if docker is installed
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}Error: Docker is not installed${NC}"
        exit 1
    fi
    
    # Check if docker-compose is available
    if ! docker compose version &> /dev/null && ! command -v docker-compose &> /dev/null; then
        echo -e "${RED}Error: Docker Compose is not available${NC}"
        exit 1
    fi
    
    # Check if environment file exists
    if [ ! -f "$PROJECT_ROOT/$ENV_FILE" ]; then
        echo -e "${YELLOW}Warning: Environment file $ENV_FILE not found${NC}"
        echo -e "${YELLOW}Creating from template...${NC}"
        if [ -f "$PROJECT_ROOT/.env.prod.example" ]; then
            cp "$PROJECT_ROOT/.env.prod.example" "$PROJECT_ROOT/$ENV_FILE"
            echo -e "${YELLOW}Please edit $ENV_FILE with your actual values before deploying${NC}"
        else
            echo -e "${RED}Error: No environment template found${NC}"
            exit 1
        fi
    fi
    
    echo -e "${GREEN}Prerequisites check passed${NC}"
}

deploy() {
    echo -e "${BLUE}Deploying amlink-submissions-mcp...${NC}"
    echo -e "${BLUE}Registry: $REGISTRY${NC}"
    echo -e "${BLUE}Tag: $IMAGE_TAG${NC}"
    echo -e "${BLUE}Environment: $ENV_FILE${NC}"
    
    check_prerequisites
    
    cd "$PROJECT_ROOT"
    
    # Pull latest images
    echo -e "${BLUE}Pulling container images...${NC}"
    REGISTRY=$REGISTRY CLIENT_IMAGE_TAG=$IMAGE_TAG SERVER_IMAGE_TAG=$IMAGE_TAG \
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" pull
    
    # Start services
    echo -e "${BLUE}Starting services...${NC}"
    REGISTRY=$REGISTRY CLIENT_IMAGE_TAG=$IMAGE_TAG SERVER_IMAGE_TAG=$IMAGE_TAG \
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d
    
    echo -e "${GREEN}Deployment completed!${NC}"
    echo -e "${GREEN}Client: https://localhost:8443${NC}"
    echo -e "${GREEN}Server: https://localhost:9443${NC}"
}

stop() {
    echo -e "${BLUE}Stopping amlink-submissions-mcp...${NC}"
    cd "$PROJECT_ROOT"
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" stop
    echo -e "${GREEN}Application stopped${NC}"
}

show_logs() {
    echo -e "${BLUE}Showing application logs...${NC}"
    cd "$PROJECT_ROOT"
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" logs -f
}

show_status() {
    echo -e "${BLUE}Application status:${NC}"
    cd "$PROJECT_ROOT"
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps
}

cleanup() {
    echo -e "${BLUE}Cleaning up containers and networks...${NC}"
    cd "$PROJECT_ROOT"
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down --remove-orphans
    echo -e "${GREEN}Cleanup completed${NC}"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -r|--registry)
            REGISTRY="$2"
            shift 2
            ;;
        -t|--tag)
            IMAGE_TAG="$2"
            shift 2
            ;;
        -e|--env-file)
            ENV_FILE="$2"
            shift 2
            ;;
        -h|--help)
            print_usage
            exit 0
            ;;
        deploy|stop|logs|status|cleanup)
            COMMAND="$1"
            shift
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            print_usage
            exit 1
            ;;
    esac
done

# Execute command
case $COMMAND in
    deploy)
        deploy
        ;;
    stop)
        stop
        ;;
    logs)
        show_logs
        ;;
    status)
        show_status
        ;;
    cleanup)
        cleanup
        ;;
    *)
        echo -e "${RED}No command specified${NC}"
        print_usage
        exit 1
        ;;
esac