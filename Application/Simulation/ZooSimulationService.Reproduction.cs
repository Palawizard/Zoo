using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;
using Zoo.Domain.Habitats;

namespace Zoo.Application.Simulation;

public sealed partial class ZooSimulationService
{
    private sealed record OffspringBatchResult(
        IReadOnlyList<ZooAnimal> Newborns,
        int TotalBornCount,
        int SurvivorCount,
        int InfantDeathCount);

    public ZooAnimal? PeekNewbornAwaitingName()
    {
        CleanupPendingNewbornNaming();

        return _pendingNewbornNaming.TryPeek(out var animalId)
            ? _animals.FirstOrDefault(animal => animal.Id == animalId)
            : null;
    }

    public bool TryFinalizeNextNewbornNaming(string? chosenName, out ZooAnimal? newborn, out string failureReason)
    {
        CleanupPendingNewbornNaming();

        newborn = null;
        failureReason = string.Empty;

        while (_pendingNewbornNaming.TryDequeue(out var animalId))
        {
            newborn = _animals.FirstOrDefault(animal => animal.Id == animalId);
            if (newborn is null)
                continue;

            if (!string.IsNullOrWhiteSpace(chosenName))
                newborn.Rename(chosenName);

            return true;
        }

        failureReason = "No newborn is waiting for a name.";
        return false;
    }

    public bool CanReproduceToday(Animal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        return animal.CanReproduceToday();
    }

    public bool CanReproduceToday(Animal first, Animal second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return first.CanReproduceToday() && second.CanReproduceToday();
    }

    public void ProcessGestations()
    {
        ProcessOffspringCycle(
            _animals.Where(animal => animal.IsAlive && animal.Sex == SexType.Female && animal.IsGestating),
            female => female.ProgressGestationOneDay(),
            (female, batch) => $"{female.Name} gave birth to {batch.TotalBornCount} {female.Species} newborn(s).");
    }

    public void ProcessEggIncubations()
    {
        ProcessOffspringCycle(
            _animals.Where(animal => animal.IsAlive && animal.Sex == SexType.Female && animal.EggIncubationRemainingDays > 0),
            female => female.ProgressEggIncubationOneDay(),
            (female, batch) => $"{female.Name} hatched {batch.TotalBornCount} {female.Species} newborn(s).");
    }

    public void TryStartPregnancies()
    {
        CleanupInvalidMonogamousPairs();
        var reservedSlotsBySpecies = new Dictionary<SpeciesType, int>();

        foreach (var habitat in _habitats)
        {
            var females = habitat.Animals
                .OfType<ZooAnimal>()
                .Where(animal => animal.Sex == SexType.Female && animal.CanStartGestationToday())
                .ToList();

            foreach (var female in females)
            {
                if (!HasEligibleMate(habitat, female))
                    continue;

                var expectedOffspring = GetExpectedOffspringCount(female);
                var reservedToday = reservedSlotsBySpecies.GetValueOrDefault(female.Species);
                if (!HasCapacityForAdditionalOffspring(female.Species, expectedOffspring, reservedToday))
                    continue;

                female.StartGestation();
                reservedSlotsBySpecies[female.Species] = reservedToday + expectedOffspring;
                AddEvent(
                    Domain.Events.ZooEventType.Pregnancy,
                    $"{female.Name} started a gestation for species {female.Species}.");
            }
        }
    }

    public void TryEggLayingForCurrentMonth()
    {
        CleanupInvalidMonogamousPairs();
        var reservedSlotsBySpecies = new Dictionary<SpeciesType, int>();

        foreach (var habitat in _habitats)
        {
            var females = habitat.Animals
                .OfType<ZooAnimal>()
                .Where(animal => animal.Sex == SexType.Female && animal.CanLayEggThisMonth(CurrentMonth))
                .ToList();

            foreach (var female in females)
            {
                if (!HasEligibleMate(habitat, female))
                    continue;

                var eggsToIncubate = GetEggCountForMonth(female, CurrentMonth);
                if (eggsToIncubate <= 0)
                    continue;

                var reservedToday = reservedSlotsBySpecies.GetValueOrDefault(female.Species);
                if (!HasCapacityForAdditionalOffspring(female.Species, eggsToIncubate, reservedToday))
                    continue;

                female.StartEggIncubation(eggsToIncubate, CurrentMonth);
                reservedSlotsBySpecies[female.Species] = reservedToday + eggsToIncubate;
                AddEvent(
                    Domain.Events.ZooEventType.EggLaying,
                    $"{female.Name} laid {eggsToIncubate} egg(s).");
            }
        }
    }

