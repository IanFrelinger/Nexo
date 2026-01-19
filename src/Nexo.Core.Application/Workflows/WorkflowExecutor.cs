using System.Reactive.Subjects;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;
using Nexo.Core.Domain.Workflows;

namespace Nexo.Core.Application.Workflows;

/// <summary>
/// Executes a user-composed workflow from the visual composer.
/// </summary>
public class WorkflowExecutor
{
    private readonly IAgentRegistry _agents;
    private readonly IBrickRegistry _bricks;
    private readonly IBehaviorRegistry _behaviors;
    private readonly IBehaviorExecutor _behaviorExecutor;
    private readonly ITextFileSystem _fs;
    private readonly ILogger<WorkflowExecutor> _logger;
    private readonly Subject<WorkflowExecutionEvent> _events = new();
    
    public IObservable<WorkflowExecutionEvent> Events => _events.AsObservable();
    
    public WorkflowExecutor(
        IAgentRegistry agents,
        IBrickRegistry bricks,
        IBehaviorRegistry behaviors,
        IBehaviorExecutor behaviorExecutor,
        ITextFileSystem fs,
        ILogger<WorkflowExecutor> logger)
    {
        _agents = agents;
        _bricks = bricks;
        _behaviors = behaviors;
        _behaviorExecutor = behaviorExecutor;
        _fs = fs;
        _logger = logger;
    }
    
    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowDefinition workflow,
        WorkflowInput input,
        CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var context = new WorkflowExecutionContext(correlationId, workflow);
        
        _events.OnNext(new WorkflowStartedEvent(correlationId, workflow.Name));
        
