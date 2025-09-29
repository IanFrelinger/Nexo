# Security Architecture

## Overview

Nexo is designed with security as a first-class concern, especially given its "agent-first" architecture where AI agents execute tools and make changes to the system. This document outlines the security model, threat analysis, and protective measures.

## Security Model

### Agent Safety Rails

Nexo implements multiple layers of security to ensure agents operate safely:

1. **Policy Enforcement**: All tool executions are validated against security policies
2. **Path Restrictions**: File operations are restricted to allowed directories
3. **Resource Limits**: CPU, memory, and disk usage are limited
4. **Audit Logging**: All agent actions are logged for security auditing

### Trust Boundaries

```
┌─────────────────────────────────────────────────────────────┐
│                    User/Developer                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Configuration │  │   Policies      │  │   Monitoring│  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                  Policy Engine                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   Path Allowlist│  │   Size Limits   │  │   Build     │  │
│  │   Policy        │  │   Policy        │  │   Validation│  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                    AI Agents                               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   DevDirector   │  │   TDD Agent     │  │   Other     │  │
│  │   Agent         │  │                 │  │   Agents    │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                    Tools Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │   File Tools    │  │   Build Tools   │  │   Git Tools │  │
│  │                 │  │                 │  │             │  │
│  └─────────────────┘  └─────────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Threat Analysis

### Attack Vectors

#### 1. Malicious Tool Execution
- **Risk**: Tools that attempt to access restricted files or execute dangerous commands
- **Mitigation**: Policy enforcement, path allowlists, command validation

#### 2. Path Traversal Attacks
- **Risk**: Tools attempting to access files outside allowed directories (e.g., `../../../etc/passwd`)
- **Mitigation**: Path allowlist policy with strict validation

#### 3. Resource Exhaustion
- **Risk**: Tools that consume excessive CPU, memory, or disk space
- **Mitigation**: Resource limits, timeout enforcement, size restrictions

#### 4. Code Injection
- **Risk**: Malicious code injected through tool parameters
- **Mitigation**: Input validation, parameter sanitization, sandboxing

#### 5. Data Exfiltration
- **Risk**: Agents attempting to read sensitive files or send data externally
- **Mitigation**: Path restrictions, network monitoring, audit logging

### Threat Scenarios

#### Scenario 1: Malicious File Access
```
Agent attempts to read /etc/passwd
↓
PathAllowlist policy checks path
↓
Path not in allowlist → REJECT
↓
Audit log: "Tool execution blocked: path not allowed"
```

#### Scenario 2: Large File Write
```
Agent attempts to write 1GB file
↓
MaxWriteSize policy checks size
↓
Size exceeds limit → REJECT
↓
Audit log: "Tool execution blocked: file too large"
```

#### Scenario 3: Build Bypass
```
Agent attempts to commit without building
↓
BuildMustPassBeforeCommit policy checks
↓
No build executed → REJECT
↓
Audit log: "Tool execution blocked: build required"
```

## Security Policies

### PathAllowlist Policy

**Purpose**: Restrict file operations to safe directories

**Configuration**:
```csharp
var policy = new PathAllowlist(new[] { 
    "./src/", 
    "./tests/", 
    "./docs/" 
});
```

**Protection**:
- Prevents access to system directories
- Blocks path traversal attacks
- Configurable allowlist per environment

### MaxWriteSize Policy

**Purpose**: Limit file write sizes to prevent resource exhaustion

**Configuration**:
```csharp
var policy = new MaxWriteSize(maxBytes: 1024 * 1024); // 1MB
```

**Protection**:
- Prevents large file writes
- Reduces disk usage
- Prevents DoS attacks

### BuildMustPassBeforeCommit Policy

**Purpose**: Ensure code quality before commits

**Configuration**:
```csharp
var policy = new BuildMustPassBeforeCommit();
```

**Protection**:
- Prevents broken code commits
- Ensures build integrity
- Maintains code quality

## Audit Logging

### Event Schema

```csharp
public record SecurityEvent(
    DateTimeOffset Timestamp,
    string AgentId,
    string ToolId,
    string Action,
    string Path,
    SecurityEventType Type,
    bool Allowed,
    string Reason
);

public enum SecurityEventType
{
    ToolExecution,
    PolicyViolation,
    ResourceLimit,
    PathAccess,
    BuildValidation
}
```

### Log Examples

```json
{
  "timestamp": "2024-09-28T10:30:00Z",
  "agentId": "DevDirectorAgent",
  "toolId": "RepoFsWriteTool",
  "action": "WriteFile",
  "path": "../../../etc/passwd",
  "type": "PathAccess",
  "allowed": false,
  "reason": "Path not in allowlist"
}
```

### Monitoring

- **Real-time Alerts**: Policy violations trigger immediate alerts
- **Dashboard**: Security events displayed in monitoring dashboard
- **Reports**: Regular security reports for administrators

## Security Best Practices

### For Developers

1. **Enable All Policies**: Always use security policies in production
2. **Restrict Paths**: Use minimal path allowlists
3. **Monitor Logs**: Regularly review security audit logs
4. **Update Regularly**: Keep Nexo and dependencies updated
5. **Test Policies**: Verify policies work as expected

### For Administrators

1. **Review Permissions**: Regularly audit agent permissions
2. **Monitor Resources**: Watch for resource usage patterns
3. **Backup Logs**: Ensure audit logs are backed up
4. **Incident Response**: Have a plan for security incidents
5. **Training**: Train team on security practices

## Configuration Examples

### Development Environment

```csharp
var policies = new[]
{
    new PathAllowlist(new[] { "./src/", "./tests/", "./docs/" }),
    new MaxWriteSize(10 * 1024 * 1024), // 10MB
    new BuildMustPassBeforeCommit()
};
```

### Production Environment

```csharp
var policies = new[]
{
    new PathAllowlist(new[] { "./app/", "./config/" }),
    new MaxWriteSize(1024 * 1024), // 1MB
    new BuildMustPassBeforeCommit(),
    new NetworkAccessPolicy(denyExternal: true),
    new TimeoutPolicy(maxDuration: TimeSpan.FromMinutes(5))
};
```

## Incident Response

### Security Incident Process

1. **Detection**: Monitor audit logs for policy violations
2. **Assessment**: Determine severity and impact
3. **Containment**: Stop affected agents/tools
4. **Investigation**: Analyze logs and determine cause
5. **Recovery**: Restore normal operations
6. **Lessons Learned**: Update policies and procedures

### Emergency Contacts

- **Security Team**: [security@nexo.dev](mailto:security@nexo.dev)
- **Incident Response**: [incident@nexo.dev](mailto:incident@nexo.dev)
- **Emergency**: [emergency@nexo.dev](mailto:emergency@nexo.dev)

## Compliance

### Security Standards

- **OWASP Top 10**: Protection against common web vulnerabilities
- **CIS Controls**: Implementation of security best practices
- **NIST Framework**: Risk-based approach to security

### Audit Requirements

- **Log Retention**: 90 days minimum
- **Access Controls**: Role-based access to security features
- **Regular Reviews**: Monthly security policy reviews
- **Incident Documentation**: All incidents documented

## Future Enhancements

### Planned Security Features

- **Network Isolation**: Complete network access control
- **Encryption**: End-to-end encryption for sensitive data
- **Multi-Factor Authentication**: Enhanced access controls
- **Behavioral Analysis**: AI-powered threat detection
- **Compliance Reporting**: Automated compliance reports

---

**Last Updated**: September 2024
