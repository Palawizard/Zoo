namespace Zoo.Domain.Finance;

/// <summary>
/// Stores the financial history of the zoo
/// </summary>
public sealed class Ledger
{
    private readonly List<Transaction> _transactions = new();

    public IReadOnlyList<Transaction> Transactions => _transactions;

    /// <summary>
    /// Adds a new transaction to the ledger
    /// </summary>
    public void Add(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _transactions.Add(transaction);
    }
}
