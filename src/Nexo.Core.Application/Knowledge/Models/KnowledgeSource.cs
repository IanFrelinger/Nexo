namespace Nexo.Core.Application.Knowledge.Models;

/// <summary>Knowledge source backing a query result entry.</summary>
public enum KnowledgeSource
{
    /// <summary>Adaptation runtime log entries.</summary>
    Adaptation = 0,

    /// <summary>Learned pattern store entries.</summary>
    Pattern = 1,

    /// <summary>User-supplied knowledge log entries.</summary>
    UserKnowledge = 2
}
