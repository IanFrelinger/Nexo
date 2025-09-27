using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Android.Generators
{
    public partial class RoomDatabaseCodeGenerator
    {
        private readonly ILogger<RoomDatabaseCodeGenerator> _logger;

        public RoomDatabaseCodeGenerator(ILogger<RoomDatabaseCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<RoomFile>> GenerateRoomFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<RoomFile>();

            try
            {
                _logger.LogInformation("Generating Room database files for {EntityCount} entities", applicationLogic.Entities.Count);

                // Generate database class
                var databaseFile = new RoomFile
                {
                    FileName = "AppDatabase.kt",
                    FilePath = "database/AppDatabase.kt",
                    Content = GenerateDatabaseCode(applicationLogic),
                    FileType = RoomFileType.Database
                };
                files.Add(databaseFile);

                // Generate entities
                foreach (var entity in applicationLogic.Entities)
                {
                    var entityFile = new RoomFile
                    {
                        FileName = $"{entity}Entity.kt",
                        FilePath = $"database/{entity}Entity.kt",
                        Content = GenerateEntityCode(entity),
                        FileType = RoomFileType.Entity
                    };
                    files.Add(entityFile);
                }

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Room database files");
                return files;
            }
        }

        private string GenerateDatabaseCode(StandardizedApplicationLogic applicationLogic)
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
)";
        }
    }
}
