using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;

namespace Zoo.Domain.Reproduction;

public sealed class ReproductionService
{
    private readonly ReproductionRules _rules;

    public ReproductionService(ReproductionRules? rules = null)
    {
        _rules = rules ?? new ReproductionRules();
    }

    //gestations +1  et retourne les bébé
    public List<ZooAnimal> ProcessGestations(List<ZooAnimal> animals)
    {
        var newborns = new List<ZooAnimal>();

        foreach (var female in animals.Where(a => a.IsAlive && a.Sex == SexType.Female && a.IsGestating))
        {
            var bornCount = female.ProgressGestationOneDay();
            if (bornCount <= 0) continue;

            newborns.AddRange(CreateOffspringBatch(female.Species, female.Name, bornCount, female.Profile.InfantMortalityRate));
        }

        return newborns;
    }

    
    public List<ZooAnimal> ProcessEggIncubations(List<ZooAnimal> animals)
    {
        var newborns = new List<ZooAnimal>();

        foreach (var female in animals.Where(a => a.IsAlive && a.Sex == SexType.Female && a.EggIncubationRemainingDays > 0))
        {
            var hatchedCount = female.ProgressEggIncubationOneDay();
            if (hatchedCount <= 0) continue;

            newborns.AddRange(CreateOffspringBatch(female.Species, female.Name, hatchedCount, female.Profile.InfantMortalityRate));
        }

        return newborns;
    }

    // essaie de déclencher des grossesses lorsque des couples sont présents
    public void TryStartPregnancies(List<ZooAnimal> animals)
    {
        var aliveBySpecies = animals
            .Where(a => a.IsAlive)
            .GroupBy(a => a.Species);

        foreach (var speciesGroup in aliveBySpecies)
        {
            var hasEligibleMale = speciesGroup.Any(a =>
                a.Sex == SexType.Male &&
                a.CanReproduceToday() &&
                a.CanReproduceByAge());

            if (!hasEligibleMale) continue;

            foreach (var female in speciesGroup.Where(a => a.Sex == SexType.Female))
            {
                if (female.CanStartGestationToday())
                    female.StartGestation();
            }
        }
    }
    public void TryEggLayingForCurrentMonth(List<ZooAnimal> animals, int currentMonth)
    {
        foreach (var female in animals.Where(a => a.IsAlive && a.Sex == SexType.Female))
        {
            if (!female.CanLayEggThisMonth(currentMonth)) continue;

            var eggsToIncubate = GetEggCountForMonth(female, currentMonth);
            if (eggsToIncubate <= 0) continue;

            female.StartEggIncubation(eggsToIncubate, currentMonth);
        }
    }

    private static IEnumerable<ZooAnimal> CreateOffspringBatch(SpeciesType species, string parentName, int count, decimal? infantMortalityRate)
    {
        var survivorCount = ComputeSurvivorsAfterInfantMortality(count, infantMortalityRate);

        var newborns = new List<ZooAnimal>(survivorCount);

        for (var i = 0; i < survivorCount; i++)
        {
            var sex = Random.Shared.Next(0, 2) == 0 ? SexType.Male : SexType.Female;
            var name = BuildTemporaryNewbornName(species, parentName, i + 1);
            newborns.Add(new ZooAnimal(name, sex, species, ageDays: 0, isHungry: false, isSick: false));
        }

        return newborns;
    }

    private static int ComputeSurvivorsAfterInfantMortality(int newbornCount, decimal? infantMortalityRate)
    {
        if (newbornCount <= 0) return 0;

        var rate = NormalizeInfantMortalityRate(infantMortalityRate);

        if (rate <= 0m) return newbornCount;
        if (rate >= 1m) return 0;

        var survivors = 0;

        for (var i = 0; i < newbornCount; i++)
        {
            var roll = (decimal)Random.Shared.NextDouble();

            if (roll >= rate) survivors++;
        }

        return survivors;
    }

    private static decimal NormalizeInfantMortalityRate(decimal? infantMortalityRate)
    {
        if (!infantMortalityRate.HasValue) return 0m;

        return Math.Clamp(infantMortalityRate.Value, 0m, 1m);
    }

    private static int GetEggCountForMonth(Animal female, int month)
    {
        if (female.Profile.EggLayingMonth is int layingMonth &&
            layingMonth == month &&
            female.Profile.LitterSize is int litterSize &&
            litterSize > 0)
        {
            return litterSize;
        }

        if (female.Profile.EggsPerYear is int eggsPerYear && eggsPerYear > 0)
        {
            return Math.Max(1, (int)Math.Round(eggsPerYear / 12.0, MidpointRounding.AwayFromZero));
        }

        return 0;
    }

    private static string BuildTemporaryNewbornName(SpeciesType species, string parentName, int order)
    {
        var label = species switch
        {
            SpeciesType.Tiger => "Cub",
            SpeciesType.Eagle => "Eaglet",
            SpeciesType.Rooster => "Chick",
            _ => "Child"
        };

        return $"{label} of {parentName} {order}";
    }
}
