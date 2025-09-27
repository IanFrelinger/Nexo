using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using System;
using System.Collections.Generic;

namespace Nexo.Feature.Platform.Tests.Services
{
    /// <summary>
    /// Helper methods for Android code generator tests.
    /// </summary>
    public partial class AndroidCodeGeneratorTests
    {
        // Helper methods
        private StandardizedApplicationLogic CreateValidApplicationLogic()
        {
            return new StandardizedApplicationLogic
            {
                Patterns = new List<ApplicationPattern>
                {
                    new ApplicationPattern
                    {
                        Name = "Repository",
                        Type = PatternType.Repository,
                        Implementation = "Repository pattern implementation"
                    },
                    new ApplicationPattern
                    {
                        Name = "UnitOfWork",
                        Type = PatternType.UnitOfWork,
                        Implementation = "Unit of Work pattern implementation"
                    }
                },
                SecurityPatterns = new List<SecurityPattern>
                {
                    new SecurityPattern
                    {
                        Name = "JWT",
                        Type = SecurityPatternType.Jwt,
                        Implementation = "JWT authentication"
                    }
                },
                StateManagementPatterns = new List<StateManagementPattern>
                {
                    new StateManagementPattern
                    {
                        Name = "GlobalState",
                        Type = StateManagementType.GlobalState,
                        Implementation = "Global state management"
                    }
                },
                ApiContracts = new List<ApiContract>
                {
                    new ApiContract
                    {
                        Name = "UserAPI",
                        Method = Nexo.Feature.AI.Models.HttpMethod.Get,
                        Endpoint = "/api/users",
                        Parameters = new List<ApiParameter>(),
                        Response = new ApiResponse()
                    }
                },
                DataFlowPatterns = new List<DataFlowPattern>
                {
                    new DataFlowPattern
                    {
                        Name = "Unidirectional",
                        Type = DataFlowType.Unidirectional,
                        Implementation = "Unidirectional data flow"
                    }
                },
                CachingStrategies = new List<CachingStrategy>
                {
                    new CachingStrategy
                    {
                        Name = "MemoryCache",
                        Type = CachingStrategyType.MemoryCache,
                        Implementation = "In-memory caching"
                    }
                }
            };
        }

        private StandardizedApplicationLogic CreateEmptyApplicationLogic()
        {
            return new StandardizedApplicationLogic
            {
                Patterns = new List<ApplicationPattern>(),
                SecurityPatterns = new List<SecurityPattern>(),
                StateManagementPatterns = new List<StateManagementPattern>(),
                ApiContracts = new List<ApiContract>(),
                DataFlowPatterns = new List<DataFlowPattern>(),
                CachingStrategies = new List<CachingStrategy>()
            };
        }

        private AndroidGeneratedCode CreateValidAndroidGeneratedCode()
        {
            return new AndroidGeneratedCode
            {
                ComposeFiles = new List<ComposeFile>
                {
                    new ComposeFile
                    {
                        FileName = "MainScreen.kt",
                        Content = "package com.example.app\n\n@Composable\nfun MainScreen() { }",
                        ViewType = ComposeViewType.Screen
                    }
                },
                RoomFiles = new List<RoomFile>
                {
                    new RoomFile
                    {
                        FileName = "AppDatabase.kt",
                        Content = "package com.example.app\n\n@Database(entities = [], version = 1)\nabstract class AppDatabase : RoomDatabase() { }",
                        FileType = RoomFileType.Database
                    }
                },
                AppConfiguration = new AndroidAppConfiguration
                {
                    AppName = "TestApp",
                    PackageName = "com.example.testapp"
                }
            };
        }

        private AndroidGeneratedCode CreateInvalidAndroidGeneratedCode()
        {
            return new AndroidGeneratedCode
            {
                ComposeFiles = new List<ComposeFile>
                {
                    new ComposeFile
                    {
                        FileName = "EmptyScreen.kt",
                        Content = "", // Empty content will cause validation error
                        ViewType = ComposeViewType.Screen
                    }
                },
                RoomFiles = new List<RoomFile>
                {
                    new RoomFile
                    {
                        FileName = "EmptyDatabase.kt",
                        Content = "", // Empty content will cause validation error
                        FileType = RoomFileType.Database
                    }
                }
                // Missing AppConfiguration will cause validation error
            };
        }
    }
}
