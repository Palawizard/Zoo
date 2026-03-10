namespace Zoo.Domain.Feeding;

public sealed class FoodMarket
{
    public decimal Buy(FoodType type, decimal kg)
    {
        if (kg < 0m) throw new ArgumentOutOfRangeException(nameof(kg));
        if (kg == 0m) return 0m;

        return PricePerKg(type) * kg;
    }

    public decimal PricePerKg(FoodType type) => type switch // better to more food
{
    FoodType.Meat => 5m,
    FoodType.Seeds => 2.5m,
    _ => throw new ArgumentOutOfRangeException(nameof(type))
};
}
