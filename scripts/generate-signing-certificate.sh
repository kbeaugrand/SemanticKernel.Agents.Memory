#!/bin/bash

# Script to generate a code signing certificate for NuGet packages
# This creates a self-signed certificate suitable for development and testing

set -e

CERT_NAME="SemanticKernel.Agents.Memory Code Signing"
CERT_FILE="PackageSigning.pfx"
SNK_FILE="StrongName.snk"
DAYS_VALID=365
PASSWORD=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-25)

echo "🔐 Generating code signing certificate..."
echo "Certificate Name: $CERT_NAME"
echo "Valid for: $DAYS_VALID days"
echo "Password: $PASSWORD"
echo ""

# Create certificates directory if it doesn't exist
mkdir -p certificates

# Generate a private key
echo "📝 Generating private key..."
openssl genrsa -out certificates/private.key 2048

# Create a certificate signing request
echo "📋 Creating certificate signing request..."
openssl req -new -key certificates/private.key -out certificates/cert.csr -subj "/CN=$CERT_NAME/O=SemanticKernel.Agents.Memory/C=US"

# Generate self-signed certificate
echo "🏆 Generating self-signed certificate..."
openssl x509 -req -in certificates/cert.csr -signkey certificates/private.key -out certificates/cert.crt -days $DAYS_VALID

# Convert to PFX format for code signing
echo "📦 Converting to PFX format..."
openssl pkcs12 -export -out certificates/$CERT_FILE -inkey certificates/private.key -in certificates/cert.crt -password pass:$PASSWORD

# Generate strong name key for assembly signing
echo "🔑 Generating strong name key..."
if command -v sn &> /dev/null; then
    sn -k certificates/$SNK_FILE
else
    # Use openssl to generate a key file compatible with .NET
    openssl genrsa -out certificates/temp.key 1024
    openssl rsa -in certificates/temp.key -outform DER -out certificates/$SNK_FILE
    rm certificates/temp.key
fi

# Copy certificates to repository root
echo "📋 Copying certificates to repository root..."
cp certificates/$CERT_FILE ../
cp certificates/$SNK_FILE ../

# Save password for easy secret export
echo "$PASSWORD" > certificates/password.txt

# Get base64 encoded certificate
echo "🔢 Encoding certificate to base64..."
CERT_BASE64=$(base64 -w 0 certificates/$CERT_FILE)

# Clean up temporary files
echo "🧹 Cleaning up temporary files..."
rm certificates/private.key certificates/cert.csr certificates/cert.crt

echo ""
echo "✅ Certificate generation completed!"
echo ""
echo "📁 Files created:"
echo "  - certificates/$CERT_FILE"
echo "  - certificates/$SNK_FILE"
echo "  - $CERT_FILE (copied to repository root)"
echo "  - $SNK_FILE (copied to repository root)"
echo ""
echo "🔐 Certificate Details:"
echo "  Password: $PASSWORD"
echo ""
echo "📋 Base64 Encoded Certificate (for GitHub Secrets):"
echo "$CERT_BASE64"
echo ""
echo "🚀 Next Steps:"
echo "1. Add the following secrets to your GitHub repository:"
echo "   - PACKAGE_SIGNING_CERT_BASE64: $CERT_BASE64"
echo "   - PACKAGE_SIGNING_CERT_PASSWORD: $PASSWORD"
echo ""
echo "2. The certificate files have been added to .gitignore to prevent accidental commits"
echo ""
echo "⚠️  Important: This is a self-signed certificate suitable for development."
echo "    For production, consider using a certificate from a trusted CA."
