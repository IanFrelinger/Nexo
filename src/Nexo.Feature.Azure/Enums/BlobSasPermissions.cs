using System;

namespace Nexo.Feature.Azure.Enums;

/// <summary>
/// Blob SAS permissions
/// </summary>
[Flags]
public enum BlobSasPermissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    List = 8,
    Add = 16,
    Create = 32,
    Update = 64,
    Process = 128,
    All = Read | Write | Delete | List | Add | Create | Update | Process
} 