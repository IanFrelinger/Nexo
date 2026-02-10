namespace Nexo.Guide.Sandbox;

public class SandboxedExecutionEngine
{
    private readonly SandboxPolicy _policy;
    private int _bricksThisTurn;
    private int _currentElementCount;
    private int _currentVertexCount;
    private int _currentParticleCount;

    public SandboxedExecutionEngine(SandboxPolicy policy)
    {
        _policy = policy;
    }

    public void ValidateBrickCall(string brickNamespace, string brickName)
    {
        if (!_policy.AllowedNamespaces.Any(a => brickNamespace.StartsWith(a)))
            throw new SandboxViolationException($"Brick {brickName} not allowed in sandbox.");
        if (++_bricksThisTurn > _policy.MaxBricksPerTurn)
            throw new SandboxViolationException($"Exceeded max bricks per turn ({_policy.MaxBricksPerTurn}).");
        if (_currentElementCount >= _policy.MaxSceneElements)
            throw new SandboxViolationException($"Exceeded max scene elements ({_policy.MaxSceneElements}).");
    }

    public void TrackElement() => _currentElementCount++;
    public void TrackVertices(int count)
    {
        _currentVertexCount += count;
        if (_currentVertexCount > _policy.MaxTotalVertices)
            throw new SandboxViolationException($"Exceeded max vertices ({_policy.MaxTotalVertices}).");
    }
    public void TrackParticles(int count)
    {
        _currentParticleCount += count;
        if (_currentParticleCount > _policy.MaxTotalParticles)
            throw new SandboxViolationException($"Exceeded max particles ({_policy.MaxTotalParticles}).");
    }

    public void ResetTurnBudget() => _bricksThisTurn = 0;
    public void ResetScene()
    {
        _currentElementCount = 0;
        _currentVertexCount = 0;
        _currentParticleCount = 0;
    }
}
