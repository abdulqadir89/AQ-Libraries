# AQ Libraries Repository Setup Script (PowerShell)
# This script configures branch protection and repository settings

param(
    [switch]$WhatIf = $false
)

$ErrorActionPreference = "Stop"

Write-Host "🔧 AQ Libraries Repository Setup" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Check if we're in a git repository
try {
    git rev-parse --is-inside-work-tree | Out-Null
} catch {
    Write-Host "❌ Not in a git repository. Please run this script from the repository root." -ForegroundColor Red
    exit 1
}

# Check if GitHub CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "❌ GitHub CLI (gh) is required but not installed." -ForegroundColor Red
    Write-Host "Please install GitHub CLI: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

Write-Host "ℹ️ Checking GitHub CLI authentication..." -ForegroundColor Blue
try {
    gh auth status | Out-Null
} catch {
    Write-Host "⚠️ GitHub CLI is not authenticated." -ForegroundColor Yellow
    Write-Host "Please run: gh auth login" -ForegroundColor Yellow
    exit 1
}

# Get repository information
$repoOwner = (gh repo view --json owner --jq .owner.login)
$repoName = (gh repo view --json name --jq .name)

Write-Host "📦 Repository: $repoOwner/$repoName" -ForegroundColor Blue

function Configure-BranchProtection {
    param([string]$branch)
    
    Write-Host "🛡️ Configuring branch protection for '$branch'..." -ForegroundColor Blue
    
    # Check if branch exists
    try {
        gh api "repos/$repoOwner/$repoName/branches/$branch" | Out-Null
    } catch {
        Write-Host "⚠️ Branch '$branch' does not exist, skipping..." -ForegroundColor Yellow
        return
    }
    
    if ($WhatIf) {
        Write-Host "WHAT-IF: Would configure branch protection for '$branch'" -ForegroundColor Magenta
        return
    }
    
    # Configure branch protection
    try {
        $protectionConfig = @{
            required_status_checks = @{
                strict = $true
                contexts = @(
                    "Build and Test",
                    "Code Quality Analysis", 
                    "Validate Pull Request",
                    "Security Vulnerability Scan"
                )
            }
            enforce_admins = $false
            required_pull_request_reviews = @{
                required_approving_review_count = 1
                dismiss_stale_reviews = $true
                require_code_owner_reviews = $false
                require_last_push_approval = $true
            }
            restrictions = $null
            allow_force_pushes = $false
            allow_deletions = $false
            block_creations = $false
            required_linear_history = $true
            required_conversation_resolution = $true
        }
        
        $json = $protectionConfig | ConvertTo-Json -Depth 10
        gh api --method PUT -H "Accept: application/vnd.github+json" "repos/$repoOwner/$repoName/branches/$branch/protection" --input - <<< $json
        
        Write-Host "✅ Branch protection configured for '$branch'" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to configure branch protection for '$branch': $_" -ForegroundColor Red
    }
}

function Configure-RepositorySettings {
    Write-Host "⚙️ Configuring repository settings..." -ForegroundColor Blue
    
    if ($WhatIf) {
        Write-Host "WHAT-IF: Would configure repository settings" -ForegroundColor Magenta
        return
    }
    
    try {
        gh api --method PATCH -H "Accept: application/vnd.github+json" "repos/$repoOwner/$repoName" `
            -F allow_squash_merge=true `
            -F allow_merge_commit=false `
            -F allow_rebase_merge=true `
            -F delete_branch_on_merge=true `
            -F allow_auto_merge=true `
            -F squash_merge_commit_title="PR_TITLE" `
            -F squash_merge_commit_message="PR_BODY"
        
        Write-Host "✅ Repository settings configured" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to configure repository settings: $_" -ForegroundColor Red
    }
}

function Create-ProductionEnvironment {
    Write-Host "🏗️ Creating production environment..." -ForegroundColor Blue
    
    if ($WhatIf) {
        Write-Host "WHAT-IF: Would create production environment" -ForegroundColor Magenta
        return
    }
    
    try {
        gh api --method PUT -H "Accept: application/vnd.github+json" "repos/$repoOwner/$repoName/environments/production" `
            -F wait_timer=0 `
            -f 'deployment_branch_policy={"protected_branches":true,"custom_branch_policies":false}'
        
        Write-Host "✅ Production environment created" -ForegroundColor Green
    } catch {
        Write-Host "❌ Failed to create production environment: $_" -ForegroundColor Red
    }
}

function Enable-SecurityFeatures {
    Write-Host "🔒 Enabling security features..." -ForegroundColor Blue
    
    if ($WhatIf) {
        Write-Host "WHAT-IF: Would enable security features" -ForegroundColor Magenta
        return
    }
    
    # Enable secret scanning
    try {
        gh api --method PUT -H "Accept: application/vnd.github+json" "repos/$repoOwner/$repoName/secret-scanning" -F enabled=true
        Write-Host "✅ Secret scanning enabled" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ Could not enable secret scanning (may require admin rights)" -ForegroundColor Yellow
    }
    
    # Enable secret scanning push protection
    try {
        gh api --method PUT -H "Accept: application/vnd.github+json" "repos/$repoOwner/$repoName/secret-scanning/push-protection" -F enabled=true
        Write-Host "✅ Secret scanning push protection enabled" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ Could not enable secret scanning push protection (may require admin rights)" -ForegroundColor Yellow
    }
    
    # Enable dependency alerts
    try {
        gh api --method PUT -H "Accept: application/vnd.github+json" "repos/$repoOwner/$repoName/vulnerability-alerts"
        Write-Host "✅ Dependency alerts enabled" -ForegroundColor Green
    } catch {
        Write-Host "⚠️ Could not enable dependency alerts (may require admin rights)" -ForegroundColor Yellow
    }
}

# Main setup process
Write-Host "🚀 Starting repository setup..." -ForegroundColor Blue

if ($WhatIf) {
    Write-Host "🔍 WHAT-IF MODE: No changes will be made" -ForegroundColor Magenta
    Write-Host ""
}

# Configure branch protection for main branches
Configure-BranchProtection "main"
Configure-BranchProtection "master"

# Configure repository settings
Configure-RepositorySettings

# Create production environment
Create-ProductionEnvironment

# Enable security features
Enable-SecurityFeatures

Write-Host ""
Write-Host "🎉 Repository setup completed!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Blue
Write-Host "1. Add NUGET_API_KEY secret to repository secrets"
Write-Host "2. Review and test the branch protection rules"
Write-Host "3. Create your first feature branch and PR to test the workflow"
Write-Host "4. Configure any additional team members as collaborators"
Write-Host ""
Write-Host "📖 Documentation:" -ForegroundColor Blue
Write-Host "- Setup Guide: docs/CI-CD-Setup.md"
Write-Host "- Pipeline Summary: docs/Pipeline-Summary.md"
Write-Host ""
Write-Host "✅ Your repository is now configured with industry-standard branch protection!" -ForegroundColor Green
