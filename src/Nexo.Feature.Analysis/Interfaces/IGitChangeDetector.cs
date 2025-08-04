using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Analysis.Interfaces
{
    /// <summary>
    /// Interface for detecting Git changes to enable intelligent test selection.
    /// </summary>
    public interface IGitChangeDetector
    {
        /// <summary>
        /// Gets changed files since a specific commit or reference.
        /// </summary>
        /// <param name="sinceReference">Git reference (commit hash, branch, tag, etc.)</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of changed file paths.</returns>
        Task<List<string>> GetChangedFilesAsync(string sinceReference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets changed files in the working directory (uncommitted changes).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of changed file paths.</returns>
        Task<List<string>> GetUncommittedChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets changed files between two commits.
        /// </summary>
        /// <param name="fromCommit">Starting commit hash.</param>
        /// <param name="toCommit">Ending commit hash.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of changed file paths.</returns>
        Task<List<string>> GetChangedFilesBetweenCommitsAsync(string fromCommit, string toCommit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the current directory is a Git repository.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the directory is a Git repository.</returns>
        Task<bool> IsGitRepositoryAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current branch name.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Current branch name.</returns>
        Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest commit hash.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Latest commit hash.</returns>
        Task<string> GetLatestCommitHashAsync(CancellationToken cancellationToken = default);
    }
}