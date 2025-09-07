# GitHub Actions Setup

This repository uses several GitHub Actions workflows for automated CI/CD. Some workflows require repository secrets to be configured.

## Required Secrets

### For Release Workflow

To enable automatic NuGet package publishing, configure the following secret in your repository:

1. Go to your repository settings
2. Navigate to "Secrets and variables" → "Actions"
3. Add the following repository secret:

| Secret Name | Description | Required For |
|------------|-------------|--------------|
| `NUGET_API_KEY` | Your NuGet.org API key for publishing packages | Release workflow (automatic NuGet publishing) |

### How to get a NuGet API Key

1. Go to [NuGet.org](https://www.nuget.org)
2. Sign in to your account
3. Go to your account settings
4. Navigate to "API Keys"
5. Create a new API key with:
   - **Key Name**: SemanticKernel.Agents.Memory (or similar descriptive name)
   - **Select Scopes**: Push new packages and package versions
   - **Select Packages**: Choose packages or use glob pattern `SemanticKernel.Agents.Memory.*`
6. Copy the generated API key
7. Add it as the `NUGET_API_KEY` secret in your GitHub repository

## Workflow Behaviors

### Without Secrets

- All workflows will run successfully for builds and tests
- Release workflow will skip NuGet publishing step and show a warning
- GitHub releases will still be created automatically on version tags

### With Secrets Configured

- All functionality enabled including automatic NuGet package publishing
- Release workflow will publish packages to NuGet.org when version tags are pushed

## Security Notes

- Never commit API keys or other secrets to your repository
- Use GitHub repository secrets for sensitive information
- API keys should have minimal required permissions (push packages only)
- Consider using scoped API keys that only work with your specific packages

## Testing Workflows

You can test the workflows by:

1. **CI Workflow**: Push any code changes to trigger build and test
2. **Release Workflow**: Create and push a version tag (e.g., `git tag v1.0.0 && git push origin v1.0.0`)
3. **Samples Workflow**: Modify files in the `samples/` directory
4. **Code Quality**: Any push will trigger formatting and linting checks
5. **Security Scanning**: CodeQL analysis runs on pushes and pull requests

## Dependabot

This repository uses Dependabot for automated dependency updates. Configuration is in `.github/dependabot.yml` and includes:

- .NET NuGet packages
- GitHub Actions versions
- Docker base images
- Python requirements (for MarkitDown service)

Dependabot will automatically create pull requests for dependency updates on a weekly schedule.
