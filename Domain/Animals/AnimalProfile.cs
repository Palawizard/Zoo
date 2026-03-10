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
