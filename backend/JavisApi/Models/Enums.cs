namespace JavisApi.Models;

public enum ScopeType
{
    Global,
    Project
}

public enum WorkspaceRole
{
    Viewer = 0,
    Contributor = 1,
    Editor = 2,
    Admin = 3
}

public enum SkillStatus
{
    Active,
    Processing,
    Deleting,
    Deprecated,
    Archived
}

public enum SourceStatus
{
    Pending,
    Processing,
    Ready,
    Error
}

public enum DraftStatus
{
    Pending,
    Approved,
    Rejected
}

public enum ChangeType
{
    AgentCompile,
    AgentRetry,
    EditorEdit,
    DraftApproved,
    ManualRebuild,
    Rollback
}
