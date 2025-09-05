using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Handlers;

namespace SemanticKernel.Agents.Memory.Samples;

/// <summary>
/// Demo for the Semantic Chunking handler that shows how to chunk documents based on their structure.
/// </summary>
public static class SemanticChunkingDemo
{
    /// <summary>
    /// Simple context implementation for demo purposes.
    /// </summary>
    private sealed class NoopContext : IContext { }

    /// <summary>
    /// Runs a semantic chunking demo with structured documents.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunAsync(
        IServiceProvider serviceProvider, 
        CancellationToken ct = default)
    {
        // Get logger for orchestrator
        var logger = serviceProvider.GetService<ILogger<ImportOrchestrator>>();
        var orchestrator = new ImportOrchestrator(logger);
        
        // Get text extraction handler from DI container
        var textExtractionHandler = serviceProvider.GetRequiredService<TextExtractionHandler>();
        orchestrator.AddHandler(textExtractionHandler);
        
        // Configure semantic chunking with custom options and logger
        var semanticChunkingOptions = new SemanticChunkingOptions
        {
            TitleLevelThreshold = 2,  // Split on H2 and above (H1, H2)
            MaxChunkSize = 1500,      // Maximum chunk size in characters
            MinChunkSize = 100,       // Minimum chunk size to avoid tiny chunks
            IncludeTitleContext = true // Include title hierarchy in chunks
        };
        var semanticChunkingLogger = serviceProvider.GetService<ILogger<SemanticChunking>>();
        orchestrator.AddHandler(new SemanticChunking(semanticChunkingOptions, semanticChunkingLogger));
        
        // Add additional handlers for comparison with logging
        var embeddingsLogger = serviceProvider.GetService<ILogger<GenerateEmbeddingsHandler>>();
        orchestrator.AddHandler(new GenerateEmbeddingsHandler(embeddingsLogger));
        
        var saveRecordsLogger = serviceProvider.GetService<ILogger<SaveRecordsHandler>>();
        orchestrator.AddHandler(new SaveRecordsHandler(saveRecordsLogger));

        // Create sample documents with different structures
        var request = new DocumentUploadRequest
        {
            Files =
            {
                CreateMarkdownDocument(),
                CreateTechnicalDocument(),
                CreateSimpleDocument()
            }
        };

        // Create a custom pipeline with semantic chunking instead of using the default
        var pipeline = new DataPipelineResult
        {
            Index = "semantic-docs",
            DocumentId = Guid.NewGuid().ToString("n"),
            ExecutionId = Guid.NewGuid().ToString("n")
        };

        // Add the custom steps for semantic chunking
        pipeline.Then("text-extraction")
               .Then("semantic-chunking")  // Use semantic chunking instead of simple text chunking
               .Then("generate-embeddings")
               .Then("save-records");

        // Add files to FilesToUpload so they get processed by text extraction
        foreach (var file in request.Files)
        {
            pipeline.FilesToUpload.Add(file);
        }

        // Add context
        pipeline.ContextArguments["upload_request"] = request;
        pipeline.ContextArguments["context"] = new NoopContext();
        await orchestrator.RunPipelineAsync(pipeline, ct);
        return (pipeline.DocumentId, pipeline.Logs);
    }

    /// <summary>
    /// Demonstrates semantic chunking with simple text comparison.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Comparison results between different chunking strategies.</returns>
    public static async Task<ComparisonResult> CompareChunkingStrategiesAsync(
        IServiceProvider serviceProvider, 
        CancellationToken ct = default)
    {
        var sampleText = CreateLongStructuredDocument();
        
        // Test Simple Chunking 
        var simpleChunking = new SimpleTextChunking(new TextChunkingOptions
        {
            MaxChunkSize = 1000,
            TextOverlap = 100
        });

        // Test Semantic Chunking with different configurations
        var semanticChunkingH2 = new SemanticChunking(new SemanticChunkingOptions
        {
            TitleLevelThreshold = 2,
            MaxChunkSize = 1000
        });

        var semanticChunkingH3 = new SemanticChunking(new SemanticChunkingOptions
        {
            TitleLevelThreshold = 3,
            MaxChunkSize = 1000
        });

        // Create test pipeline
        var textExtractionHandler = serviceProvider.GetRequiredService<TextExtractionHandler>();
        
        // Test each chunking strategy
        var results = new ComparisonResult();
        
        results.SimpleChunking = await TestChunkingStrategy(textExtractionHandler, simpleChunking, sampleText, ct);
        results.SemanticH2 = await TestChunkingStrategy(textExtractionHandler, semanticChunkingH2, sampleText, ct);
        results.SemanticH3 = await TestChunkingStrategy(textExtractionHandler, semanticChunkingH3, sampleText, ct);

        return results;
    }

    /// <summary>
    /// Tests a specific chunking strategy and returns the results.
    /// </summary>
    private static async Task<ChunkingTestResult> TestChunkingStrategy(
        TextExtractionHandler textExtractionHandler,
        IPipelineStepHandler chunkingHandler,
        string sampleText,
        CancellationToken ct)
    {
        var orchestrator = new ImportOrchestrator();
        orchestrator.AddHandler(textExtractionHandler);
        orchestrator.AddHandler(chunkingHandler);

        var request = new DocumentUploadRequest
        {
            Files =
            {
                new UploadedFile
                {
                    FileName = "test-document.md",
                    Bytes = Encoding.UTF8.GetBytes(sampleText),
                    MimeType = "text/markdown"
                }
            }
        };

        // Create custom pipeline
        var pipeline = new DataPipelineResult
        {
            Index = "test",
            DocumentId = Guid.NewGuid().ToString("n"),
            ExecutionId = Guid.NewGuid().ToString("n")
        };

        // Add steps
        pipeline.Then("text-extraction")
               .Then(chunkingHandler.StepName);

        // Add files to FilesToUpload for text extraction
        foreach (var file in request.Files)
        {
            pipeline.FilesToUpload.Add(file);
        }

        // Add context
        pipeline.ContextArguments["upload_request"] = request;
        pipeline.ContextArguments["context"] = new NoopContext();

        await orchestrator.RunPipelineAsync(pipeline, ct);

        var chunks = pipeline.Files.FindAll(f => f.ArtifactType == ArtifactTypes.TextPartition);
        
        return new ChunkingTestResult
        {
            ChunkCount = chunks.Count,
            TotalSize = sampleText.Length,
            AverageChunkSize = chunks.Count > 0 ? chunks.Sum(c => c.Size) / chunks.Count : 0,
            Logs = pipeline.Logs.ToList()
        };
    }

    /// <summary>
    /// Creates a sample Markdown document with various heading levels.
    /// </summary>
    private static UploadedFile CreateMarkdownDocument()
    {
        var content = @"# Product Documentation

This is the main product documentation that covers all aspects of our software solution.

## Getting Started

Welcome to our product! This section will help you get up and running quickly.

### Installation

To install the software, follow these simple steps:

1. Download the installer from our website
2. Run the installer as administrator
3. Follow the installation wizard

The installation process typically takes 5-10 minutes depending on your system.

### Configuration

After installation, you'll need to configure the application:

- Set up your user preferences
- Configure network settings
- Import your existing data

## User Guide

This comprehensive guide covers all features of the application.

### Basic Features

The application provides several basic features that every user should know:

#### Creating Projects

To create a new project:
1. Click on ""New Project""
2. Enter project details
3. Select a template
4. Click ""Create""

#### Managing Files

File management is straightforward:
- Use the file browser to navigate
- Drag and drop files to upload
- Right-click for context menu options

### Advanced Features

For power users, we offer advanced functionality:

#### Automation

Set up automated workflows to streamline your processes. The automation engine supports:
- Scheduled tasks
- Event-driven triggers
- Custom scripts

#### Integration

Connect with external systems through our robust API:
- REST endpoints
- Webhook support
- Third-party connectors

## Troubleshooting

Common issues and their solutions.

### Performance Issues

If you experience slow performance:
1. Check system requirements
2. Close unnecessary applications
3. Restart the software

### Connection Problems

Network connectivity issues can be resolved by:
- Checking firewall settings
- Verifying proxy configuration
- Testing network connectivity

## API Reference

Detailed API documentation for developers.

### Authentication

All API calls require authentication using API keys.

### Endpoints

Available REST endpoints:
- GET /api/projects
- POST /api/projects
- PUT /api/projects/{id}
- DELETE /api/projects/{id}

## Support

Contact information and support resources.

For technical support, please contact:
- Email: support@example.com
- Phone: 1-800-123-4567
- Online chat: Available 24/7
";

        return new UploadedFile
        {
            FileName = "product-docs.md",
            Bytes = Encoding.UTF8.GetBytes(content),
            MimeType = "text/markdown"
        };
    }

    /// <summary>
    /// Creates a technical document with numbered sections.
    /// </summary>
    private static UploadedFile CreateTechnicalDocument()
    {
        var content = @"System Architecture Specification
===================================

1. Introduction

This document describes the system architecture for our cloud-based platform.

1.1. Purpose

The purpose of this document is to provide a comprehensive overview of the system architecture.

1.2. Scope

This specification covers the following components:
- Frontend applications
- Backend services
- Database design
- Infrastructure requirements

2. System Overview

The system follows a microservices architecture pattern with the following key characteristics:
- Scalable and distributed
- Event-driven communication
- Cloud-native design

2.1. Architecture Principles

Our architecture is based on the following principles:
- Single responsibility
- Loose coupling
- High cohesion
- Fault tolerance

2.2. Technology Stack

The system uses modern technologies:
- Frontend: React, TypeScript
- Backend: .NET Core, Python
- Database: PostgreSQL, Redis
- Infrastructure: Kubernetes, Docker

3. Component Design

Detailed design of each system component.

3.1. User Interface Layer

The UI layer consists of:
- Web application
- Mobile applications
- Admin dashboard

3.1.1. Web Application

Built using React and TypeScript, the web application provides:
- Responsive design
- Real-time updates
- Offline capabilities

3.1.2. Mobile Applications

Native mobile apps for iOS and Android with features:
- Push notifications
- Biometric authentication
- Sync capabilities

3.2. API Gateway

The API Gateway serves as the entry point for all client requests:
- Request routing
- Authentication
- Rate limiting
- Monitoring

3.3. Microservices

Individual services handle specific business domains:
- User service
- Product service
- Order service
- Payment service

4. Data Architecture

Comprehensive data management strategy.

4.1. Database Design

Each microservice has its own database:
- User database (PostgreSQL)
- Product catalog (PostgreSQL)
- Session store (Redis)
- Analytics (MongoDB)

4.2. Data Flow

Data flows through the system via:
- REST APIs
- Message queues
- Event streams
- Batch processing

5. Security

Security measures implemented throughout the system.

5.1. Authentication

Multi-factor authentication using:
- OAuth 2.0
- JWT tokens
- Biometric verification

5.2. Authorization

Role-based access control with:
- Fine-grained permissions
- Resource-level security
- Audit logging

6. Deployment

Deployment and infrastructure considerations.

6.1. Container Orchestration

Using Kubernetes for:
- Service orchestration
- Auto-scaling
- Load balancing
- Health monitoring

6.2. CI/CD Pipeline

Automated deployment pipeline:
- Source control integration
- Automated testing
- Progressive deployment
- Rollback capabilities
";

        return new UploadedFile
        {
            FileName = "architecture-spec.txt",
            Bytes = Encoding.UTF8.GetBytes(content),
            MimeType = "text/plain"
        };
    }

    /// <summary>
    /// Creates a simple document for basic testing.
    /// </summary>
    private static UploadedFile CreateSimpleDocument()
    {
        var content = @"Simple Document

This is a simple document without complex structure.

It contains multiple paragraphs but no formal headings. The semantic chunker should handle this by falling back to paragraph-based chunking when no significant headings are detected.

This paragraph demonstrates how the chunker maintains readability while splitting content appropriately based on the configured maximum chunk size.

The algorithm is designed to be flexible and handle various document formats gracefully.
";

        return new UploadedFile
        {
            FileName = "simple-doc.txt",
            Bytes = Encoding.UTF8.GetBytes(content),
            MimeType = "text/plain"
        };
    }

    /// <summary>
    /// Creates a long structured document for comparison testing.
    /// </summary>
    private static string CreateLongStructuredDocument()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Comprehensive Guide to Machine Learning");
        sb.AppendLine();
        sb.AppendLine("Machine learning is a subset of artificial intelligence that enables computers to learn and improve from experience without being explicitly programmed.");
        sb.AppendLine();
        
        sb.AppendLine("## Introduction to Machine Learning");
        sb.AppendLine();
        sb.AppendLine("Machine learning algorithms build mathematical models based on training data to make predictions or decisions. This field has revolutionized many industries and continues to drive innovation.");
        sb.AppendLine();
        sb.AppendLine("The core idea is to create systems that can automatically learn and improve their performance on a specific task through experience, without human intervention for each decision.");
        sb.AppendLine();
        
        sb.AppendLine("### Types of Machine Learning");
        sb.AppendLine();
        sb.AppendLine("There are three main types of machine learning:");
        sb.AppendLine("- Supervised Learning");
        sb.AppendLine("- Unsupervised Learning"); 
        sb.AppendLine("- Reinforcement Learning");
        sb.AppendLine();
        
        sb.AppendLine("#### Supervised Learning");
        sb.AppendLine();
        sb.AppendLine("Supervised learning uses labeled training data to learn a mapping from inputs to outputs. Common algorithms include linear regression, decision trees, and neural networks.");
        sb.AppendLine();
        
        sb.AppendLine("#### Unsupervised Learning");
        sb.AppendLine();
        sb.AppendLine("Unsupervised learning finds patterns in data without labeled examples. Clustering and dimensionality reduction are common unsupervised techniques.");
        sb.AppendLine();
        
        sb.AppendLine("## Data Preprocessing");
        sb.AppendLine();
        sb.AppendLine("Data preprocessing is a crucial step in machine learning pipelines. Raw data often contains noise, missing values, and inconsistencies that must be addressed.");
        sb.AppendLine();
        sb.AppendLine("Key preprocessing steps include data cleaning, normalization, feature selection, and handling missing values. The quality of preprocessing directly impacts model performance.");
        sb.AppendLine();
        
        sb.AppendLine("### Data Cleaning");
        sb.AppendLine();
        sb.AppendLine("Data cleaning involves identifying and correcting errors in the dataset. This includes removing duplicates, fixing inconsistent formats, and handling outliers.");
        sb.AppendLine();
        
        sb.AppendLine("### Feature Engineering");
        sb.AppendLine();
        sb.AppendLine("Feature engineering is the process of creating new features from existing data. This can significantly improve model performance by providing more relevant information.");
        sb.AppendLine();
        
        sb.AppendLine("## Model Selection and Training");
        sb.AppendLine();
        sb.AppendLine("Choosing the right model depends on the problem type, data characteristics, and performance requirements. Different algorithms have different strengths and weaknesses.");
        sb.AppendLine();
        sb.AppendLine("Training involves feeding data to the algorithm so it can learn patterns. This process requires careful tuning of hyperparameters and monitoring for overfitting.");
        sb.AppendLine();
        
        sb.AppendLine("### Cross-Validation");
        sb.AppendLine();
        sb.AppendLine("Cross-validation is a technique for assessing model performance by splitting data into training and testing sets multiple times. This provides a more robust estimate of model quality.");
        sb.AppendLine();
        
        sb.AppendLine("## Model Evaluation");
        sb.AppendLine();
        sb.AppendLine("Proper evaluation ensures that models will perform well on new, unseen data. Different metrics are appropriate for different types of problems.");
        sb.AppendLine();
        sb.AppendLine("Common evaluation metrics include accuracy, precision, recall, F1-score for classification, and mean squared error for regression problems.");
        sb.AppendLine();
        
        return sb.ToString();
    }

    /// <summary>
    /// Results from comparing different chunking strategies.
    /// </summary>
    public class ComparisonResult
    {
        public ChunkingTestResult SimpleChunking { get; set; } = new();
        public ChunkingTestResult SemanticH2 { get; set; } = new();
        public ChunkingTestResult SemanticH3 { get; set; } = new();
    }

    /// <summary>
    /// Results from testing a specific chunking strategy.
    /// </summary>
    public class ChunkingTestResult
    {
        public int ChunkCount { get; set; }
        public long TotalSize { get; set; }
        public long AverageChunkSize { get; set; }
        public List<PipelineLogEntry> Logs { get; set; } = new();
    }
}
