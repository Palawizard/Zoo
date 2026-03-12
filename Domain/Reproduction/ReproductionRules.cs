namespace Zoo.Domain.Reproduction;

public sealed class ReproductionRules
{
    /// empêche les adultes de se reproduire le premier mois après leur arrivée
    public bool NoAdultReproductionFirstArrivalMonth { get; }

    public ReproductionRules(bool noAdultReproductionFirstArrivalMonth = true)
    {
        NoAdultReproductionFirstArrivalMonth = noAdultReproductionFirstArrivalMonth;
    }
}