    private void ProcessOffspringCycle(
        IEnumerable<ZooAnimal> mothers,
        Func<ZooAnimal, int> completeCycle,
        Func<ZooAnimal, OffspringBatchResult, string> successMessageFactory)
    {
        var newborns = new List<ZooAnimal>();

        foreach (var female in mothers.ToList())
        {
            var bornCount = completeCycle(female);
            if (bornCount <= 0)
                continue;

            female.RegisterBirthCycleCompleted();

            var queuedForSpecies = newborns.Count(animal => animal.Species == female.Species);
            var availableHabitatSlots = GetRemainingHabitatCapacityForSpecies(female.Species, queuedForSpecies);
            var batch = CreateOffspringBatch(
                female.Species,
                female.Name,
                bornCount,
                female.Profile.InfantMortalityRate,
                availableHabitatSlots);

            EmitOffspringEvents(female, batch, successMessageFactory);
            newborns.AddRange(batch.Newborns);
        }

        AddNewbornsToZoo(newborns);
    }

    private void EmitOffspringEvents(
        ZooAnimal female,
        OffspringBatchResult batch,
        Func<ZooAnimal, OffspringBatchResult, string> successMessageFactory)
    {
        if (batch.TotalBornCount > 0)
        {
            AddEvent(
                Domain.Events.ZooEventType.Birth,
                successMessageFactory(female, batch));
        }

        if (batch.InfantDeathCount > 0)
        {
            AddEvent(
                Domain.Events.ZooEventType.InfantDeath,
                $"{batch.InfantDeathCount} {female.Species} newborn(s) died from infant mortality.");
        }
    }

    private OffspringBatchResult CreateOffspringBatch(
        SpeciesType species,
        string parentName,
        int count,
        decimal? infantMortalityRate,
        int availableHabitatSlots)
    {
        var survivorCount = ComputeSurvivorsAfterInfantMortality(count, infantMortalityRate);
        survivorCount = Math.Min(survivorCount, Math.Max(0, availableHabitatSlots));

        var newborns = new List<ZooAnimal>(survivorCount);
        for (var i = 0; i < survivorCount; i++)
        {
            var sex = Random.Shared.Next(0, 2) == 0 ? SexType.Male : SexType.Female;
            var name = BuildTemporaryNewbornName(species, parentName, i + 1);
            newborns.Add(new ZooAnimal(name, sex, species, ageDays: 0, isHungry: false, isSick: false));
        }

        return new OffspringBatchResult(
            newborns,
            count,
            survivorCount,
            count - survivorCount);
    }

    private static int ComputeSurvivorsAfterInfantMortality(int newbornCount, decimal? infantMortalityRate)
    {
        if (newbornCount <= 0)
            return 0;

        var rate = NormalizeInfantMortalityRate(infantMortalityRate);
        if (rate <= 0m)
            return newbornCount;
        if (rate >= 1m)
            return 0;

        var survivors = 0;
        for (var i = 0; i < newbornCount; i++)
        {
            var roll = (decimal)Random.Shared.NextDouble();
            if (roll >= rate)
                survivors++;
        }

        return survivors;
    }

