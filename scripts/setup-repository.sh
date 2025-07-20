#!/bin/bash

# AQ Libraries Repository Setup Script
# This script configures branch protection and repository settings

set -e

echo "🔧 AQ Libraries Repository Setup"
echo "================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check if we're in a git repository
if ! git rev-parse --is-inside-work-tree > /dev/null 2>&1; then
    echo -e "${RED}❌ Not in a git repository. Please run this script from the repository root.${NC}"
    exit 1
fi

# Check if we have the necessary tools
command -v gh >/dev/null 2>&1 || { 
    echo -e "${RED}❌ GitHub CLI (gh) is required but not installed.${NC}"
    echo "Please install GitHub CLI: https://cli.github.com/"
    exit 1
}

echo -e "${BLUE}ℹ️ Checking GitHub CLI authentication...${NC}"
if ! gh auth status > /dev/null 2>&1; then
    echo -e "${YELLOW}⚠️ GitHub CLI is not authenticated.${NC}"
    echo "Please run: gh auth login"
    exit 1
fi

# Get repository information
REPO_OWNER=$(gh repo view --json owner --jq .owner.login)
REPO_NAME=$(gh repo view --json name --jq .name)

echo -e "${BLUE}📦 Repository: ${REPO_OWNER}/${REPO_NAME}${NC}"

# Function to configure branch protection
configure_branch_protection() {
    local branch=$1
    echo -e "${BLUE}🛡️ Configuring branch protection for '${branch}'...${NC}"
    
    # Check if branch exists
    if ! gh api repos/${REPO_OWNER}/${REPO_NAME}/branches/${branch} >/dev/null 2>&1; then
        echo -e "${YELLOW}⚠️ Branch '${branch}' does not exist, skipping...${NC}"
        return
    fi
    
    # Configure branch protection
    gh api \
        --method PUT \
        -H "Accept: application/vnd.github+json" \
        repos/${REPO_OWNER}/${REPO_NAME}/branches/${branch}/protection \
        -f required_status_checks='{"strict":true,"contexts":["Build and Test","Code Quality Analysis","Validate Pull Request","Security Vulnerability Scan"]}' \
        -F enforce_admins=false \
        -f required_pull_request_reviews='{"required_approving_review_count":1,"dismiss_stale_reviews":true,"require_code_owner_reviews":false,"require_last_push_approval":true}' \
        -F allow_force_pushes=false \
        -F allow_deletions=false \
        -F block_creations=false \
        -F required_linear_history=true \
        -F allow_fork_syncing=true \
        -F required_conversation_resolution=true \
    && echo -e "${GREEN}✅ Branch protection configured for '${branch}'${NC}" \
    || echo -e "${RED}❌ Failed to configure branch protection for '${branch}'${NC}"
}

# Function to configure repository settings
configure_repository_settings() {
    echo -e "${BLUE}⚙️ Configuring repository settings...${NC}"
    
    gh api \
        --method PATCH \
        -H "Accept: application/vnd.github+json" \
        repos/${REPO_OWNER}/${REPO_NAME} \
        -F allow_squash_merge=true \
        -F allow_merge_commit=false \
        -F allow_rebase_merge=true \
        -F delete_branch_on_merge=true \
        -F allow_auto_merge=true \
        -F squash_merge_commit_title="PR_TITLE" \
        -F squash_merge_commit_message="PR_BODY" \
    && echo -e "${GREEN}✅ Repository settings configured${NC}" \
    || echo -e "${RED}❌ Failed to configure repository settings${NC}"
}

# Function to create production environment
create_production_environment() {
    echo -e "${BLUE}🏗️ Creating production environment...${NC}"
    
    gh api \
        --method PUT \
        -H "Accept: application/vnd.github+json" \
        repos/${REPO_OWNER}/${REPO_NAME}/environments/production \
        -F wait_timer=0 \
        -f deployment_branch_policy='{"protected_branches":true,"custom_branch_policies":false}' \
    && echo -e "${GREEN}✅ Production environment created${NC}" \
    || echo -e "${RED}❌ Failed to create production environment${NC}"
}

# Function to enable security features
enable_security_features() {
    echo -e "${BLUE}🔒 Enabling security features...${NC}"
    
    # Enable secret scanning
    gh api \
        --method PUT \
        -H "Accept: application/vnd.github+json" \
        repos/${REPO_OWNER}/${REPO_NAME}/secret-scanning \
        -F enabled=true \
    && echo -e "${GREEN}✅ Secret scanning enabled${NC}" \
    || echo -e "${YELLOW}⚠️ Could not enable secret scanning (may require admin rights)${NC}"
    
    # Enable secret scanning push protection
    gh api \
        --method PUT \
        -H "Accept: application/vnd.github+json" \
        repos/${REPO_OWNER}/${REPO_NAME}/secret-scanning/push-protection \
        -F enabled=true \
    && echo -e "${GREEN}✅ Secret scanning push protection enabled${NC}" \
    || echo -e "${YELLOW}⚠️ Could not enable secret scanning push protection (may require admin rights)${NC}"
    
    # Enable dependency alerts
    gh api \
        --method PUT \
        -H "Accept: application/vnd.github+json" \
        repos/${REPO_OWNER}/${REPO_NAME}/vulnerability-alerts \
    && echo -e "${GREEN}✅ Dependency alerts enabled${NC}" \
    || echo -e "${YELLOW}⚠️ Could not enable dependency alerts (may require admin rights)${NC}"
}

# Main setup process
echo -e "${BLUE}🚀 Starting repository setup...${NC}"

# Configure branch protection for main branches
configure_branch_protection "main"
configure_branch_protection "master"

# Configure repository settings
configure_repository_settings

# Create production environment
create_production_environment

# Enable security features
enable_security_features

echo ""
echo -e "${GREEN}🎉 Repository setup completed!${NC}"
echo ""
echo -e "${BLUE}📋 Next Steps:${NC}"
echo "1. Add NUGET_API_KEY secret to repository secrets"
echo "2. Review and test the branch protection rules"
echo "3. Create your first feature branch and PR to test the workflow"
echo "4. Configure any additional team members as collaborators"
echo ""
echo -e "${BLUE}📖 Documentation:${NC}"
echo "- Setup Guide: docs/CI-CD-Setup.md" 
echo "- Pipeline Summary: docs/Pipeline-Summary.md"
echo ""
echo -e "${GREEN}✅ Your repository is now configured with industry-standard branch protection!${NC}"
