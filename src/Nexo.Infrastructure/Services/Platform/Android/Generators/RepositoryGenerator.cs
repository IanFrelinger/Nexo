using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform.Android.Generators
{
    public partial class RepositoryGenerator
    {
        private readonly ILogger<RepositoryGenerator> _logger;

        public RepositoryGenerator(ILogger<RepositoryGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<GeneratedFile>> GenerateRepositoriesAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var files = new List<GeneratedFile>();

            try
            {
                _logger.LogInformation("Generating Repositories for {EntityCount} entities", applicationLogic.Entities.Count);

                foreach (var entity in applicationLogic.Entities)
                {
                    var repositoryFile = new GeneratedFile
                    {
                        Name = $"{entity}Repository.kt",
                        Content = GenerateRepositoryCode(entity),
                        Type = "Repository"
                    };
                    files.Add(repositoryFile);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Repositories");
                return files;
            }
        }

        private string GenerateRepositoryCode(string entityName)
        {
            return $@"package com.example.app.repository

import com.example.app.database.{entityName}Dao
import com.example.app.database.{entityName}Entity
import com.example.app.model.{entityName}
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class {entityName}Repository @Inject constructor(
    private val {entityName.ToLower()}Dao: {entityName}Dao
) {{

    fun getAll{entityName}s(): Flow<List<{entityName}>> {{
        return {entityName.ToLower()}Dao.getAll{entityName}s().map {{ entities ->
            entities.map {{ it.to{entityName}() }}
        }}
    }}

    suspend fun get{entityName}ById(id: Long): {entityName}? {{
        return {entityName.ToLower()}Dao.get{entityName}ById(id)?.to{entityName}()
    }}

    suspend fun insert{entityName}({entityName.ToLower()}: {entityName}): Long {{
        return {entityName.ToLower()}Dao.insert{entityName}({entityName.ToLower()}.to{entityName}Entity())
    }}

    suspend fun update{entityName}({entityName.ToLower()}: {entityName}) {{
        {entityName.ToLower()}Dao.update{entityName}({entityName.ToLower()}.to{entityName}Entity())
    }}

    suspend fun delete{entityName}(id: Long) {{
        val {entityName.ToLower()} = {entityName.ToLower()}Dao.get{entityName}ById(id)
        {entityName.ToLower()}?.let {{ {entityName.ToLower()}Dao.delete{entityName}(it) }}
    }}

    suspend fun delete{entityName}({entityName.ToLower()}: {entityName}) {{
        {entityName.ToLower()}Dao.delete{entityName}({entityName.ToLower()}.to{entityName}Entity())
    }}
}}

// Extension functions for mapping between Entity and Model
fun {entityName}Entity.to{entityName}(): {entityName} {{
    return {entityName}(
        id = this.id,
        name = this.name,
        description = this.description,
        createdAt = this.createdAt
    )
}}

fun {entityName}.to{entityName}Entity(): {entityName}Entity {{
    return {entityName}Entity(
        id = this.id,
        name = this.name,
        description = this.description,
        createdAt = this.createdAt
    )
}}";
        }
    }
}
