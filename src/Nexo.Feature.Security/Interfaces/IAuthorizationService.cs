using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Security.Interfaces;

/// <summary>
/// Provides comprehensive authorization services with role-based access control and permission-based authorization.
/// This interface acts as an orchestrator, delegating specific functionalities to partial interface implementations.
/// </summary>
public partial interface IAuthorizationService
{
    // This interface acts as an orchestrator for various authorization functionalities,
    // with specific categories defined in partial interfaces.
}