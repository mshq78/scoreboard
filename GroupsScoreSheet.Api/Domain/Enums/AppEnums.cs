namespace GroupsScoreSheet.Api.Domain.Enums;

public enum CourseStatus
{
    Active = 1,
    Deleted = 2
}

public enum EvaluationRoundStatus
{
    Active = 1,
    Closed = 2,
    Reset = 3
}

public enum EvaluatorProfileStatus
{
    Active = 1,
    Finalized = 2,
    Invalidated = 3
}

public enum ExcelImportStatus
{
    Valid = 1,
    Invalid = 2,
    Imported = 3,
    Failed = 4
}

public enum SyncType
{
    Partial = 1,
    Final = 2
}

public enum SyncStatus
{
    Success = 1,
    Failed = 2,
    Rejected = 3
}