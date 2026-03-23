using System;
using System.Collections.Generic;
using System.Linq;
using Zoo.Domain.Animals;
using Zoo.Domain.Habitats;

namespace Zoo.Application.Simulation;

public sealed partial class ZooSimulationService
{
    /// <summary>
    /// Bundles the result of one birth or hatch cycle
    /// </summary>
    private sealed record OffspringBatchResult(
        IReadOnlyList<ZooAnimal> Newborns,
        int TotalBornCount,
        int SurvivorCount,
        int InfantDeathCount);

    /// <summary>
    /// Returns the next newborn waiting for a final name
    /// </summary>
    public ZooAnimal? PeekNewbornAwaitingName()
    {
        CleanupPendingNewbornNaming();

        return _pendingNewbornNaming.TryPeek(out var animalId)
            ? _animals.FirstOrDefault(animal => animal.Id == animalId)
            : null;
    }

    /// <summary>
    /// Finalizes the name of the next newborn waiting in the queue
    /// </summary>
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

    /// <summary>
    /// Returns whether one animal can reproduce today
    /// </summary>
    public bool CanReproduceToday(Animal animal)
    {
        ArgumentNullException.ThrowIfNull(animal);
        return animal.CanReproduceToday();
    }

    /// <summary>
    /// Returns whether both animals can reproduce today
    /// </summary>
    public bool CanReproduceToday(Animal first, Animal second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        return first.CanReproduceToday() && second.CanReproduceToday();
    }

    /// <summary>
    /// Advances all current gestations
    /// </summary>
    public void ProcessGestations()
    {
        ProcessOffspringCycle(
            _animals.Where(animal => animal.IsAlive && animal.Sex == SexType.Female && animal.IsGestating),
            female => female.ProgressGestationOneDay(),
            (female, batch) => $"{female.Name} gave birth to {batch.TotalBornCount} {female.Species} newborn(s).");
    }

    /// <summary>
    /// Advances all current egg incubations
    /// </summary>
    public void ProcessEggIncubations()
    {
        ProcessOffspringCycle(
            _animals.Where(animal => animal.IsAlive && animal.Sex == SexType.Female && animal.EggIncubationRemainingDays > 0),
            female => female.ProgressEggIncubationOneDay(),
            (female, batch) => $"{female.Name} hatched {batch.TotalBornCount} {female.Species} newborn(s).");
    }

    /// <summary>
    /// Tries to start new gestations in all habitats
    /// </summary>
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

    /// <summary>
    /// Tries to start egg incubation for the current month
    /// </summary>
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