    private static decimal NormalizeInfantMortalityRate(decimal? infantMortalityRate)
    {
        if (!infantMortalityRate.HasValue)
            return 0m;

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
            var baseEggs = eggsPerYear / 12;
            var remainder = eggsPerYear % 12;
            return baseEggs + (month <= remainder ? 1 : 0);
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

    private void AddNewbornsToZoo(IEnumerable<ZooAnimal> newborns)
    {
        foreach (var newborn in newborns)
        {
            AddAnimal(newborn);
            _pendingNewbornNaming.Enqueue(newborn.Id);
            TryPlaceAnimalInHabitat(newborn);
        }
    }

    private void CleanupPendingNewbornNaming()
    {
        while (_pendingNewbornNaming.TryPeek(out var animalId) &&
               !_animals.Any(animal => animal.Id == animalId && animal.IsAlive))
        {
            _pendingNewbornNaming.Dequeue();
        }
    }

    private int GetReservedOffspringSlots(SpeciesType species)
    {
        return _animals
            .Where(animal => animal.IsAlive && animal.Species == species)
            .Sum(GetReservedOffspringCount);
    }

    private static int GetReservedOffspringCount(ZooAnimal animal)
    {
        if (animal.IsGestating)
            return animal.Profile.LitterSize ?? 0;
        if (animal.EggIncubationRemainingDays > 0)
            return animal.PendingEggs;

        return 0;
    }

    private int GetRemainingHabitatCapacityForSpecies(SpeciesType species, int queuedNewborns)
    {
        return Math.Max(0, GetAvailableHabitatSlots(species) - queuedNewborns);
    }

    private bool HasCapacityForAdditionalOffspring(SpeciesType species, int requiredSlots, int reservedToday)
    {
        var remainingSlots = GetAvailableHabitatSlots(species) - GetReservedOffspringSlots(species) - reservedToday;
        return remainingSlots >= requiredSlots;
    }

    private static int GetExpectedOffspringCount(ZooAnimal female)
    {
        return Math.Max(1, female.Profile.LitterSize ?? 1);
    }

    private bool HasEligibleMate(Habitat habitat, ZooAnimal female)
    {
        if (female.Profile.IsMonogamous)
        {
            var partner = GetOrCreateMonogamousPartner(habitat, female);
            return partner is not null && IsEligibleMaleForReproduction(partner);
        }

        return habitat.Animals
            .OfType<ZooAnimal>()
            .Any(IsEligibleMaleForReproduction);
    }

    private static bool IsEligibleMaleForReproduction(ZooAnimal animal)
    {
        return animal.Sex == SexType.Male
            && animal.CanReproduceToday()
            && animal.CanReproduceByAge();
    }

    private ZooAnimal? GetOrCreateMonogamousPartner(Habitat habitat, ZooAnimal female)
    {
        var existingPartner = GetPairedAnimal(female);
        if (existingPartner is not null)
        {
            if (habitat.Animals.Contains(existingPartner) && existingPartner.Species == female.Species)
                return existingPartner;

            RemovePairing(female.Id);
        }

        var availableMale = habitat.Animals
            .OfType<ZooAnimal>()
            .FirstOrDefault(animal =>
                animal.Sex == SexType.Male &&
                IsEligibleMaleForReproduction(animal) &&
                GetPairedAnimal(animal) is null);

        if (availableMale is null)
            return null;

        RegisterPair(female, availableMale);
        return availableMale;
    }

    private ZooAnimal? GetPairedAnimal(ZooAnimal animal)
    {
        if (!_monogamousPairs.TryGetValue(animal.Id, out var partnerId))
            return null;

        return _animals.FirstOrDefault(candidate => candidate.Id == partnerId && candidate.IsAlive);
    }

    private void RegisterPair(ZooAnimal first, ZooAnimal second)
    {
        RemovePairing(first.Id);
        RemovePairing(second.Id);
        _monogamousPairs[first.Id] = second.Id;
        _monogamousPairs[second.Id] = first.Id;
    }

    private void RemovePairing(Guid animalId)
    {
        if (!_monogamousPairs.Remove(animalId, out var partnerId))
            return;

        _monogamousPairs.Remove(partnerId);
    }

    private void CleanupInvalidMonogamousPairs()
    {
        var aliveAnimalIds = _animals
            .Where(animal => animal.IsAlive)
            .Select(animal => animal.Id)
            .ToHashSet();

        foreach (var animalId in _monogamousPairs.Keys.ToList())
        {
            if (!_monogamousPairs.TryGetValue(animalId, out var partnerId))
                continue;
            if (aliveAnimalIds.Contains(animalId) && aliveAnimalIds.Contains(partnerId))
                continue;

            RemovePairing(animalId);
        }
    }
}
