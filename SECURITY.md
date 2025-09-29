# Security Policy

## Supported Versions

We provide security updates for the following versions of Nexo:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security vulnerability in Nexo, please report it responsibly.

### How to Report

1. **Do not** create a public GitHub issue for security vulnerabilities
2. **Email** us directly at [security@nexo.dev](mailto:security@nexo.dev) with:
   - A detailed description of the vulnerability
   - Steps to reproduce the issue
   - Potential impact assessment
   - Any suggested fixes or mitigations

### What to Expect

- **Acknowledgment**: We will acknowledge receipt of your report within 48 hours
- **Initial Response**: We will provide an initial response within 5 business days
- **Regular Updates**: We will keep you informed of our progress
- **Resolution**: We will work with you to resolve the issue and coordinate disclosure

### Responsible Disclosure

We follow responsible disclosure practices:

1. **Confidentiality**: We will keep your report confidential until we have resolved the issue
2. **Timeline**: We aim to resolve critical vulnerabilities within 30 days
3. **Credit**: We will credit you in our security advisories (unless you prefer to remain anonymous)
4. **Coordination**: We will coordinate with you on the disclosure timeline

## Security Considerations

### Agent Safety

Nexo is designed with agent safety as a core principle:

- **Policy Enforcement**: All tool executions are subject to policy validation
- **Path Restrictions**: File operations are restricted to allowed paths
- **Size Limits**: File write operations have size limits
- **Audit Logging**: All agent actions are logged for security auditing

### Tool Execution

Tools executed by agents are subject to:

- **Path Allowlists**: Restrict file operations to safe directories
- **Size Limits**: Prevent excessive resource consumption
- **Timeout Envelopes**: Prevent runaway processes
- **Policy Validation**: All actions must pass security policies

### Data Protection

- **No PII Storage**: We do not store personally identifiable information
- **Encrypted Communication**: All network communication is encrypted
- **Secure Defaults**: Security policies are enabled by default

## Security Features

### Built-in Protections

- **Path Allowlist Policy**: Restricts file operations to safe directories
- **Max Write Size Policy**: Limits file write sizes
- **Build Validation Policy**: Ensures builds pass before commits
- **Audit Logging**: Complete audit trail of all agent actions

### Recommended Practices

1. **Use Policy Enforcement**: Always enable security policies
2. **Restrict Paths**: Use path allowlists to limit file access
3. **Monitor Logs**: Regularly review audit logs
4. **Update Regularly**: Keep Nexo and dependencies updated
5. **Review Permissions**: Regularly review agent permissions

## Threat Model

### Attack Vectors

- **Malicious Tools**: Tools that attempt to access restricted files
- **Path Traversal**: Attempts to access files outside allowed directories
- **Resource Exhaustion**: Tools that consume excessive resources
- **Code Injection**: Attempts to inject malicious code

### Mitigations

- **Policy Enforcement**: All actions validated against security policies
- **Sandboxing**: Tools run in restricted environments
- **Resource Limits**: CPU, memory, and disk usage limits
- **Input Validation**: All inputs validated before processing

### Non-Goals

- **Protection Against Malicious Agents**: We assume agents are trusted
- **Network Security**: We do not provide network-level security
- **OS-Level Security**: We rely on the underlying operating system

## Security Updates

Security updates are released as:

- **Patch Releases**: For critical security fixes
- **Minor Releases**: For security improvements
- **Major Releases**: For breaking security changes

## Contact

For security-related questions or concerns:

- **Email**: [security@nexo.dev](mailto:security@nexo.dev)
- **GitHub Security**: Use GitHub's private vulnerability reporting
- **General Questions**: [GitHub Discussions](https://github.com/IanFrelinger/Nexo/discussions)

## Acknowledgments

We thank all security researchers who responsibly disclose vulnerabilities to us. Your efforts help make Nexo more secure for everyone.

---

**Last Updated**: September 2024
