namespace Zoo.Domain.Finance;

public sealed class Ledger
{
    private readonly List<Transaction> _transactions = new();

    public IReadOnlyList<Transaction> Transactions => _transactions;

    public void Add(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _transactions.Add(transaction);
    }
}
