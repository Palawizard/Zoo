namespace Zoo.Domain.Feeding;

public sealed class FoodStock
{
    public decimal MeatKg { get; private set; }
    public decimal SeedsKg { get; private set; }

    public FoodStock(decimal meatKg = 0m, decimal seedsKg = 0m)
    {
        if (meatKg < 0m) throw new ArgumentOutOfRangeException(nameof(meatKg));
        if (seedsKg < 0m) throw new ArgumentOutOfRangeException(nameof(seedsKg));

        MeatKg = meatKg;
        SeedsKg = seedsKg;
    }

    public void Add(FoodType type, decimal kg)
    {
        if (kg < 0m) throw new ArgumentOutOfRangeException(nameof(kg));

        if (type == FoodType.Meat)
            MeatKg += kg;
        else
            SeedsKg += kg;
    }

    public bool Consume(FoodType type, decimal kg)
    {
        if (kg < 0m) throw new ArgumentOutOfRangeException(nameof(kg));
        if (kg == 0m) return true;

        if (type == FoodType.Meat)
        {
            if (MeatKg >= kg)
            {
                MeatKg -= kg;
                return true;
            }

            MeatKg = 0m;
            return false;
        }

        if (SeedsKg >= kg)
        {
            SeedsKg -= kg;
            return true;
        }

        SeedsKg = 0m;
        return false;
    }

    public void LosePercent(FoodType type, decimal pct)
    {
        if (pct < 0m || pct > 1m)
            throw new ArgumentOutOfRangeException(nameof(pct), "Percent must be between 0 and 1.");

        if (type == FoodType.Meat)
        {
            MeatKg -= MeatKg * pct;
            return;
        }

        SeedsKg -= SeedsKg * pct;
    }
}
