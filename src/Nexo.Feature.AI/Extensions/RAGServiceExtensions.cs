using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Interfaces.RAG;
using Nexo.Feature.AI.Services;
using Nexo.Feature.AI.Services.RAG;

namespace Nexo.Feature.AI.Extensions
{
    /// <summary>
    /// Extension methods for registering RAG services
    /// </summary>
    public static class RAGServiceExtensions
    {
        /// <summary>
        /// Adds RAG services to the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddRAGServices(this IServiceCollection services)
        {
            // Register core RAG services
            services.AddSingleton<IDocumentationVectorStore, DocumentationVectorStore>();
            services.AddSingleton<IDocumentationEmbeddingService, DocumentationEmbeddingService>();
            services.AddSingleton<IDocumentationParser, MarkdownDocumentationParser>();
            
            // Register model orchestrator
            services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();
            
            services.AddSingleton<IDocumentationRAGService, DocumentationRAGService>();
            services.AddSingleton<IDocumentationIngestionService, DocumentationIngestionService>();

            // Note: RAG-enhanced agents are registered in the Agent project

            return services;
        }

        /// <summary>
        /// Adds RAG services with custom configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddRAGServices(this IServiceCollection services, Action<RAGServiceOptions> configure)
        {
            var options = new RAGServiceOptions();
            configure(options);

            // Register core RAG services
            services.AddSingleton<IDocumentationVectorStore, DocumentationVectorStore>();
            services.AddSingleton<IDocumentationEmbeddingService, DocumentationEmbeddingService>();
            services.AddSingleton<IDocumentationParser, MarkdownDocumentationParser>();
            
            // Register model orchestrator
            services.AddSingleton<IModelOrchestrator, ModelOrchestrator>();
            
            services.AddSingleton<IDocumentationRAGService, DocumentationRAGService>();
            services.AddSingleton<IDocumentationIngestionService, DocumentationIngestionService>();

            // Note: RAG-enhanced agents are registered in the Agent project

            // Register options
            services.AddSingleton(options);

            return services;
        }
    }

    /// <summary>
    /// Configuration options for RAG services
    /// </summary>
    public class RAGServiceOptions
    {
        /// <summary>
        /// Path to the documentation directory
        /// </summary>
        public string DocumentationPath { get; set; } = "./docs/rag-documentation";

        /// <summary>
        /// Default similarity threshold for vector search
        /// </summary>
        public double DefaultSimilarityThreshold { get; set; } = 0.7;

        /// <summary>
        /// Default maximum results for vector search
        /// </summary>
        public int DefaultMaxResults { get; set; } = 5;

        /// <summary>
        /// Whether to enable automatic documentation ingestion on startup
        /// </summary>
        public bool AutoIngestOnStartup { get; set; } = false;

        /// <summary>
        /// Whether to enable RAG enhancement by default
        /// </summary>
        public bool EnableRAGByDefault { get; set; } = true;

        /// <summary>
        /// Embedding model configuration
        /// </summary>
        public EmbeddingModelOptions EmbeddingModel { get; set; } = new();

        /// <summary>
        /// Vector store configuration
        /// </summary>
        public VectorStoreOptions VectorStore { get; set; } = new();
    }

    /// <summary>
    /// Configuration options for embedding models
    /// </summary>
    public class EmbeddingModelOptions
    {
        /// <summary>
        /// The embedding model to use
        /// </summary>
        public string ModelName { get; set; } = "simple-tfidf";

        /// <summary>
        /// The dimension size of the embedding vectors
        /// </summary>
        public int DimensionSize { get; set; } = 100;

        /// <summary>
        /// Whether to enable caching
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Cache size limit
        /// </summary>
        public int CacheSizeLimit { get; set; } = 1000;
    }

    /// <summary>
    /// Configuration options for vector store
    /// </summary>
    public class VectorStoreOptions
    {
        /// <summary>
        /// The type of vector store to use
        /// </summary>
        public string StoreType { get; set; } = "in-memory";

        /// <summary>
        /// Connection string for external vector stores
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Index name for external vector stores
        /// </summary>
        public string IndexName { get; set; } = "nexo-documentation";

        /// <summary>
        /// Whether to enable persistence
        /// </summary>
        public bool EnablePersistence { get; set; } = false;

        /// <summary>
        /// Persistence file path
        /// </summary>
        public string PersistencePath { get; set; } = "./data/vector-store.json";
    }
}
