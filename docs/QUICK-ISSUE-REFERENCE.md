# Quick Reference: Creating GitHub Issues

This is a quick reference for creating the 8 new GitHub issues identified in PRs #3 and #6.

## One-Command Creation

```bash
# 1. Authenticate (first time only)
gh auth login

# 2. Create all issues
./scripts/create-issues.sh
```

## Issues Summary

| # | Title | Labels | Priority | Effort |
|---|-------|--------|----------|--------|
| 3 | Add Input Validation to All MCP Tools | enhancement, security, validation, high-priority | High | 1-2d |
| 4 | Replace Console.WriteLine with Structured Logging | enhancement, logging, observability, high-priority | High | 1d |
| 5 | Improve JWT Token Handling | enhancement, security, medium-priority | Medium | 2d |
| 6 | Add HTTP Client Resilience | enhancement, reliability, medium-priority | Medium | 2d |
| 7 | Add Correlation IDs and Improve Error Context | enhancement, observability, medium-priority | Medium | 1-2d |
| 8 | Implement Health Checks | enhancement, observability, medium-priority | Medium | 1d |
| 9 | Add EditorConfig and Code Style Enforcement | enhancement, code-quality, low-priority | Low | 0.5d |
| 10 | Enhance Configuration Validation at Startup | enhancement, configuration, low-priority | Low | 1d |
| 11 | Define API Versioning Strategy | enhancement, api, documentation, low-priority | Low | 1d |

## Manual Creation Shortcuts

For each issue, the content is available in [ISSUES-TO-CREATE.md](ISSUES-TO-CREATE.md).

Quick links for manual creation:
1. Go to: https://github.com/eduardomb-aw/amlink-submissions-mcp/issues/new
2. Copy content from ISSUES-TO-CREATE.md for the specific issue
3. Set labels as shown in the table above
4. Click "Submit new issue"

## Files You Need

| File | Purpose |
|------|---------|
| [scripts/create-issues.sh](../scripts/create-issues.sh) | Automated creation script |
| [ISSUES-TO-CREATE.md](ISSUES-TO-CREATE.md) | Full content for manual creation |
| [CREATE-ISSUES-GUIDE.md](CREATE-ISSUES-GUIDE.md) | Detailed step-by-step guide |
| [ISSUE-CREATION-SUMMARY.md](../ISSUE-CREATION-SUMMARY.md) | Executive summary |

## Verification

After creating issues, check:
- [ ] All 11 issues exist (2 existing + 8 new + 1 new = 11 total)
- [ ] Labels are applied correctly
- [ ] Each issue has detailed description
- [ ] Acceptance criteria are present
- [ ] Related documentation is linked

## Common Issues

**"gh not authenticated"**
```bash
gh auth login
# Follow prompts to authenticate
```

**"Permission denied"**
```bash
chmod +x scripts/create-issues.sh
```

**"Script not found"**
```bash
# Run from repository root
cd /path/to/amlink-submissions-mcp
./scripts/create-issues.sh
```

## Quick Links

- View issues: https://github.com/eduardomb-aw/amlink-submissions-mcp/issues
- Create new issue: https://github.com/eduardomb-aw/amlink-submissions-mcp/issues/new
- Task details: [TASKS.md](../TASKS.md)
- Implementation guide: [TASK-IMPLEMENTATION-GUIDE.md](TASK-IMPLEMENTATION-GUIDE.md)

---

*For more details, see [CREATE-ISSUES-GUIDE.md](CREATE-ISSUES-GUIDE.md)*
