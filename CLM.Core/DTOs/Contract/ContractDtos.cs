using CLM.Core.Enums;

namespace CLM.Core.DTOs.Contract;

public class CreateContractRequest
{
    public string Title { get; set; } = string.Empty;
    public string Parties { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Value { get; set; }
    public string? Description { get; set; }
}

public class UpdateContractRequest
{
    public string Title { get; set; } = string.Empty;
    public string Parties { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Value { get; set; }
    public string? Description { get; set; }
    public string? ChangeNotes { get; set; }
}

public class ContractResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Parties { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Value { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? Description { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int VersionCount { get; set; }
}

public class ContractFilterRequest
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