    // The same pipeline is used for mammals and egg-laying species
    private void ProcessOffspringCycle(
        IEnumerable<ZooAnimal> mothers,
        Func<ZooAnimal, int> completeCycle,
        Func<ZooAnimal, OffspringBatchResult, string> successMessageFactory)
    {
        var newborns = new List<ZooAnimal>();

        // ToList avoids modifying the underlying sequence while newborns are added later
        foreach (var female in mothers.ToList())
        {
            var bornCount = completeCycle(female);
            if (bornCount <= 0)
                continue;

            female.RegisterBirthCycleCompleted();

            // Newborns already queued this turn also consume future habitat space
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

    // Birth and infant mortality are logged separately
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

    // The batch is trimmed if there is not enough habitat space left
    private OffspringBatchResult CreateOffspringBatch(
        SpeciesType species,
        string parentName,
        int count,
        decimal? infantMortalityRate,
        int availableHabitatSlots)
    {
        var survivorCount = ComputeSurvivorsAfterInfantMortality(count, infantMortalityRate);

        // Newborns beyond the available habitat space are discarded here
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

    // Each newborn gets a mortality roll based on the species profile
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

    // The rate is normalized before the mortality rolls
    private static decimal NormalizeInfantMortalityRate(decimal? infantMortalityRate)
    {
        if (!infantMortalityRate.HasValue)
            return 0m;

        return Math.Clamp(infantMortalityRate.Value, 0m, 1m);
    }

    // Egg counts depend on either a fixed laying month or a yearly quota
    private static int GetEggCountForMonth(Animal female, int month)
    {
        if (female.Profile.EggLayingMonth is int layingMonth &&
            layingMonth == month &&
            female.Profile.LitterSize is int litterSize &&
            litterSize > 0)
        {
            // Fixed laying month species use their litter size as the egg batch
            return litterSize;
        }

        if (female.Profile.EggsPerYear is int eggsPerYear && eggsPerYear > 0)
        {
            var baseEggs = eggsPerYear / 12;
            var remainder = eggsPerYear % 12;

            // The remainder is spread over the first months of the year
            return baseEggs + (month <= remainder ? 1 : 0);
        }

        return 0;
    }

    // Temporary names let the UI queue newborn naming later
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

    // Newborns are added first, then placed into habitats if possible
    private void AddNewbornsToZoo(IEnumerable<ZooAnimal> newborns)
    {
        foreach (var newborn in newborns)
        {
            AddAnimal(newborn);
            _pendingNewbornNaming.Enqueue(newborn.Id);
            TryPlaceAnimalInHabitat(newborn);
        }
    }

    // Dead or missing newborns are removed from the naming queue
    private void CleanupPendingNewbornNaming()
    {
        while (_pendingNewbornNaming.TryPeek(out var animalId) &&
               !_animals.Any(animal => animal.Id == animalId && animal.IsAlive))
        {
            _pendingNewbornNaming.Dequeue();
        }
    }

    // Reserved slots account for pregnancies and incubating eggs already in progress
    private int GetReservedOffspringSlots(SpeciesType species)
    {
        return _animals
            .Where(animal => animal.IsAlive && animal.Species == species)
            .Sum(GetReservedOffspringCount);
    }

    // Future offspring already consume habitat capacity before they are born
    private static int GetReservedOffspringCount(ZooAnimal animal)
    {
        if (animal.IsGestating)
            return animal.Profile.LitterSize ?? 0;
        if (animal.EggIncubationRemainingDays > 0)
            return animal.PendingEggs;

        return 0;
    }

    // Queued newborns are counted before they are placed
    private int GetRemainingHabitatCapacityForSpecies(SpeciesType species, int queuedNewborns)
    {
        return Math.Max(0, GetAvailableHabitatSlots(species) - queuedNewborns);
    }

    // Today's reservations are tracked separately from already existing pregnancies
    private bool HasCapacityForAdditionalOffspring(SpeciesType species, int requiredSlots, int reservedToday)
    {
        var remainingSlots = GetAvailableHabitatSlots(species) - GetReservedOffspringSlots(species) - reservedToday;
        return remainingSlots >= requiredSlots;
    }

    // Litter size falls back to one when the profile does not define it
    private static int GetExpectedOffspringCount(ZooAnimal female)
    {
        return Math.Max(1, female.Profile.LitterSize ?? 1);
    }

    // Monogamous species need a stable partner, others just need any eligible male
    private bool HasEligibleMate(Habitat habitat, ZooAnimal female)
    {
        if (female.Profile.IsMonogamous)
        {
            var partner = GetOrCreateMonogamousPartner(habitat, female);
            return partner is not null && IsEligibleMaleForReproduction(partner);
        }

        // Non-monogamous species only need one eligible male in the habitat
        return habitat.Animals
            .OfType<ZooAnimal>()
            .Any(IsEligibleMaleForReproduction);
    }

    // Males must be alive, healthy and in the right age window
    private static bool IsEligibleMaleForReproduction(ZooAnimal animal)
    {
        return animal.Sex == SexType.Male
            && animal.CanReproduceToday()
            && animal.CanReproduceByAge();
    }

    // Existing monogamous pairs are reused as long as they stay valid
    private ZooAnimal? GetOrCreateMonogamousPartner(Habitat habitat, ZooAnimal female)
    {
        var existingPartner = GetPairedAnimal(female);
        if (existingPartner is not null)
        {
            if (habitat.Animals.Contains(existingPartner) && existingPartner.Species == female.Species)
                return existingPartner;

            RemovePairing(female.Id);
        }

        // A male already paired with another female cannot be reused here
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

    // Pairings are stored by animal id instead of direct references
    private ZooAnimal? GetPairedAnimal(ZooAnimal animal)
    {
        if (!_monogamousPairs.TryGetValue(animal.Id, out var partnerId))
            return null;

        return _animals.FirstOrDefault(candidate => candidate.Id == partnerId && candidate.IsAlive);
    }

    // Pair registration stays symmetric in the dictionary
    private void RegisterPair(ZooAnimal first, ZooAnimal second)
    {
        RemovePairing(first.Id);
        RemovePairing(second.Id);
        _monogamousPairs[first.Id] = second.Id;
        _monogamousPairs[second.Id] = first.Id;
    }

    // Removing one side also removes the reverse link
    private void RemovePairing(Guid animalId)
    {
        if (!_monogamousPairs.Remove(animalId, out var partnerId))
            return;

        _monogamousPairs.Remove(partnerId);
    }

    // Dead animals cannot stay inside a monogamous pair
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
