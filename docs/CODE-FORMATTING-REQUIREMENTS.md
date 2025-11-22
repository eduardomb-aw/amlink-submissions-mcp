# Code Formatting Requirements - Quick Reference

## ⚠️ MANDATORY: Always Check Formatting Before Push/PR

Code formatting violations will cause **automatic PR validation failures**. Always validate formatting before committing.

## 🚀 Quick Commands

### Recommended: Use the Pre-Commit Script
```powershell
# Windows PowerShell (auto-fix formatting)
.\scripts\pre-commit-check.ps1 -FixFormatting

# Linux/macOS (auto-fix formatting)  
./scripts/pre-commit-check.sh --fix-formatting
```

### Manual Validation
```bash
# Check for formatting issues
dotnet format --verify-no-changes

# Fix formatting automatically
dotnet format

# Complete validation sequence
dotnet format --verify-no-changes && dotnet build --configuration Release && dotnet test --configuration Release
```

## 📚 Where to Find Instructions

1. **[.github/copilot-instructions.md](.github/copilot-instructions.md)** - Complete coding standards with detailed formatting requirements
2. **[README.md](README.md)** - Quick start development section and contributing workflow
3. **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** - Detailed development workflow with formatting checks

## 🔧 Available Scripts

- **`scripts/pre-commit-check.ps1`** - PowerShell pre-commit validation script
- **`scripts/pre-commit-check.sh`** - Bash pre-commit validation script (cross-platform)

### Script Features
- ✅ Automatic formatting check
- 🔧 Optional auto-fix formatting (`-FixFormatting` / `--fix-formatting`)
- 🏗️ Build validation
- 🧪 Test execution
- ⏭️ Skip tests option (`-SkipTests` / `--skip-tests`)

## 🚨 Common Issues

### "Fix whitespace formatting" Error
```bash
# This means formatting violations were detected
# Solution: Run the fix command
dotnet format

# Then verify it's clean
dotnet format --verify-no-changes
```

### Mixed Tabs/Spaces
- Project uses **spaces** for indentation
- Configure your IDE to show whitespace characters
- Enable EditorConfig support for consistent rules

### Line Endings
- Use consistent line endings (CRLF on Windows, LF on Unix)
- Let dotnet format handle this automatically

## ✅ Integration Points

**When formatting is checked:**
1. Before every commit (developer responsibility)
2. In GitHub Actions CI/CD pipeline
3. PR validation workflows
4. Merge protection rules

**Why this matters:**
- Prevents PR validation failures
- Maintains consistent code quality
- Reduces review friction
- Ensures reliable CI/CD builds