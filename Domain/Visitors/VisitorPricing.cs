namespace Zoo.Domain.Visitors;

public sealed class VisitorPricing
{
    public const decimal AdultTicketPrice = 17m;
    public const decimal ChildTicketPrice = 13m;

    //2 adultes + 2 enfants
    public decimal GroupRevenue => (2 * AdultTicketPrice) + (2 * ChildTicketPrice);
}
