namespace Zoo.Domain.Visitors;

public sealed class VisitorStats
{
    public int MonthlyVisitors { get; private set; }
    public Season CurrentSeason { get; set; } = Season.Low;
    public int ExposedAnimalsCount { get; set; }

    public int ComputeVisitors()
    {
        var baseVisitors = 100 + (ExposedAnimalsCount * 20);
        var multiplier = CurrentSeason == Season.High ? 1.2m : 0.8m;

        MonthlyVisitors = (int)Math.Max(0, Math.Round(baseVisitors * multiplier, MidpointRounding.AwayFromZero));
        return MonthlyVisitors;
    }

    public decimal ComputeRevenue(VisitorPricing pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);
        if (MonthlyVisitors <= 0) ComputeVisitors();

        const decimal adultRatio = 0.7m;
        const decimal childRatio = 0.3m;

        return MonthlyVisitors * ((pricing.AdultPrice * adultRatio) + (pricing.ChildPrice * childRatio));
    }
}
