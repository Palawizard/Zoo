using Zoo.Domain.Animals;

namespace Zoo.Domain.Visitors;

public sealed class VisitorFlow
{
    //donne le nombre moyen de groupes de visiteurs par animal
    public decimal GetVisitorsPerAnimal(SpeciesType species, bool isHighSeason)
    {
        return species switch
        {
            SpeciesType.Tiger => isHighSeason ? 30m : 5m,
            SpeciesType.Rooster => isHighSeason ? 2m : 0.5m,
            SpeciesType.Eagle => isHighSeason ? 15m : 7m,
            _ => 0m
        };
    }
}
