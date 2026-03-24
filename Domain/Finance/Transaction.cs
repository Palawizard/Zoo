namespace Zoo.Domain.Finance;

/// <summary>
/// Represents one money movement in the zoo ledger
/// </summary>
public sealed record Transaction(
    DateTime TimestampUtc,
    decimal Amount,
    string Description,
    string Category,
    decimal BalanceAfter
);
