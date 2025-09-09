using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces.Security;
using Nexo.Infrastructure.Services.Security;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.Security
{
    /// <summary>
    /// Tests for secure API key manager.
    /// Part of Phase 3.3 testing and validation.
    /// </summary>
    public class SecureApiKeyManagerTests
    {
        private readonly SecureApiKeyManager _apiKeyManager;

        public SecureApiKeyManagerTests()
        {
            _apiKeyManager = new SecureApiKeyManager();
        }

        [Fact]
        public async Task GenerateApiKeyAsync_WithValidInput_ReturnsApiKeyInfo()
        {
            // Arrange
            var name = "Test API Key";
            var description = "Test description";
            var expiration = TimeSpan.FromDays(30);
            var permissions = new[] { "read", "write" };

            // Act
            var result = await _apiKeyManager.GenerateApiKeyAsync(
                name, description, expiration, permissions);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(name, result.Name);
            Assert.Equal(description, result.Description);
            Assert.True(result.ExpiresAt.HasValue);
            Assert.True(result.ExpiresAt.Value > DateTimeOffset.UtcNow);
            Assert.Equal(2, result.Permissions.Count);
            Assert.Contains("read", result.Permissions);
            Assert.Contains("write", result.Permissions);
            Assert.True(result.IsActive);
            Assert.Equal(0, result.UsageCount);
        }

        [Fact]
        public async Task GenerateApiKeyAsync_WithNullPermissions_ReturnsApiKeyWithEmptyPermissions()
        {
            // Arrange
            var name = "Test API Key";
            var description = "Test description";

            // Act
            var result = await _apiKeyManager.GenerateApiKeyAsync(
                name, description, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(name, result.Name);
            Assert.Equal(description, result.Description);
            Assert.Empty(result.Permissions);
        }

        [Fact]
        public async Task GenerateApiKeyAsync_WithNoExpiration_ReturnsApiKeyWithoutExpiration()
        {
            // Arrange
            var name = "Test API Key";
            var description = "Test description";

            // Act
            var result = await _apiKeyManager.GenerateApiKeyAsync(
                name, description, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.ExpiresAt.HasValue);
        }

        [Fact]
        public async Task ValidateApiKeyAsync_WithValidKey_ReturnsValidResult()
        {
            // Arrange
            var apiKey = await _apiKeyManager.GenerateApiKeyAsync(
                "Test Key", "Test Description", TimeSpan.FromDays(30));

            // Act
            var result = await _apiKeyManager.ValidateApiKeyAsync(apiKey.KeyHash);

            // Assert
            Assert.True(result.IsValid);
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.KeyInfo);
            Assert.Equal(apiKey.Id, result.KeyInfo.Id);
        }

        [Fact]
        public async Task ValidateApiKeyAsync_WithInvalidKey_ReturnsInvalidResult()
        {
            // Arrange
            var invalidKey = "invalid-key-hash";

            // Act
            var result = await _apiKeyManager.ValidateApiKeyAsync(invalidKey);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Invalid API key", result.ErrorMessage);
            Assert.Null(result.KeyInfo);
        }

        [Fact]
        public async Task ValidateApiKeyAsync_WithRequiredPermission_ValidatesPermission()
        {
            // Arrange
            var apiKey = await _apiKeyManager.GenerateApiKeyAsync(
                "Test Key", "Test Description", TimeSpan.FromDays(30), 
                new[] { "read", "write" });

            // Act
            var validResult = await _apiKeyManager.ValidateApiKeyAsync(
                apiKey.KeyHash, "read");
            var invalidResult = await _apiKeyManager.ValidateApiKeyAsync(
                apiKey.KeyHash, "admin");

            // Assert
            Assert.True(validResult.IsValid);
            Assert.False(invalidResult.IsValid);
            Assert.Equal("Insufficient permissions", invalidResult.ErrorMessage);
        }

        [Fact]
        public async Task RevokeApiKeyAsync_WithValidKeyId_ReturnsTrue()
        {
            // Arrange
            var apiKey = await _apiKeyManager.GenerateApiKeyAsync(
                "Test Key", "Test Description");

            // Act
            var result = await _apiKeyManager.RevokeApiKeyAsync(apiKey.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task RevokeApiKeyAsync_WithInvalidKeyId_ReturnsFalse()
        {
            // Arrange
            var invalidKeyId = "invalid-key-id";

            // Act
            var result = await _apiKeyManager.RevokeApiKeyAsync(invalidKeyId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RevokeApiKeyAsync_WithValidKeyId_DeactivatesKey()
        {
            // Arrange
            var apiKey = await _apiKeyManager.GenerateApiKeyAsync(
                "Test Key", "Test Description");

            // Act
            await _apiKeyManager.RevokeApiKeyAsync(apiKey.Id);
            var validationResult = await _apiKeyManager.ValidateApiKeyAsync(apiKey.KeyHash);

            // Assert
            Assert.False(validationResult.IsValid);
            Assert.Equal("API key is inactive", validationResult.ErrorMessage);
        }

        [Fact]
        public async Task ListApiKeysAsync_WithMultipleKeys_ReturnsAllKeys()
        {
            // Arrange
            var key1 = await _apiKeyManager.GenerateApiKeyAsync("Key 1", "Description 1");
            var key2 = await _apiKeyManager.GenerateApiKeyAsync("Key 2", "Description 2");

            // Act
            var keys = await _apiKeyManager.ListApiKeysAsync();

            // Assert
            Assert.True(keys.Count() >= 2);
            Assert.Contains(keys, k => k.Id == key1.Id);
            Assert.Contains(keys, k => k.Id == key2.Id);
        }

        [Fact]
        public async Task GetUsageStatisticsAsync_WithMultipleKeys_ReturnsCorrectStatistics()
        {
            // Arrange
            var key1 = await _apiKeyManager.GenerateApiKeyAsync("Key 1", "Description 1");
            var key2 = await _apiKeyManager.GenerateApiKeyAsync("Key 2", "Description 2");
            await _apiKeyManager.RevokeApiKeyAsync(key2.Id);

            // Act
            var stats = await _apiKeyManager.GetUsageStatisticsAsync();

            // Assert
            Assert.True(stats.TotalKeys >= 2);
            Assert.True(stats.ActiveKeys >= 1);
            Assert.True(stats.RevokedKeys >= 1);
        }

        [Fact]
        public async Task RotateApiKeyAsync_WithValidKeyId_ReturnsNewKey()
        {
            // Arrange
            var originalKey = await _apiKeyManager.GenerateApiKeyAsync(
                "Test Key", "Test Description", TimeSpan.FromDays(30));

            // Act
            var newKey = await _apiKeyManager.RotateApiKeyAsync(originalKey.Id);

            // Assert
            Assert.NotNull(newKey);
            Assert.NotEqual(originalKey.Id, newKey.Id);
            Assert.Equal(originalKey.Name, newKey.Name);
            Assert.Equal(originalKey.Description, newKey.Description);
            Assert.Equal(originalKey.ExpiresAt, newKey.ExpiresAt);
            Assert.Equal(originalKey.Permissions, newKey.Permissions);
        }

        [Fact]
        public async Task RotateApiKeyAsync_WithInvalidKeyId_ThrowsException()
        {
            // Arrange
            var invalidKeyId = "invalid-key-id";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _apiKeyManager.RotateApiKeyAsync(invalidKeyId));
        }

        [Fact]
        public async Task RotateApiKeyAsync_WithValidKeyId_DeactivatesOriginalKey()
        {
            // Arrange
            var originalKey = await _apiKeyManager.GenerateApiKeyAsync(
                "Test Key", "Test Description");

            // Act
            await _apiKeyManager.RotateApiKeyAsync(originalKey.Id);
            var validationResult = await _apiKeyManager.ValidateApiKeyAsync(originalKey.KeyHash);

            // Assert
            Assert.False(validationResult.IsValid);
            Assert.Equal("API key is inactive", validationResult.ErrorMessage);
        }

        [Fact]
        public async Task ValidateApiKeyAsync_WithExpiredKey_ReturnsInvalidResult()
        {
            // Arrange
            var apiKey = await _apiKeyManager.GenerateApiKeyAsync(
                "Test Key", "Test Description", TimeSpan.FromMilliseconds(1));
            
            // Wait for expiration
            await Task.Delay(100);

            // Act
            var result = await _apiKeyManager.ValidateApiKeyAsync(apiKey.KeyHash);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("API key has expired", result.ErrorMessage);
        }

        [Fact]
        public async Task GenerateApiKeyAsync_WithEmptyName_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _apiKeyManager.GenerateApiKeyAsync("", "Description"));
        }

        [Fact]
        public async Task GenerateApiKeyAsync_WithEmptyDescription_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _apiKeyManager.GenerateApiKeyAsync("Name", ""));
        }
    }
}