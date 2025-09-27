using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Helper methods for MigrationService.
    /// Contains initialization and utility methods.
    /// </summary>
    public partial class MigrationService
    {
        /// <summary>
        /// Initializes sample migrations for testing and development.
        /// </summary>
        private void InitializeSampleMigrations()
        {
            var migration1 = new MigrationInfo
            {
                Id = "2025.01.26.100000_Initial_Schema",
                Name = "Initial Schema",
                Description = "Create initial database schema",
                Version = "2025.01.26.100000",
                Timestamp = new DateTime(2025, 1, 26, 10, 0, 0, DateTimeKind.Utc),
                IsApplied = false,
                Script = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username VARCHAR(100) NOT NULL UNIQUE,
                        Email VARCHAR(255) NOT NULL UNIQUE,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    CREATE TABLE IF NOT EXISTS Projects (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name VARCHAR(255) NOT NULL,
                        Description TEXT,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );",
                RollbackScript = @"
                    DROP TABLE IF EXISTS Projects;
                    DROP TABLE IF EXISTS Users;",
                Dependencies = new List<string>()
            };

            var migration2 = new MigrationInfo
            {
                Id = "2025.01.26.110000_Add_User_Roles",
                Name = "Add User Roles",
                Description = "Add role-based access control",
                Version = "2025.01.26.110000",
                Timestamp = new DateTime(2025, 1, 26, 11, 0, 0, DateTimeKind.Utc),
                IsApplied = false,
                Script = @"
                    ALTER TABLE Users ADD COLUMN Role VARCHAR(50) DEFAULT 'User';
                    CREATE TABLE IF NOT EXISTS Roles (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name VARCHAR(50) NOT NULL UNIQUE,
                        Description TEXT
                    );
                    
                    INSERT INTO Roles (Name, Description) VALUES 
                        ('Admin', 'Administrator with full access'),
                        ('User', 'Standard user with limited access'),
                        ('Guest', 'Guest user with read-only access');",
                RollbackScript = @"
                    DROP TABLE IF EXISTS Roles;
                    ALTER TABLE Users DROP COLUMN Role;",
                Dependencies = new List<string> { "2025.01.26.100000_Initial_Schema" }
            };

            _migrations[migration1.Id] = migration1;
            _migrations[migration2.Id] = migration2;
        }
    }
}