        try
        {
            // Validate the workflow
            var validation = ValidateWorkflow(workflow);
            if (!validation.IsValid)
            {
                throw new WorkflowValidationException(validation.Errors);
            }
            
            // Build execution plan (topological sort)
            var plan = BuildExecutionPlan(workflow);
            _events.OnNext(new ExecutionPlanCreatedEvent(correlationId, plan));
            
            // Execute nodes in order
            var nodeResults = new Dictionary<string, NodeResult>();
            
            foreach (var nodeId in plan.ExecutionOrder)
            {
                var node = workflow.Nodes.First(n => n.Id == nodeId);
                
                _events.OnNext(new NodeStartedEvent(correlationId, node));
                
                // Gather inputs from connected nodes
                var nodeInputs = GatherInputs(workflow, node, nodeResults);
                
                // Execute the node
                var result = await ExecuteNodeAsync(node, nodeInputs, context, ct);
                
                nodeResults[nodeId] = result;
                
                _events.OnNext(new NodeCompletedEvent(correlationId, node, result));
            }
            
            // Gather final outputs
            var outputs = GatherOutputs(workflow, nodeResults);
            
            var workflowResult = new WorkflowResult
            {
                CorrelationId = correlationId,
                Success = true,
                Outputs = outputs,
                NodeResults = nodeResults,
                Metrics = context.Metrics
            };
            
            _events.OnNext(new WorkflowCompletedEvent(correlationId, workflowResult));
            
            return workflowResult;
        }
        catch (Exception ex)
        {
            _events.OnNext(new WorkflowFailedEvent(correlationId, ex));
            throw;
        }
    }
    
    private async Task<NodeResult> ExecuteNodeAsync(
        WorkflowNode node,
        Dictionary<string, object> inputs,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        return node switch
        {
            InputNode inputNode => await ExecuteInputNodeAsync(inputNode, context, ct),
            AgentNode agentNode => await ExecuteAgentNodeAsync(agentNode, inputs, context, ct),
            BrickNode brickNode => await ExecuteBrickNodeAsync(brickNode, inputs, context, ct),
            ClusterNode clusterNode => await ExecuteClusterNodeAsync(clusterNode, inputs, context, ct),
            TransformNode transformNode => ExecuteTransformNode(transformNode, inputs),
            ConditionalNode conditionalNode => ExecuteConditionalNode(conditionalNode, inputs),
            OutputNode outputNode => await ExecuteOutputNodeAsync(outputNode, inputs, context, ct),
            _ => throw new InvalidOperationException($"Unknown node type: {node.GetType().Name}")
        };
    }
    
    private async Task<NodeResult> ExecuteInputNodeAsync(
        InputNode node,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        object? data = null;
        
        switch (node.Type)
        {
            case InputType.File:
                if (node.FilePath != null)
                {
                    data = await _fs.ReadAllTextAsync(node.FilePath, ct);
                }
                break;
            case InputType.Content:
                data = node.Content;
                break;
            case InputType.Webhook:
                // TODO: Implement webhook input
                throw new NotImplementedException("Webhook input not yet implemented");
            case InputType.Database:
                // TODO: Implement database input
                throw new NotImplementedException("Database input not yet implemented");
        }
        
        return new NodeResult
        {
            NodeId = node.Id,
            Success = true,
            Outputs = new Dictionary<string, object> { ["data"] = data ?? "" }
        };
    }
    
    private async Task<NodeResult> ExecuteAgentNodeAsync(
        AgentNode node,
        Dictionary<string, object> inputs,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        var agent = _agents.GetAgent(node.AgentId);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent not found: {node.AgentId}");
        }
        
        // Get the first behavior for now (in future, allow selection)
        var behaviorId = agent.Behaviors.FirstOrDefault();
        if (behaviorId == null)
        {
            throw new InvalidOperationException($"Agent {node.AgentId} has no behaviors");
        }
        
        var behavior = _behaviors.GetBehavior(behaviorId);
        if (behavior == null)
        {
            throw new InvalidOperationException($"Behavior not found: {behaviorId}");
        }
        
        // Create execution options with implementation overrides
        var options = new ExecutionOptions
        {
            IsAirGapped = context.ExecutionContext.IsAirGapped,
            AuditMode = context.ExecutionContext.AuditMode,
            Provider = context.ExecutionContext.Provider,
            ImplementationMode = node.Mode,
            BehaviorOverrides = node.BehaviorOverrides,
            BrickOverrides = node.BrickOverrides
        };
        
        // Stream brick-level events
        var behaviorInput = new BehaviorInput(new Dictionary<string, object>(inputs));
        
        var outputs = new Dictionary<string, object>();
        await foreach (var evt in _behaviorExecutor.ExecuteWithEventsAsync(
            agent, behavior, behaviorInput, options, ct))
        {
            // Forward events to workflow events
            _events.OnNext(new NodeBrickEvent(context.CorrelationId, node.Id, evt));
            
            if (evt is BehaviorCompletedEvent completed)
            {
                outputs = ToMutableDictionary(completed.Outputs);
            }
        }
        
        return new NodeResult
        {
            NodeId = node.Id,
            Success = true,
            Outputs = outputs
        };
    }
    
    private async Task<NodeResult> ExecuteBrickNodeAsync(
        BrickNode node,
        Dictionary<string, object> inputs,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        var brick = _bricks.GetBrick(node.BrickId);
        if (brick == null)
        {
            throw new InvalidOperationException($"Brick not found: {node.BrickId}");
        }

        // Use the same swap-on-failure/fallback semantics as BehaviorExecutor.
        var preferred = node.Implementation == ImplementationType.Auto ? brick.DefaultImplementation : node.Implementation;
        var chain = BuildBrickExecutionChain(brick, preferred, context.ExecutionContext);

        var brickInput = new BrickInput(inputs);
        BrickOutput? result = null;
        Exception? last = null;

        foreach (var impl in chain)
        {
            _events.OnNext(new BrickImplementationSelectedEvent(
                context.CorrelationId, node.Id, brick.Name, impl));

            try
            {
                result = await brick.ExecuteAsync(brickInput, impl, context.ExecutionContext, ct);
                break;
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        if (result == null)
        {
            throw last ?? new InvalidOperationException("Brick execution failed");
        }
        
        return new NodeResult
        {
            NodeId = node.Id,
            Success = true,
            Outputs = ToMutableDictionary(result.ToDictionary())
        };
    }

    private static IReadOnlyList<ImplementationType> BuildBrickExecutionChain(
        Brick brick,
        ImplementationType preferred,
        IExecutionContext ctx)
    {
        if (ctx.IsAirGapped)
        {
            return brick.Implementations.HasDeterministic ? new[] { ImplementationType.Deterministic } : Array.Empty<ImplementationType>();
        }

        var chain = new List<ImplementationType>();
        if (preferred != ImplementationType.Auto) chain.Add(preferred);
        foreach (var f in brick.FallbackChain)
        {
            if (!chain.Contains(f)) chain.Add(f);
        }
        if (chain.Count == 0) chain.Add(brick.DefaultImplementation);

        bool Available(ImplementationType t) => t switch
        {
            ImplementationType.Deterministic => brick.Implementations.HasDeterministic,
            // Provider availability is enforced by the brick/provider path itself; on failure we fall back.
            ImplementationType.Agentic => brick.Implementations.HasAgentic,
            _ => false
        };

        return chain.Where(Available).ToList();
    }
    
    private Task<NodeResult> ExecuteClusterNodeAsync(
        ClusterNode node,
        Dictionary<string, object> inputs,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        // TODO: Implement cluster execution
        throw new NotImplementedException("Cluster execution not yet implemented");
    }
    
    private NodeResult ExecuteTransformNode(
        TransformNode node,
        Dictionary<string, object> inputs)
    {
        // TODO: Implement transform operations
        throw new NotImplementedException("Transform operations not yet implemented");
    }
    
    private NodeResult ExecuteConditionalNode(
        ConditionalNode node,
        Dictionary<string, object> inputs)
    {
        // TODO: Implement conditional branching
        throw new NotImplementedException("Conditional branching not yet implemented");
    }
    
    private async Task<NodeResult> ExecuteOutputNodeAsync(
        OutputNode node,
        Dictionary<string, object> inputs,
        WorkflowExecutionContext context,
        CancellationToken ct)
    {
        var data = inputs.Values.FirstOrDefault();
        
        switch (node.Type)
        {
            case OutputType.Display:
                // Data will be returned in workflow result
                break;
            case OutputType.File:
                if (node.FilePath != null && data != null)
                {
                    var content = SerializeOutput(data, node.Format);
                    await _fs.WriteAllTextAsync(node.FilePath, content, ct);
                }
                break;
            case OutputType.Webhook:
                // TODO: Implement webhook output
                throw new NotImplementedException("Webhook output not yet implemented");
            case OutputType.Database:
                // TODO: Implement database output
                throw new NotImplementedException("Database output not yet implemented");
        }
        
        return new NodeResult
        {
            NodeId = node.Id,
            Success = true,
            Outputs = new Dictionary<string, object> { ["output"] = data ?? "" }
        };
    }
    
    private ExecutionPlan BuildExecutionPlan(WorkflowDefinition workflow)
    {
        // Topological sort of nodes based on connections
        var graph = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();
        
        foreach (var node in workflow.Nodes)
        {
            graph[node.Id] = new List<string>();
            inDegree[node.Id] = 0;
        }
        
        foreach (var connection in workflow.Connections)
        {
            graph[connection.FromNodeId].Add(connection.ToNodeId);
            inDegree[connection.ToNodeId]++;
        }
        
        var queue = new Queue<string>();
        foreach (var kvp in inDegree)
        {
            var nodeId = kvp.Key;
            var degree = kvp.Value;
            if (degree == 0)
                queue.Enqueue(nodeId);
        }
        
        var order = new List<string>();
        while (queue.Count > 0)
        {
            var nodeId = queue.Dequeue();
            order.Add(nodeId);
            
            foreach (var neighbor in graph[nodeId])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }
        
        if (order.Count != workflow.Nodes.Count)
            throw new WorkflowValidationException(new List<string> { "Workflow contains cycles" });
        
        return new ExecutionPlan
        {
            ExecutionOrder = order,
            ParallelGroups = IdentifyParallelGroups(order, workflow)
        };
    }
    
    private Dictionary<string, object> GatherInputs(
        WorkflowDefinition workflow,
        WorkflowNode node,
        Dictionary<string, NodeResult> nodeResults)
    {
        var inputs = new Dictionary<string, object>();
        
        // Find all connections to this node
        var incomingConnections = workflow.Connections
            .Where(c => c.ToNodeId == node.Id)
            .ToList();
        
        foreach (var connection in incomingConnections)
        {
            if (nodeResults.TryGetValue(connection.FromNodeId, out var fromResult))
            {
                // Get the output from the source node
                var port = workflow.Nodes
                    .First(n => n.Id == connection.FromNodeId)
                    .Outputs.FirstOrDefault(p => p.Id == connection.FromPortId);
                
                if (port != null && fromResult.Outputs.TryGetValue(port.Name, out var value))
                {
                    var targetPort = node.Inputs.FirstOrDefault(p => p.Id == connection.ToPortId);
                    if (targetPort != null)
                    {
                        inputs[targetPort.Name] = value;
                    }
                }
            }
        }
        
        return inputs;
    }

    private static Dictionary<string, object> ToMutableDictionary(IReadOnlyDictionary<string, object> source)
    {
        var dict = new Dictionary<string, object>(source.Count);
        foreach (var kvp in source)
        {
            dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }
    
    private Dictionary<string, object> GatherOutputs(
        WorkflowDefinition workflow,
        Dictionary<string, NodeResult> nodeResults)
    {
        var outputs = new Dictionary<string, object>();
        
        // Find all output nodes
        var outputNodes = workflow.Nodes.OfType<OutputNode>().ToList();
        
        foreach (var outputNode in outputNodes)
        {
            if (nodeResults.TryGetValue(outputNode.Id, out var result))
            {
                outputs[outputNode.Name] = result.Outputs;
            }
        }
        
        return outputs;
    }
    
    private WorkflowValidationResult ValidateWorkflow(WorkflowDefinition workflow)
    {
        var errors = new List<string>();
        
        // Check for cycles (handled in BuildExecutionPlan)
        // Check for type mismatches in connections
        foreach (var connection in workflow.Connections)
        {
            var fromNode = workflow.Nodes.FirstOrDefault(n => n.Id == connection.FromNodeId);
            var toNode = workflow.Nodes.FirstOrDefault(n => n.Id == connection.ToNodeId);
            
            if (fromNode == null || toNode == null)
            {
                errors.Add($"Connection references non-existent node");
                continue;
            }
            
            var fromPort = fromNode.Outputs.FirstOrDefault(p => p.Id == connection.FromPortId);
            var toPort = toNode.Inputs.FirstOrDefault(p => p.Id == connection.ToPortId);
            
            if (fromPort == null || toPort == null)
            {
                errors.Add($"Connection references non-existent port");
                continue;
            }
            
            // Type compatibility check
            if (!IsTypeCompatible(fromPort.DataType, toPort.DataType))
            {
                errors.Add($"Type mismatch: {fromPort.DataType} -> {toPort.DataType}");
            }
        }
        
        return new WorkflowValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
    
    private bool IsTypeCompatible(string fromType, string toType)
    {
        if (fromType == toType) return true;
        if (toType == "any") return true;
        
        // Add more compatibility rules as needed
        return false;
    }
    
    private List<List<string>> IdentifyParallelGroups(
        List<string> executionOrder,
        WorkflowDefinition workflow)
    {
        // Simple implementation: nodes at the same depth can run in parallel
        // More sophisticated analysis could identify truly independent nodes
        var groups = new List<List<string>>();
        var currentGroup = new List<string> { executionOrder[0] };
        
        for (int i = 1; i < executionOrder.Count; i++)
        {
            var nodeId = executionOrder[i];
            var hasDependency = workflow.Connections.Any(c => c.ToNodeId == nodeId);
            
            if (hasDependency)
            {
                groups.Add(currentGroup);
                currentGroup = new List<string> { nodeId };
            }
            else
            {
                currentGroup.Add(nodeId);
            }
        }
        
        if (currentGroup.Count > 0)
            groups.Add(currentGroup);
        
        return groups;
    }
    
    private string SerializeOutput(object data, OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Json => System.Text.Json.JsonSerializer.Serialize(data),
            OutputFormat.Xml => throw new NotImplementedException("XML serialization not implemented"),
            OutputFormat.Csv => throw new NotImplementedException("CSV serialization not implemented"),
            OutputFormat.Markdown => throw new NotImplementedException("Markdown serialization not implemented"),
            OutputFormat.Html => throw new NotImplementedException("HTML serialization not implemented"),
            OutputFormat.Pdf => throw new NotImplementedException("PDF serialization not implemented"),
            _ => data.ToString() ?? ""
        };
    }
}

// Supporting classes and events

public class WorkflowInput
{
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

public class WorkflowResult
{
    public string CorrelationId { get; init; } = "";
    public bool Success { get; init; }
    public Dictionary<string, object> Outputs { get; init; } = new();
    public Dictionary<string, NodeResult> NodeResults { get; init; } = new();
    public WorkflowMetrics Metrics { get; init; } = new();
}

public class NodeResult
{
    public string NodeId { get; init; } = "";
    public bool Success { get; init; }
    public Dictionary<string, object> Outputs { get; init; } = new();
    public NodeMetrics? Metrics { get; init; }
}

public class NodeMetrics
{
    public TimeSpan Duration { get; init; }
    public ImplementationType? Implementation { get; init; }
    public long? TokensUsed { get; init; }
    public bool CacheHit { get; init; }
}

public class WorkflowMetrics
{
    public TimeSpan TotalDuration { get; init; }
    public int NodesExecuted { get; init; }
    public int DeterministicBricks { get; init; }
    public int AgenticBricks { get; init; }
    public long TotalTokensUsed { get; init; }
    public int CacheHits { get; init; }
}

public class ExecutionPlan
{
    public List<string> ExecutionOrder { get; init; } = new();
    public List<List<string>> ParallelGroups { get; init; } = new();
}

public class WorkflowExecutionContext
{
    public string CorrelationId { get; init; }
    public WorkflowDefinition Workflow { get; init; }
    public IExecutionContext ExecutionContext { get; init; }
    public WorkflowMetrics Metrics { get; set; } = new();
    
    public WorkflowExecutionContext(string correlationId, WorkflowDefinition workflow)
    {
        CorrelationId = correlationId;
        Workflow = workflow;
        // Create a simple execution context implementation
        ExecutionContext = new SimpleExecutionContext
        {
            AgentId = "workflow",
            BehaviorId = workflow.Id,
            IsAirGapped = false,
            AuditMode = false,
            Provider = "openai",
            Variables = new Dictionary<string, object>()
        };
    }
}

/// <summary>
/// Simple implementation of IExecutionContext for workflow execution.
/// </summary>
internal class SimpleExecutionContext : IExecutionContext
{
    public string AgentId { get; init; } = "";
    public string BehaviorId { get; init; } = "";
    public bool IsAirGapped { get; init; }
    public bool AuditMode { get; init; }
    public string Provider { get; init; } = "openai";
    public Dictionary<string, object> Variables { get; init; } = new();
    
    IReadOnlyDictionary<string, object> IExecutionContext.Variables => Variables;
}

public class WorkflowValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
}

public class WorkflowValidationException : Exception
{
    public List<string> Errors { get; }
    
    public WorkflowValidationException(List<string> errors) 
        : base($"Workflow validation failed: {string.Join(", ", errors)}")
    {
        Errors = errors;
    }
}

// Events

public abstract record WorkflowExecutionEvent(string CorrelationId, DateTimeOffset Timestamp);

public record WorkflowStartedEvent(string CorrelationId, string WorkflowName) 
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);

public record ExecutionPlanCreatedEvent(string CorrelationId, ExecutionPlan Plan) 
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);

public record NodeStartedEvent(string CorrelationId, WorkflowNode Node) 
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);

public record NodeCompletedEvent(string CorrelationId, WorkflowNode Node, NodeResult Result) 
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);

public record BrickImplementationSelectedEvent(
    string CorrelationId, 
    string NodeId, 
    string BrickName, 
    ImplementationType Implementation) 
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);

public record NodeBrickEvent(
    string CorrelationId, 
    string NodeId, 
    ExecutionEvent BrickEvent) 
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);

public record WorkflowCompletedEvent(string CorrelationId, WorkflowResult Result) 
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);

public record WorkflowFailedEvent(string CorrelationId, Exception Error) 
    : WorkflowExecutionEvent(CorrelationId, DateTimeOffset.UtcNow);
