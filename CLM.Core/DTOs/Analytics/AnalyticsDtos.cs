namespace CLM.Core.DTOs.Analytics;

public class DashboardSummary
{
    public int TotalContracts { get; set; }
    public int DraftContracts { get; set; }
    public int PendingContracts { get; set; }
    public int ApprovedContracts { get; set; }
    public int RejectedContracts { get; set; }
    public int ExpiringIn30Days { get; set; }
    public int ExpiringIn7Days { get; set; }
    public decimal TotalContractValue { get; set; }
    public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
    public List<StatusDistribution> StatusDistribution { get; set; } = new();
    public List<ExpiringContractInfo> ExpiringContracts { get; set; } = new();
}

public class MonthlyTrend
{
    public string Month { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Value { get; set; }
}

public class StatusDistribution
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ExpiringContractInfo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Parties { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}
