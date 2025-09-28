using System;
using System.Collections.Generic;

namespace Nexo.Shared.Models.Assembly
{
    /// <summary>
    /// Represents the result of decompiling an assembly
    /// </summary>
    public class DecompilationResult
    {
        public string AssemblyPath { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public DateTime DecompiledAt { get; set; }
        public DecompilationStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public IReadOnlyList<DecompiledType> Types { get; set; } = new List<DecompiledType>();
        public IReadOnlyList<DecompiledResource> Resources { get; set; } = new List<DecompiledResource>();
        public IReadOnlyList<DecompilationWarning> Warnings { get; set; } = new List<DecompilationWarning>();
        public DecompilationSettings Settings { get; set; } = new DecompilationSettings();
    }

    /// <summary>
    /// Represents a decompiled type
    /// </summary>
    public class DecompiledType
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string SourceCode { get; set; } = string.Empty;
        public TypeKind Kind { get; set; }
        public IReadOnlyList<DecompiledMember> Members { get; set; } = new List<DecompiledMember>();
        public IReadOnlyList<DecompilationWarning> Warnings { get; set; } = new List<DecompilationWarning>();
    }

    /// <summary>
    /// Represents a decompiled member (method, property, field, event)
    /// </summary>
    public class DecompiledMember
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public MemberKind Kind { get; set; }
        public string SourceCode { get; set; } = string.Empty;
        public string ILCode { get; set; } = string.Empty;
        public IReadOnlyList<DecompilationWarning> Warnings { get; set; } = new List<DecompilationWarning>();
    }

    /// <summary>
    /// Represents a decompiled resource
    /// </summary>
    public class DecompiledResource
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public ResourceType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public byte[]? BinaryContent { get; set; }
        public long Size { get; set; }
    }

    /// <summary>
    /// Represents a decompilation warning
    /// </summary>
    public class DecompilationWarning
    {
        public string Message { get; set; } = string.Empty;
        public WarningLevel Level { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents decompilation settings
    /// </summary>
    public class DecompilationSettings
    {
        public bool IncludeComments { get; set; } = true;
        public bool IncludeAttributes { get; set; } = true;
        public bool IncludeUsings { get; set; } = true;
        public bool IncludeILCode { get; set; } = false;
        public bool IncludeResources { get; set; } = true;
        public bool IncludePrivateMembers { get; set; } = false;
        public bool IncludeInternalMembers { get; set; } = false;
        public bool IncludeNestedTypes { get; set; } = true;
        public bool IncludeGenerics { get; set; } = true;
        public bool IncludeAsyncMethods { get; set; } = true;
        public bool IncludeLambdaExpressions { get; set; } = true;
        public bool IncludeLINQ { get; set; } = true;
        public string Language { get; set; } = "C#";
        public string OutputFormat { get; set; } = "Source";
    }

    /// <summary>
    /// Represents the status of decompilation
    /// </summary>
    public enum DecompilationStatus
    {
        Success,
        Partial,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Represents the kind of a type
    /// </summary>
    public enum TypeKind
    {
        Class,
        Interface,
        Struct,
        Enum,
        Delegate,
        GenericTypeDefinition,
        Array,
        Pointer,
        ByRef
    }

    /// <summary>
    /// Represents the kind of a member
    /// </summary>
    public enum MemberKind
    {
        Method,
        Property,
        Field,
        Event,
        Constructor,
        Destructor,
        Indexer,
        Operator,
        NestedType
    }

    /// <summary>
    /// Represents the type of a resource
    /// </summary>
    public enum ResourceType
    {
        String,
        Binary,
        Image,
        Icon,
        Cursor,
        Font,
        Audio,
        Video,
        Document,
        Archive,
        Other
    }

    /// <summary>
    /// Represents the level of a warning
    /// </summary>
    public enum WarningLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
