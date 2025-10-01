using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Validation tests for CI/CD workflows and package structure.
    /// </summary>
    [TestFixture]
    public class Validation_CICD
    {
        [Test]
        public void GitHubWorkflows_ShouldExist()
        {
            // Arrange
            var workflowDir = Path.Combine("..", "..", "..", "..", "..", ".github", "workflows");
            var buildWorkflow = Path.Combine(workflowDir, "build.yml");
            var testsWorkflow = Path.Combine(workflowDir, "tests.yml");
            var analyzersWorkflow = Path.Combine(workflowDir, "analyzers.yml");
            var coverageWorkflow = Path.Combine(workflowDir, "coverage.yml");
            
            // Assert
            Assert.IsTrue(File.Exists(buildWorkflow), "build.yml workflow should exist");
            Assert.IsTrue(File.Exists(testsWorkflow), "tests.yml workflow should exist");
            Assert.IsTrue(File.Exists(analyzersWorkflow), "analyzers.yml workflow should exist");
            Assert.IsTrue(File.Exists(coverageWorkflow), "coverage.yml workflow should exist");
        }
        
        [Test]
        public void PackageJson_ShouldExist()
        {
            // Arrange
            var packageJsonPath = Path.Combine("..", "..", "..", "package.json");
            
            // Assert
            Assert.IsTrue(File.Exists(packageJsonPath), "package.json should exist");
        }
        
        [Test]
        public void PackageJson_ShouldHaveRequiredFields()
        {
            // Arrange
            var packageJsonPath = Path.Combine("..", "..", "..", "package.json");
            
            // Act
            var packageJsonContent = File.ReadAllText(packageJsonPath);
            
            // Assert
            Assert.IsTrue(packageJsonContent.Contains("\"name\""), "package.json should have name field");
            Assert.IsTrue(packageJsonContent.Contains("\"version\""), "package.json should have version field");
            Assert.IsTrue(packageJsonContent.Contains("\"displayName\""), "package.json should have displayName field");
            Assert.IsTrue(packageJsonContent.Contains("\"description\""), "package.json should have description field");
            Assert.IsTrue(packageJsonContent.Contains("\"unity\""), "package.json should have unity field");
            Assert.IsTrue(packageJsonContent.Contains("\"dependencies\""), "package.json should have dependencies field");
        }
        
        [Test]
        public void README_ShouldExist()
        {
            // Arrange
            var readmePath = Path.Combine("..", "..", "..", "README.md");
            
            // Assert
            Assert.IsTrue(File.Exists(readmePath), "README.md should exist");
        }
        
        [Test]
        public void README_ShouldHaveRequiredSections()
        {
            // Arrange
            var readmePath = Path.Combine("..", "..", "..", "README.md");
            
            // Act
            var readmeContent = File.ReadAllText(readmePath);
            
            // Assert
            Assert.IsTrue(readmeContent.Contains("# Director Studio"), "README should have title");
            Assert.IsTrue(readmeContent.Contains("## Features"), "README should have features section");
            Assert.IsTrue(readmeContent.Contains("## Quick Start"), "README should have quick start section");
            Assert.IsTrue(readmeContent.Contains("## Installation"), "README should have installation section");
            Assert.IsTrue(readmeContent.Contains("## Configuration"), "README should have configuration section");
        }
        
        [Test]
        public void APIReference_ShouldExist()
        {
            // Arrange
            var apiRefPath = Path.Combine("..", "..", "..", "docs", "API_Reference.md");
            
            // Assert
            Assert.IsTrue(File.Exists(apiRefPath), "API_Reference.md should exist");
        }
        
        [Test]
        public void APIReference_ShouldHaveRequiredSections()
        {
            // Arrange
            var apiRefPath = Path.Combine("..", "..", "..", "docs", "API_Reference.md");
            
            // Act
            var apiRefContent = File.ReadAllText(apiRefPath);
            
            // Assert
            Assert.IsTrue(apiRefContent.Contains("# Director Studio API Reference"), "API reference should have title");
            Assert.IsTrue(apiRefContent.Contains("## Core Interfaces"), "API reference should have core interfaces section");
            Assert.IsTrue(apiRefContent.Contains("## Data Transfer Objects"), "API reference should have DTOs section");
            Assert.IsTrue(apiRefContent.Contains("## Adapters"), "API reference should have adapters section");
            Assert.IsTrue(apiRefContent.Contains("## Commands"), "API reference should have commands section");
        }
        
        [Test]
        public void AssemblyDefinitions_ShouldExist()
        {
            // Arrange
            var runtimeAsmdef = Path.Combine("..", "..", "..", "Runtime", "NexoDirectorStudio.Runtime.asmdef");
            var editorAsmdef = Path.Combine("..", "..", "..", "Editor", "NexoDirectorStudio.Editor.asmdef");
            
            // Assert
            Assert.IsTrue(File.Exists(runtimeAsmdef), "Runtime assembly definition should exist");
            Assert.IsTrue(File.Exists(editorAsmdef), "Editor assembly definition should exist");
        }
        
        [Test]
        public void TestAssemblies_ShouldExist()
        {
            // Arrange
            var editModeAsmdef = Path.Combine("..", "..", "..", "Tests", "EditMode", "NexoDirectorStudio.Tests.EditMode.asmdef");
            var playModeAsmdef = Path.Combine("..", "..", "..", "Tests", "PlayMode", "NexoDirectorStudio.Tests.PlayMode.asmdef");
            
            // Assert
            Assert.IsTrue(File.Exists(editModeAsmdef), "EditMode test assembly definition should exist");
            Assert.IsTrue(File.Exists(playModeAsmdef), "PlayMode test assembly definition should exist");
        }
        
        [Test]
        public void TestFiles_ShouldExist()
        {
            // Arrange
            var testDir = Path.Combine("..", "..", "..", "Tests");
            var editModeDir = Path.Combine(testDir, "EditMode");
            var playModeDir = Path.Combine(testDir, "PlayMode");
            
            // Act
            var editModeFiles = Directory.GetFiles(editModeDir, "*.cs");
            var playModeFiles = Directory.GetFiles(playModeDir, "*.cs");
            
            // Assert
            Assert.IsTrue(editModeFiles.Length > 0, "Should have EditMode test files");
            Assert.IsTrue(playModeFiles.Length > 0, "Should have PlayMode test files");
        }
        
        [Test]
        public void TestFiles_ShouldHaveCorrectCategories()
        {
            // Arrange
            var testDir = Path.Combine("..", "..", "..", "Tests");
            var editModeDir = Path.Combine(testDir, "EditMode");
            
            // Act
            var testFiles = Directory.GetFiles(editModeDir, "*.cs");
            var unitTests = testFiles.Where(f => Path.GetFileName(f).Contains("Unit_")).Count();
            var integrationTests = testFiles.Where(f => Path.GetFileName(f).Contains("Integration_")).Count();
            var smokeTests = testFiles.Where(f => Path.GetFileName(f).Contains("Smoke_")).Count();
            var validationTests = testFiles.Where(f => Path.GetFileName(f).Contains("Validation_")).Count();
            
            // Assert
            Assert.IsTrue(unitTests > 0, "Should have unit tests");
            Assert.IsTrue(integrationTests > 0, "Should have integration tests");
            Assert.IsTrue(smokeTests > 0, "Should have smoke tests");
            Assert.IsTrue(validationTests > 0, "Should have validation tests");
        }
        
        [Test]
        public void WorkflowFiles_ShouldHaveCorrectContent()
        {
            // Arrange
            var workflowDir = Path.Combine("..", "..", "..", "..", "..", ".github", "workflows");
            var buildWorkflow = Path.Combine(workflowDir, "build.yml");
            
            // Act
            var buildContent = File.ReadAllText(buildWorkflow);
            
            // Assert
            Assert.IsTrue(buildContent.Contains("name: Build Director Studio"), "Build workflow should have correct name");
            Assert.IsTrue(buildContent.Contains("on:"), "Build workflow should have trigger");
            Assert.IsTrue(buildContent.Contains("jobs:"), "Build workflow should have jobs");
            Assert.IsTrue(buildContent.Contains("build:"), "Build workflow should have build job");
        }
        
        [Test]
        public void PackageStructure_ShouldBeCorrect()
        {
            // Arrange
            var packageDir = Path.Combine("..", "..", "..");
            var runtimeDir = Path.Combine(packageDir, "Runtime");
            var editorDir = Path.Combine(packageDir, "Editor");
            var testsDir = Path.Combine(packageDir, "Tests");
            var docsDir = Path.Combine(packageDir, "docs");
            
            // Assert
            Assert.IsTrue(Directory.Exists(runtimeDir), "Runtime directory should exist");
            Assert.IsTrue(Directory.Exists(editorDir), "Editor directory should exist");
            Assert.IsTrue(Directory.Exists(testsDir), "Tests directory should exist");
            Assert.IsTrue(Directory.Exists(docsDir), "docs directory should exist");
        }
        
        [Test]
        public void RuntimeComponents_ShouldExist()
        {
            // Arrange
            var runtimeDir = Path.Combine("..", "..", "..", "Runtime");
            var adaptersDir = Path.Combine(runtimeDir, "Adapters");
            var commandsDir = Path.Combine(runtimeDir, "Commands");
            var dtoDir = Path.Combine(runtimeDir, "DTO");
            var orchestrationDir = Path.Combine(runtimeDir, "Orchestration");
            var policiesDir = Path.Combine(runtimeDir, "Policies");
            var profilesDir = Path.Combine(runtimeDir, "Profiles");
            var validatorsDir = Path.Combine(runtimeDir, "Validators");
            
            // Assert
            Assert.IsTrue(Directory.Exists(adaptersDir), "Adapters directory should exist");
            Assert.IsTrue(Directory.Exists(commandsDir), "Commands directory should exist");
            Assert.IsTrue(Directory.Exists(dtoDir), "DTO directory should exist");
            Assert.IsTrue(Directory.Exists(orchestrationDir), "Orchestration directory should exist");
            Assert.IsTrue(Directory.Exists(policiesDir), "Policies directory should exist");
            Assert.IsTrue(Directory.Exists(profilesDir), "Profiles directory should exist");
            Assert.IsTrue(Directory.Exists(validatorsDir), "Validators directory should exist");
        }
    }
}
