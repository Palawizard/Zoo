namespace Zoo.Domain.Reproduction;

public sealed class ReproductionRules
{
    /// Pas de reproduction le premier mois
    public bool NoAdultReproductionFirstArrivalMonth { get; }

    public ReproductionRules(bool noAdultReproductionFirstArrivalMonth = true)
    {
        NoAdultReproductionFirstArrivalMonth = noAdultReproductionFirstArrivalMonth;
    }
}