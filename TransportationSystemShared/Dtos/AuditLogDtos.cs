namespace TransportationSystemApi.Dtos;

public class AuditLogListItemDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    // Short human summary of the change (field names for an update, "created" / "deleted").
    public string Summary { get; set; } = string.Empty;
}

public class AuditLogDetailDto : AuditLogListItemDto
{
    public string? ChangesJson { get; set; }
}

public class AuditLogPageDto
{
    public List<AuditLogListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// Distinct entity names present in the log, for the filter dropdown.
public class AuditLogFacetsDto
{
    public List<string> EntityNames { get; set; } = new();
    public List<string> Actions { get; set; } = new();
}
