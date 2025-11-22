#!/bin/bash
set -e

# AmLink Submissions MCP - Pre-Commit Validation Script
# Runs mandatory formatting checks, build validation, and tests before allowing commits.

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Parse arguments
SKIP_TESTS=false
FIX_FORMATTING=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --fix-formatting)
            FIX_FORMATTING=true
            shift
            ;;
        *)
            echo -e "${RED}Unknown argument: $1${NC}"
            echo "Usage: $0 [--skip-tests] [--fix-formatting]"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}🚀 AmLink Submissions MCP - Pre-Commit Validation${NC}"
echo -e "${CYAN}=================================================${NC}"

success=true

# Step 1: Check code formatting
echo -e "\n${YELLOW}📝 Checking code formatting...${NC}"
if ! dotnet format --verify-no-changes > /dev/null 2>&1; then
    echo -e "${RED}❌ Code formatting issues detected!${NC}"
    
    if [ "$FIX_FORMATTING" = true ]; then
        echo -e "\n${YELLOW}🔧 Fixing formatting issues automatically...${NC}"
        if dotnet format; then
            echo -e "${GREEN}✅ Code formatting fixed!${NC}"
        else
            echo -e "${RED}❌ Failed to fix formatting issues!${NC}"
            success=false
        fi
    else
        echo -e "\n${BLUE}💡 Run with --fix-formatting to automatically fix issues, or run:${NC}"
        echo -e "   ${GRAY}dotnet format${NC}"
        success=false
    fi
else
    echo -e "${GREEN}✅ Code formatting is clean!${NC}"
fi

# Step 2: Check markdown linting
echo -e "\n${YELLOW}📄 Checking markdown formatting...${NC}"

# Find markdown files (excluding node_modules, bin, obj directories)
markdown_files=$(find . -name "*.md" -not -path "./node_modules/*" -not -path "./*/bin/*" -not -path "./*/obj/*" 2>/dev/null || true)

if [ -n "$markdown_files" ]; then
    # Check if markdownlint-cli2 is available
    if command -v npx >/dev/null 2>&1; then
        # Use npx to run markdownlint-cli2 temporarily
        export NODE_OPTIONS="--no-warnings"
        if npx --yes markdownlint-cli2@latest "**/*.md" "!**/node_modules/**" "!**/bin/**" "!**/obj/**" >/dev/null 2>&1; then
            echo -e "${GREEN}✅ Markdown formatting is clean!${NC}"
        else
            echo -e "${RED}❌ Markdown linting issues detected!${NC}"
            npx --yes markdownlint-cli2@latest "**/*.md" "!**/node_modules/**" "!**/bin/**" "!**/obj/**" 2>&1 || true
            echo -e "\n${BLUE}💡 Fix markdown issues manually or check the super-linter configuration${NC}"
            success=false
        fi
    else
        echo -e "${YELLOW}⚠️  Markdown linting skipped (Node.js/npx not available)${NC}"
        echo -e "${BLUE}💡 Install Node.js to enable markdown linting${NC}"
    fi
else
    echo -e "${BLUE}ℹ️  No markdown files found to lint${NC}"
fi

# Step 3: Build solution
echo -e "\n${YELLOW}🔨 Building solution...${NC}"
if dotnet build --configuration Release --verbosity quiet; then
    echo -e "${GREEN}✅ Build successful!${NC}"
else
    echo -e "${RED}❌ Build failed!${NC}"
    success=false
fi

# Step 4: Run tests (unless skipped)
if [ "$SKIP_TESTS" = false ]; then
    echo -e "\n${YELLOW}🧪 Running tests...${NC}"
    if dotnet test --configuration Release --verbosity quiet --no-build; then
        echo -e "${GREEN}✅ All tests passed!${NC}"
    else
        echo -e "${RED}❌ Tests failed!${NC}"
        success=false
    fi
else
    echo -e "\n${YELLOW}⚠️  Tests skipped!${NC}"
fi

# Summary
echo -e "\n${CYAN}=================================================${NC}"
if [ "$success" = true ]; then
    echo -e "${GREEN}🎉 Pre-commit validation PASSED!${NC}"
    echo -e "${GREEN}✅ Your code is ready to commit and push!${NC}"
    exit 0
else
    echo -e "${RED}💥 Pre-commit validation FAILED!${NC}"
    echo -e "${RED}❌ Please fix the issues before committing!${NC}"
    exit 1
fi