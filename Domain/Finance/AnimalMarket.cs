using Zoo.Domain.Animals;

namespace Zoo.Domain.Finance;

public sealed class AnimalMarket
{
    private const int Age6MonthsDays = 180;
    private const int Age4YearsDays = 4 * 365;
    private const int Age14YearsDays = 14 * 365;

    private static readonly Dictionary<(SpeciesType Species, SexType Sex), List<PricePoint>> PriceCatalog = new()
    {
        {
            (SpeciesType.Tiger, SexType.Male),
            new List<PricePoint>
            {
                new(Age6MonthsDays, 3000m, 1500m),
                new(Age4YearsDays, 120000m, 60000m),
                new(Age14YearsDays, 60000m, 10000m)
            }
        },
        {
            (SpeciesType.Eagle, SexType.Male),
            new List<PricePoint>
            {
                new(Age6MonthsDays, 1000m, 500m),
                new(Age4YearsDays, 4000m, 2000m),
                new(Age14YearsDays, 2000m, 400m)
            }
        },
        {
            (SpeciesType.Rooster, SexType.Male),
            new List<PricePoint>
            {
                new(Age6MonthsDays, 100m, 20m)
            }
        },
        {
            (SpeciesType.Tiger, SexType.Female),
            new List<PricePoint>
            {
                new(Age6MonthsDays, 3000m, 1500m),
                new(Age4YearsDays, 120000m, 60000m),
                new(Age14YearsDays, 60000m, 10000m)
            }
        },
        {
            (SpeciesType.Eagle, SexType.Female),
            new List<PricePoint>
            {
                new(Age6MonthsDays, 1000m, 500m),
                new(Age4YearsDays, 4000m, 2000m),
                new(Age14YearsDays, 2000m, 400m)
            }
        },
        {
            (SpeciesType.Rooster, SexType.Female),
            new List<PricePoint>
            {
                new(Age6MonthsDays, 20m, 10m)
            }
        }
    };

    private sealed record PricePoint(int AgeDays, decimal BuyPrice, decimal SellPrice);
}
