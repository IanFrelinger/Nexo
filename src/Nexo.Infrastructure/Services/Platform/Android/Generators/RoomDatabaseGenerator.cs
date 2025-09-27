using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform.Android.Generators
{
    public partial class RoomDatabaseGenerator
    {
        private readonly ILogger<RoomDatabaseGenerator> _logger;

        public RoomDatabaseGenerator(ILogger<RoomDatabaseGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<GeneratedFile>> GenerateRoomDatabaseAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var files = new List<GeneratedFile>();

            try
            {
                _logger.LogInformation("Generating Room database for {EntityCount} entities", applicationLogic.Entities.Count);

                // Generate database class
                var databaseFile = new GeneratedFile
                {
                    Name = "AppDatabase.kt",
                    Content = GenerateDatabaseCode(applicationLogic),
                    Type = "Database"
                };
                files.Add(databaseFile);

                // Generate entities
                foreach (var entity in applicationLogic.Entities)
                {
                    var entityFile = new GeneratedFile
                    {
                        Name = $"{entity}Entity.kt",
                        Content = GenerateEntityCode(entity),
                        Type = "Entity"
                    };
                    files.Add(entityFile);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Room database");
                return files;
            }
        }

        private string GenerateDatabaseCode(ApplicationLogic applicationLogic)
        {
            var entities = string.Join(", ", applicationLogic.Entities);
            return $@"package com.example.app.database

import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import android.content.Context

@Database(
    entities = [{entities}],
    version = 1,
    exportSchema = false
)
abstract class AppDatabase : RoomDatabase() {{
    abstract fun {string.Join("Dao", applicationLogic.Entities.Select(e => e.ToLower()))}(): {string.Join("Dao", applicationLogic.Entities.Select(e => e + "Dao"))}

    companion object {{
        @Volatile
        private var INSTANCE: AppDatabase? = null

        fun getDatabase(context: Context): AppDatabase {{
            return INSTANCE ?: synchronized(this) {{
                val instance = Room.databaseBuilder(
                    context.applicationContext,
                    AppDatabase::class.java,
                    ""app_database""
                ).build()
                INSTANCE = instance
                instance
            }}
        }}
    }}
}}";
        }

        private string GenerateEntityCode(string entityName)
        {
            return $@"package com.example.app.database

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = ""{entityName.ToLower()}s"")
data class {entityName}Entity(
    @PrimaryKey(autoGenerate = true)
    val id: Long = 0,
    val name: String,
    val description: String,
    val createdAt: Long = System.currentTimeMillis()
)

@Entity(tableName = ""{entityName.ToLower()}_dao"")
interface {entityName}Dao {{
    @Query(""SELECT * FROM {entityName.ToLower()}s"")
    suspend fun getAll{entityName}s(): List<{entityName}Entity>

    @Query(""SELECT * FROM {entityName.ToLower()}s WHERE id = :id"")
    suspend fun get{entityName}ById(id: Long): {entityName}Entity?

    @Insert
    suspend fun insert{entityName}({entityName.ToLower()}: {entityName}Entity): Long

    @Update
    suspend fun update{entityName}({entityName.ToLower()}: {entityName}Entity)

    @Delete
    suspend fun delete{entityName}({entityName.ToLower()}: {entityName}Entity)
}}";
        }
    }
}
