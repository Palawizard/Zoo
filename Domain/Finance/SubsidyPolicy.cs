using Zoo.Domain.Animals;
using Zoo.Domain;

namespace Zoo.Domain.Finance;

public sealed class SubsidyPolicy
{
    public decimal AnnualSubsidy(SpeciesType species) => species switch
    {
        SpeciesType.Tiger => 200m,
        SpeciesType.Eagle => 50m,
        SpeciesType.Rooster => 5m,
        _ => 0m
    };

    public decimal ApplyAnnualSubsidies(ZooState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var total = state.Animals
            .GroupBy(a => a.Species)
            .Sum(g => AnnualSubsidy(g.Key) * g.Count());

        state.AddCash(total);
        return total;
    }
}
