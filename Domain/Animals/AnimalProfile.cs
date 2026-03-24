using Zoo.Domain.Feeding;

namespace Zoo.Domain.Animals;

/// <summary>
/// Stores the fixed biological and gameplay data for one species and sex
/// </summary>
public sealed record AnimalProfile(
    SpeciesType Species,
    SexType Sex,
    FoodType FoodType,
    decimal DailyFoodKg,
    int DaysBeforeHungry,
    int SexualMaturityDays,
    int? GestationDays,
    int ReproductionEndDays,
    decimal? InfantMortalityRate,
    int LifeExpectancyDays,
    bool IsMonogamous,
    int? LitterSize,
    int? MinMonthsBetweenLitters,
    int? EggsPerYear,
    int? EggLayingMonth,
    decimal AnnualDiseaseProbability,
    int BaseDiseaseDurationDays
);
