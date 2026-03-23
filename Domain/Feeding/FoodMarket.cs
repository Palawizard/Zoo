namespace Zoo.Domain.Feeding;

/// <summary>
/// Provides food prices used by the simulation
/// </summary>
public sealed class FoodMarket
{
    /// <summary>
    /// Calculates the total purchase price for a quantity of food
    /// </summary>
    public decimal Buy(FoodType type, decimal kg)
    {
        if (kg < 0m) throw new ArgumentOutOfRangeException(nameof(kg));
        if (kg == 0m) return 0m;

        return PricePerKg(type) * kg;
    }

    /// <summary>
    /// Returns the price per kilogram for the given food type
    /// </summary>
    public decimal PricePerKg(FoodType type) => type switch
    {
        FoodType.Meat => 5m,
        FoodType.Seeds => 2.5m,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
