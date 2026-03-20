using Avalonia.Media;
using Zoo.Desktop.Styling;
using Zoo.Domain.Finance;

namespace Zoo.Desktop.Models;

public sealed class LedgerRow
{
    public LedgerRow(Transaction transaction)
    {
        Transaction = transaction;
    }

    public Transaction Transaction { get; }

    public string Amount => $"{Transaction.Amount:+0.##;-0.##;0} EUR";
    public string Description => Transaction.Description;
    public string Meta => $"{Transaction.Category} | {Transaction.TimestampUtc:yyyy-MM-dd HH:mm} UTC";
    public string Balance => $"Balance {Transaction.BalanceAfter:0.##} EUR";
    public IBrush AmountBrush => Transaction.Amount >= 0m ? UiBrushes.Success : UiBrushes.Danger;
}
