# Guide: Creating GitHub Issues from Recent PRs

This guide explains how to create the GitHub issues identified in PRs #3 and #6.

## Overview

Based on the repository analysis in PR #3 (Identify Potential Repository Issues) and task definition in PR #6 (Create tasks for the new feature implementation), we have identified 10 improvement tasks that need to be tracked as GitHub issues.

**Current Status:**
- ✅ Issue #1: Setup Production Deployment Environment (exists)
- ✅ Issue #2: Add Comprehensive Test Suite (exists)
- ⏳ Issues #3-#11: Need to be created (8 new issues)

## Quick Start

### Option 1: Automated Script (Recommended)

If you have GitHub CLI (`gh`) installed and authenticated:

```bash
# Authenticate with GitHub (if not already done)
gh auth login

# Run the script to create all issues
./scripts/create-issues.sh
```

The script will:
1. Check if you're authenticated with GitHub CLI
2. Create all 8 remaining issues with proper labels and content
3. Display the URLs of created issues

### Option 2: Manual Creation

If you prefer to create issues manually or don't have `gh` CLI:

1. Go to https://github.com/eduardomb-aw/amlink-submissions-mcp/issues/new
2. Copy the content from [ISSUES-TO-CREATE.md](ISSUES-TO-CREATE.md) for each issue
3. Set the appropriate labels as specified in each issue section
4. Create the issue

## Issues to Create

### Phase 1: High Priority (Foundation)

#### Issue #3: Add Input Validation to All MCP Tools
- **Labels:** `enhancement`, `security`, `validation`, `high-priority`
- **Effort:** 1-2 days
- **Description:** Add comprehensive input validation to prevent NullReferenceException and security risks

#### Issue #4: Replace Console.WriteLine with Structured Logging
- **Labels:** `enhancement`, `logging`, `observability`, `high-priority`
- **Effort:** 1 day
- **Description:** Replace 15 Console.WriteLine calls with ILogger for proper observability

### Phase 2: Medium Priority (Reliability)

#### Issue #5: Improve JWT Token Handling
- **Labels:** `enhancement`, `security`, `medium-priority`
- **Effort:** 2 days
- **Description:** Replace manual JWT parsing with System.IdentityModel.Tokens.Jwt library

#### Issue #6: Add HTTP Client Resilience
- **Labels:** `enhancement`, `reliability`, `medium-priority`
- **Effort:** 2 days
- **Description:** Add retry logic, circuit breaker, and timeout configuration

#### Issue #7: Add Correlation IDs and Improve Error Context
- **Labels:** `enhancement`, `observability`, `medium-priority`
- **Effort:** 1-2 days
- **Description:** Implement correlation IDs for request tracing across services

#### Issue #8: Implement Health Checks
- **Labels:** `enhancement`, `observability`, `medium-priority`
- **Effort:** 1 day
- **Description:** Implement /health endpoint for load balancers and monitoring

### Phase 3: Low Priority (Quality)

#### Issue #9: Add EditorConfig and Code Style Enforcement
- **Labels:** `enhancement`, `code-quality`, `low-priority`
- **Effort:** 0.5 days
- **Description:** Add .editorconfig and Roslyn analyzers for consistent code style

#### Issue #10: Enhance Configuration Validation at Startup
- **Labels:** `enhancement`, `configuration`, `low-priority`
- **Effort:** 1 day
- **Description:** Add comprehensive configuration validation with fail-fast behavior

#### Issue #11: Define API Versioning Strategy
- **Labels:** `enhancement`, `api`, `documentation`, `low-priority`
- **Effort:** 1 day
- **Description:** Define and document versioning strategy for MCP tools

## Verification

After creating the issues, verify:

1. All 11 issues exist (including existing #1 and #2)
2. Labels are correctly applied
3. Issues are properly referenced in TASKS.md
4. Project board is updated (if using GitHub Projects)

## Next Steps

Once all issues are created:

1. **Prioritize:** Review and confirm the priority levels
2. **Assign:** Assign issues to team members
3. **Milestones:** Create milestones for Phase 1, 2, and 3
4. **Project Board:** Add issues to a GitHub Projects board for tracking
5. **Start Implementation:** Begin with Phase 1 high-priority tasks

## Related Documentation

- [TASKS.md](../TASKS.md) - Detailed task specifications
- [ISSUES-TO-CREATE.md](ISSUES-TO-CREATE.md) - Full issue content for copy-paste
- [POTENTIAL-IMPROVEMENTS.md](POTENTIAL-IMPROVEMENTS.md) - Original analysis
- [TASK-IMPLEMENTATION-GUIDE.md](TASK-IMPLEMENTATION-GUIDE.md) - Implementation guide

## Troubleshooting

### GitHub CLI Not Authenticated

```bash
gh auth login
# Follow the prompts to authenticate
```

### Script Permission Denied

```bash
chmod +x scripts/create-issues.sh
```

### Manual Issue Creation Template

When creating issues manually, use this template structure:

```markdown
## Description
[Detailed description of the problem]

## Current Issues
- [List of current issues]

## Proposed Solution
[Description with code examples]

## Acceptance Criteria
- [ ] [Criterion 1]
- [ ] [Criterion 2]

## Files to Modify
- [List of files]

## Dependencies
[Any dependencies]

## Related Documentation
- [Links to related docs]

**Priority:** [High/Medium/Low]
**Estimated Effort:** [X days]
```

## Support

If you encounter any issues or have questions:
1. Check the [TASK-IMPLEMENTATION-GUIDE.md](TASK-IMPLEMENTATION-GUIDE.md)
2. Review the original analysis in [POTENTIAL-IMPROVEMENTS.md](POTENTIAL-IMPROVEMENTS.md)
3. Consult the [TASKS.md](../TASKS.md) for detailed specifications

---

*Document created: 2025-11-20*  
*Based on: PRs #3 and #6*
