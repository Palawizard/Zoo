namespace Zoo.Domain.Finance;

public sealed record Transaction(
    DateTime TimestampUtc,
    decimal Amount,
    string Description,
    string Category,
    decimal BalanceAfter
);
