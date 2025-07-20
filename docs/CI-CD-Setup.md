# CI/CD Pipeline Setup Guide

This guide explains how to set up the CI/CD pipeline for publishing your .NET packages to NuGet.

## Prerequisites

1. **GitHub Repository**: Your code should be in a GitHub repository
2. **NuGet Account**: Create an account at [nuget.org](https://www.nuget.org/)
3. **API Key**: Generate a NuGet API key for publishing packages

## Required GitHub Secrets

You need to configure the following secrets in your GitHub repository:

### 1. NUGET_API_KEY
1. Go to [nuget.org](https://www.nuget.org/) and sign in
2. Click on your username → API Keys
3. Create a new API key with the following settings:
   - **Key Name**: GitHub Actions CI/CD
   - **Select Scopes**: Push new packages and package versions
   - **Select Packages**: All packages
   - **Glob Pattern**: Leave blank or use `AQ.Common.*`
4. Copy the generated API key
5. In your GitHub repository, go to Settings → Secrets and variables → Actions
6. Click "New repository secret"
7. Name: `NUGET_API_KEY`
8. Value: Paste the API key

### 2. GITHUB_TOKEN (Automatic)
This is automatically provided by GitHub Actions, no setup required.

## Workflow Files Overview

### 1. `ci.yml` - Continuous Integration
- **Triggers**: Push to main/master/develop branches, Pull requests
- **Actions**: 
  - Build solution
  - Run tests
  - Code quality analysis
  - Upload build artifacts
  - Generate code coverage reports

### 2. `release.yml` - Release and Publish
- **Triggers**: 
  - Git tags matching pattern `v*.*.*` (e.g., v1.0.0)
  - Manual trigger via GitHub UI
- **Actions**:
  - Validate version format
  - Build and create NuGet packages
  - Publish to NuGet.org
  - Publish to GitHub Packages
  - Create GitHub Release

### 3. `security.yml` - Security Scanning
- **Triggers**: Push to main/master, Pull requests, Weekly schedule
- **Actions**:
  - Scan for vulnerable packages
  - Check for outdated dependencies

### 4. `dependency-update.yml` - Dependency Management
- **Triggers**: Weekly schedule, Manual trigger
- **Actions**:
  - Check for package updates
  - Create PR with updated dependencies
  - Run tests to ensure compatibility

## How to Release

### Method 1: Git Tags (Recommended)
```bash
# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0
```

### Method 2: Manual Release
1. Go to your GitHub repository
2. Click "Actions" tab
3. Select "Release and Publish" workflow
4. Click "Run workflow"
5. Enter the version (e.g., 1.0.0)
6. Click "Run workflow"

## Version Naming Convention

Follow semantic versioning:
- **Major.Minor.Patch** (e.g., 1.0.0)
- **Prerelease**: 1.0.0-alpha.1, 1.0.0-beta.1, 1.0.0-rc.1

## Branch Protection and Repository Security

### Manual Setup
To configure branch protection for your repository:

1. **Branch Protection Rules**:
   - Go to Settings → Branches
   - Add rule for `main` and `master` branches
   - Enable:
     - Require a pull request before merging
     - Require status checks: "Build and Test", "Code Quality Analysis", "Validate Pull Request", "Security Vulnerability Scan"
     - Require branches to be up to date before merging
     - Require linear history
     - Require conversation resolution before merging
     - Restrict pushes that create files
     - Do not allow force pushes
     - Do not allow deletions

2. **Repository Settings**:
   - Go to Settings → General
   - Configure merge options:
     - ✅ Allow squash merging
     - ❌ Allow merge commits  
     - ✅ Allow rebase merging
     - ✅ Automatically delete head branches
     - ✅ Allow auto-merge

### Security Features

The setup includes comprehensive security measures:

- **Secret Scanning**: Automatically detects exposed API keys and tokens
- **Push Protection**: Prevents commits with secrets from being pushed
- **Dependency Alerts**: Notifies of vulnerable dependencies
- **Branch Protection**: Prevents direct pushes to main branches

## Environments

Create a `production` environment for additional security:

1. Go to Settings → Environments
2. Create environment named `production`
3. Configure protection rules:
   - Required reviewers (optional)
   - Wait timer (optional)
   - Environment secrets (NUGET_API_KEY can be stored here)

## Package Publication Process

When a release is triggered:

1. **Validation**: Version format is checked
2. **Build**: Solution is built in Release configuration
3. **Test**: All tests must pass
4. **Package**: NuGet packages are created
5. **Publish**: Packages are published to:
   - NuGet.org (public)
   - GitHub Packages (for backup/private use)
6. **Release**: GitHub release is created with changelog

## Monitoring and Maintenance

### Build Status Badges
Add to your README.md:

```markdown
[![CI](https://github.com/abdulqadir89/AQ-Libraries/actions/workflows/ci.yml/badge.svg)](https://github.com/abdulqadir89/AQ-Libraries/actions/workflows/ci.yml)
[![Release](https://github.com/abdulqadir89/AQ-Libraries/actions/workflows/release.yml/badge.svg)](https://github.com/abdulqadir89/AQ-Libraries/actions/workflows/release.yml)
```

### Security Alerts
- Enable Dependabot security updates
- Review security scan results weekly
- Keep dependencies up to date

## Troubleshooting

### Common Issues

1. **NuGet publish fails**: Check API key permissions and package names
2. **Version conflicts**: Ensure version number is unique
3. **Build failures**: Check for compilation errors or test failures
4. **Package not found**: Wait a few minutes after publishing for indexing

### Logs and Debugging
- Check GitHub Actions logs for detailed error messages
- Use the manual workflow trigger for testing
- Test locally with `dotnet pack` and `dotnet nuget push`

## Best Practices

1. **Semantic Versioning**: Follow semver for version numbers
2. **Changelog**: Document changes in each release
3. **Testing**: Ensure comprehensive test coverage
4. **Security**: Regular dependency updates and security scans
5. **Documentation**: Keep README and package descriptions updated
