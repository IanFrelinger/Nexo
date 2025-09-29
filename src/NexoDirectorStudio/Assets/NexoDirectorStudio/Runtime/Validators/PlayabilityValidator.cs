using NexoDirectorStudio.DTO;
using UnityEngine;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validates that a game slice has a path to completion and no soft locks.
    /// Ensures players can progress through the content without getting stuck.
    /// </summary>
    public sealed class PlayabilityValidator : IValidator<InteractionGraph>
    {
        public async ValueTask<ValidationResult> ValidateAsync(InteractionGraph input, CancellationToken ct)
        {
            await Task.Delay(50, ct); // Simulate processing time
            
            var issues = new List<ValidationIssue>();
            var suggestions = new List<ValidationSuggestion>();
            var score = 100;
            
            // Check for entry points
            if (input.EntryPointIds.Count == 0)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Playability",
                    Title = "No Entry Points",
                    Description = "The interaction graph has no entry points, making it unplayable.",
                    Location = "InteractionGraph.EntryPointIds",
                    SuggestedFix = "Add at least one entry point to the interaction graph."
                });
                score -= 50;
            }
            
            // Check for goal nodes
            var goalNodes = input.Nodes.Where(n => n.NodeType == "Goal").ToList();
            if (goalNodes.Count == 0)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Critical,
                    Category = "Playability",
                    Title = "No Goal Nodes",
                    Description = "The interaction graph has no goal nodes, making it impossible to complete.",
                    Location = "InteractionGraph.Nodes",
                    SuggestedFix = "Add at least one goal node to provide a completion condition."
                });
                score -= 40;
            }
            
            // Check for path connectivity
            var connectivityIssues = ValidatePathConnectivity(input);
            issues.AddRange(connectivityIssues);
            score -= connectivityIssues.Count * 10;
            
            // Check for soft locks
            var softLockIssues = ValidateSoftLocks(input);
            issues.AddRange(softLockIssues);
            score -= softLockIssues.Count * 15;
            
            // Check for required interactions
            var requiredInteractionIssues = ValidateRequiredInteractions(input);
            issues.AddRange(requiredInteractionIssues);
            score -= requiredInteractionIssues.Count * 5;
            
            // Generate suggestions
            suggestions.AddRange(GeneratePlayabilitySuggestions(input));
            
            var message = issues.Count == 0 
                ? "Playability validation passed. The game slice has a clear path to completion."
                : $"Playability validation found {issues.Count} issues that may affect gameplay.";
            
            return new ValidationResult
            {
                IsValid = issues.Count == 0 || issues.All(i => i.Severity < ValidationSeverity.Critical),
                Score = Math.Max(0, score),
                Message = message,
                Details = GeneratePlayabilityDetails(input, issues),
                Issues = issues,
                Suggestions = suggestions
            };
        }
        
        public async ValueTask<ValidationResult> ValidateAsync(object input, CancellationToken ct)
        {
            if (input is InteractionGraph graph)
            {
                return await ValidateAsync(graph, ct);
            }
            
            return new ValidationResult
            {
                IsValid = false,
                Score = 0,
                Message = "Invalid input type for playability validation.",
                Details = "PlayabilityValidator requires an InteractionGraph input."
            };
        }
        
        private static IReadOnlyList<ValidationIssue> ValidatePathConnectivity(InteractionGraph graph)
        {
            var issues = new List<ValidationIssue>();
            
            // Check if all entry points have outgoing connections
            foreach (var entryPointId in graph.EntryPointIds)
            {
                var entryNode = graph.Nodes.FirstOrDefault(n => n.Id == entryPointId);
                if (entryNode == null)
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Category = "Playability",
                        Title = "Invalid Entry Point",
                        Description = $"Entry point {entryPointId} does not exist in the interaction graph.",
                        Location = "InteractionGraph.EntryPointIds",
                        SuggestedFix = "Remove invalid entry point or add corresponding node."
                    });
                    continue;
                }
                
                var hasOutgoingConnections = graph.Connections.Any(c => c.SourceNodeId == entryPointId);
                if (!hasOutgoingConnections)
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Category = "Playability",
                        Title = "Isolated Entry Point",
                        Description = $"Entry point {entryPointId} has no outgoing connections.",
                        Location = $"Node.{entryPointId}",
                        SuggestedFix = "Add connections from the entry point to other nodes."
                    });
                }
            }
            
            // Check if goal nodes are reachable
            var goalNodes = graph.Nodes.Where(n => n.NodeType == "Goal").ToList();
            foreach (var goalNode in goalNodes)
            {
                var isReachable = IsNodeReachable(graph, goalNode.Id);
                if (!isReachable)
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Critical,
                        Category = "Playability",
                        Title = "Unreachable Goal",
                        Description = $"Goal node {goalNode.Name} is not reachable from any entry point.",
                        Location = $"Node.{goalNode.Id}",
                        SuggestedFix = "Add connections to make the goal node reachable."
                    });
                }
            }
            
            return issues;
        }
        
        private static IReadOnlyList<ValidationIssue> ValidateSoftLocks(InteractionGraph graph)
        {
            var issues = new List<ValidationIssue>();
            
            // Check for nodes that can only be reached through non-repeatable interactions
            foreach (var node in graph.Nodes)
            {
                if (node.NodeType == "Goal" || node.NodeType == "Spawn")
                    continue;
                
                var incomingConnections = graph.Connections.Where(c => c.TargetNodeId == node.Id).ToList();
                if (incomingConnections.Count == 0)
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Category = "Playability",
                        Title = "Isolated Node",
                        Description = $"Node {node.Name} has no incoming connections and may be unreachable.",
                        Location = $"Node.{node.Id}",
                        SuggestedFix = "Add connections to make the node reachable or remove it if not needed."
                    });
                }
                
                // Check for nodes that require non-repeatable interactions to reach
                var requiredNonRepeatable = incomingConnections
                    .Where(c => !IsSourceNodeRepeatable(graph, c.SourceNodeId))
                    .ToList();
                
                if (requiredNonRepeatable.Count > 0 && !node.IsRepeatable)
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Error,
                        Category = "Playability",
                        Title = "Potential Soft Lock",
                        Description = $"Node {node.Name} may be unreachable after first attempt due to non-repeatable requirements.",
                        Location = $"Node.{node.Id}",
                        SuggestedFix = "Make the node repeatable or ensure alternative paths exist."
                    });
                }
            }
            
            return issues;
        }
        
        private static IReadOnlyList<ValidationIssue> ValidateRequiredInteractions(InteractionGraph graph)
        {
            var issues = new List<ValidationIssue>();
            
            // Check for required collectibles that may be missed
            var collectibleNodes = graph.Nodes.Where(n => n.NodeType == "Collectible").ToList();
            if (collectibleNodes.Count > 0)
            {
                var hasCollectiblePath = HasCollectiblePath(graph, collectibleNodes);
                if (!hasCollectiblePath)
                {
                    issues.Add(new ValidationIssue
                    {
                        Severity = ValidationSeverity.Warning,
                        Category = "Playability",
                        Title = "Collectible Path Issues",
                        Description = "Some collectibles may not be reachable through the main path.",
                        Location = "InteractionGraph.Nodes",
                        SuggestedFix = "Ensure all collectibles are reachable or make them optional."
                    });
                }
            }
            
            return issues;
        }
        
        private static IReadOnlyList<ValidationSuggestion> GeneratePlayabilitySuggestions(InteractionGraph graph)
        {
            var suggestions = new List<ValidationSuggestion>();
            
            // Suggest adding checkpoints for long sequences
            var nodeCount = graph.Nodes.Count;
            if (nodeCount > 10)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Playability",
                    Title = "Add Checkpoints",
                    Description = "Consider adding checkpoint nodes for long interaction sequences to prevent frustration.",
                    Priority = 4,
                    Effort = "Low"
                });
            }
            
            // Suggest adding alternative paths
            var goalNodes = graph.Nodes.Where(n => n.NodeType == "Goal").ToList();
            if (goalNodes.Count == 1)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Playability",
                    Title = "Add Alternative Paths",
                    Description = "Consider adding alternative paths to the goal to increase replayability.",
                    Priority = 3,
                    Effort = "Medium"
                });
            }
            
            // Suggest adding difficulty options
            var difficultyNodes = graph.Nodes.Where(n => n.NodeType == "Hazard" || n.NodeType == "EnemySpawn").ToList();
            if (difficultyNodes.Count > 0)
            {
                suggestions.Add(new ValidationSuggestion
                {
                    Category = "Playability",
                    Title = "Add Difficulty Options",
                    Description = "Consider adding difficulty options to make the content accessible to more players.",
                    Priority = 3,
                    Effort = "Medium"
                });
            }
            
            return suggestions;
        }
        
        private static string GeneratePlayabilityDetails(InteractionGraph graph, IReadOnlyList<ValidationIssue> issues)
        {
            var details = new List<string>
            {
                $"Total Nodes: {graph.Nodes.Count}",
                $"Total Connections: {graph.Connections.Count}",
                $"Entry Points: {graph.EntryPointIds.Count}",
                $"Goal Nodes: {graph.Nodes.Count(n => n.NodeType == "Goal")}",
                $"Collectible Nodes: {graph.Nodes.Count(n => n.NodeType == "Collectible")}",
                $"Repeatable Nodes: {graph.Nodes.Count(n => n.IsRepeatable)}"
            };
            
            if (issues.Count > 0)
            {
                details.Add($"Issues Found: {issues.Count}");
                foreach (var issue in issues)
                {
                    details.Add($"- {issue.Severity}: {issue.Title}");
                }
            }
            
            return string.Join("\n", details);
        }
        
        private static bool IsNodeReachable(InteractionGraph graph, string nodeId)
        {
            var visited = new HashSet<string>();
            var queue = new Queue<string>();
            
            // Start from all entry points
            foreach (var entryPointId in graph.EntryPointIds)
            {
                queue.Enqueue(entryPointId);
            }
            
            while (queue.Count > 0)
            {
                var currentNodeId = queue.Dequeue();
                if (visited.Contains(currentNodeId))
                    continue;
                
                visited.Add(currentNodeId);
                
                if (currentNodeId == nodeId)
                    return true;
                
                // Add connected nodes to queue
                var outgoingConnections = graph.Connections.Where(c => c.SourceNodeId == currentNodeId);
                foreach (var connection in outgoingConnections)
                {
                    queue.Enqueue(connection.TargetNodeId);
                }
            }
            
            return false;
        }
        
        private static bool IsSourceNodeRepeatable(InteractionGraph graph, string sourceNodeId)
        {
            var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == sourceNodeId);
            return sourceNode?.IsRepeatable ?? false;
        }
        
        private static bool HasCollectiblePath(InteractionGraph graph, IReadOnlyList<InteractionNode> collectibleNodes)
        {
            // Simple check: if there are collectibles, ensure they're connected to the main path
            var connectedCollectibles = collectibleNodes.Where(c => 
                graph.Connections.Any(conn => conn.TargetNodeId == c.Id || conn.SourceNodeId == c.Id)
            ).ToList();
            
            return connectedCollectibles.Count == collectibleNodes.Count;
        }
    }
}
