#!/bin/bash

# Script to export secrets for GitHub repository
# Run this script after generating certificates to get the base64 values for GitHub secrets

set -e

echo "🔐 Exporting secrets for GitHub repository..."
echo ""

# Check if files exist
if [ ! -f "StrongName.snk" ]; then
    echo "❌ StrongName.snk not found. Run ./scripts/generate-signing-certificate.sh first"
    exit 1
fi

if [ ! -f "PackageSigning.pfx" ]; then
    echo "❌ PackageSigning.pfx not found. Run ./scripts/generate-signing-certificate.sh first"
    exit 1
fi

echo "📋 GitHub Repository Secrets to add:"
echo ""

# Export Strong Name Key
echo "🔑 STRONGNAME_KEY_BASE64:"
echo "$(base64 -w 0 StrongName.snk)"
echo ""

# Export Package Signing Certificate
echo "📦 PACKAGE_SIGNING_CERT_BASE64:"
echo "$(base64 -w 0 PackageSigning.pfx)"
echo ""

# Get the password from the last run if available
if [ -f "scripts/certificates/password.txt" ]; then
    echo "🔐 PACKAGE_SIGNING_CERT_PASSWORD:"
    cat scripts/certificates/password.txt
    echo ""
else
    echo "🔐 PACKAGE_SIGNING_CERT_PASSWORD:"
    echo "⚠️  Password not found. Check the output from generate-signing-certificate.sh"
    echo ""
fi

echo "📝 How to add these secrets to your GitHub repository:"
echo ""
echo "1. Go to your GitHub repository"
echo "2. Click on Settings → Secrets and variables → Actions"
echo "3. Click 'New repository secret' for each secret above"
echo "4. Copy the secret name and value exactly as shown"
echo ""
echo "🚀 Required secrets for CI/CD:"
echo "   - STRONGNAME_KEY_BASE64: For strong name signing"
echo "   - PACKAGE_SIGNING_CERT_BASE64: For package signing (optional)"
echo "   - PACKAGE_SIGNING_CERT_PASSWORD: Certificate password (optional)"
echo "   - NUGET_API_KEY: For publishing to NuGet.org (for releases)"
echo ""
echo "✅ After adding the secrets, your CI/CD will use them automatically!"
