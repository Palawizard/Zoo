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
    FoodType.Meat => 12m,
    FoodType.Seeds => 2m,
    _ => throw new ArgumentOutOfRangeException(nameof(type))
};
}
