namespace Zoo.Domain.Reproduction;

/// <summary>
/// Stores optional reproduction rules used by the legacy reproduction service
/// </summary>
public sealed class ReproductionRules
{
    /// <summary>
    /// Blocks adult reproduction during the first month after arrival
    /// </summary>
    public bool NoAdultReproductionFirstArrivalMonth { get; }

    /// <summary>
    /// Creates a new rule set
    /// </summary>
    public ReproductionRules(bool noAdultReproductionFirstArrivalMonth = true)
    {
        NoAdultReproductionFirstArrivalMonth = noAdultReproductionFirstArrivalMonth;
    }
}
