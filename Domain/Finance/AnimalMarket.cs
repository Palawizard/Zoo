using Zoo.Domain.Animals;

namespace Zoo.Domain.Finance;

/// <summary>
/// Provides buy and sell prices for animals
/// </summary>
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

    /// <summary>
    /// Stores a market price point for one age threshold
    /// </summary>
    private sealed record PricePoint(int AgeDays, decimal BuyPrice, decimal SellPrice);

    /// <summary>
    /// Returns the male buy price kept for backward compatibility
    /// </summary>
    public decimal BuyAnimalPrice(SpeciesType species, int ageDays)
        => GetPrice(species, SexType.Male, ageDays, isBuy: true);

    /// <summary>
    /// Returns the male sell price kept for backward compatibility
    /// </summary>
    public decimal SellAnimalPrice(SpeciesType species, int ageDays)
        => GetPrice(species, SexType.Male, ageDays, isBuy: false);

    /// <summary>
    /// Returns the buy price for one animal
    /// </summary>
    public decimal BuyAnimalPrice(SpeciesType species, SexType sex, int ageDays)
        => GetPrice(species, sex, ageDays, isBuy: true);

    /// <summary>
    /// Returns the sell price for one animal
    /// </summary>
    public decimal SellAnimalPrice(SpeciesType species, SexType sex, int ageDays)
        => GetPrice(species, sex, ageDays, isBuy: false);

    // Prices are interpolated between age points to avoid hard jumps
    private static decimal GetPrice(SpeciesType species, SexType sex, int ageDays, bool isBuy)
    {
        if (ageDays < 0)
            throw new ArgumentOutOfRangeException(nameof(ageDays), "Age cannot be negative.");

        if (!PriceCatalog.TryGetValue((species, sex), out var points) || points.Count == 0)
            throw new InvalidOperationException($"Missing price catalog for {species}/{sex}.");

        var first = points[0];
        if (ageDays <= first.AgeDays)
            return isBuy ? first.BuyPrice : first.SellPrice;

        var last = points[^1];
        if (ageDays >= last.AgeDays)
            return isBuy ? last.BuyPrice : last.SellPrice;

        for (var i = 0; i < points.Count - 1; i++)
        {
            var left = points[i];
            var right = points[i + 1];

            if (ageDays < right.AgeDays)
            {
                // The age sits between two catalog points, so the price is interpolated
                var ratio = (ageDays - left.AgeDays) / (decimal)(right.AgeDays - left.AgeDays);
                var leftPrice = isBuy ? left.BuyPrice : left.SellPrice;
                var rightPrice = isBuy ? right.BuyPrice : right.SellPrice;

                return leftPrice + ratio * (rightPrice - leftPrice);
            }
        }

        return isBuy ? last.BuyPrice : last.SellPrice;
    }
}
