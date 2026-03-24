using Zoo.Domain.Animals;
using Zoo.Domain.Events;
using Zoo.Domain.Habitats;

namespace Zoo.Application.Simulation;

public sealed partial class ZooSimulationService
{
    /// <summary>
    /// Destroys a habitat and creates a pending emergency when needed
    /// </summary>
    public void DestroyHabitat(Habitat habitat, ZooEventType causeType, string description)
    {
        ArgumentNullException.ThrowIfNull(habitat);
        if (!_habitats.Remove(habitat))
            return;

        // Dead animals are ignored here because the emergency concerns living ones
        var displacedAnimals = habitat.Animals
            .OfType<ZooAnimal>()
            .Where(animal => animal.IsAlive)
            .ToList();

        foreach (var animal in displacedAnimals)
            habitat.RemoveAnimal(animal);

        AddEvent(causeType, description);

        if (displacedAnimals.Count == 0)
            return;

        if (!_interactiveHabitatEmergencies)
        {
            EuthanizeDisplacedAnimals(habitat.Species, displacedAnimals);
            return;
        }

        PendingHabitatEmergency = new PendingHabitatEmergency(
            habitat.Id,
            habitat.Species,
            causeType,
            description,
            displacedAnimals,
            habitat.BuyPrice);
    }

    /// <summary>
    /// Resolves the current habitat emergency
    /// </summary>
    public bool TryResolvePendingHabitatEmergency(HabitatEmergencyResolution resolution, out string failureReason)
    {
        failureReason = string.Empty;

        if (PendingHabitatEmergency is null)
        {
            failureReason = "No habitat emergency is pending.";
            return false;
        }

        var pendingEmergency = PendingHabitatEmergency;
        if (resolution == HabitatEmergencyResolution.RehouseAnimals &&
            !TryRehouseDisplacedAnimals(pendingEmergency, out failureReason))
        {
            return false;
        }

        if (resolution == HabitatEmergencyResolution.EuthanizeAnimals)
        {
            EuthanizeDisplacedAnimals(pendingEmergency.Species, pendingEmergency.DisplacedAnimals);
        }
        else if (resolution != HabitatEmergencyResolution.RehouseAnimals)
        {
            failureReason = "Unknown habitat emergency resolution.";
            return false;
        }

        PendingHabitatEmergency = null;

        if (_pendingTurnAwaitingCompletion)
        {
            CompleteMonthlyTurn();

            if (_pendingTurnRequiresYearlyProcessing)
                ProcessYearlyTurn();

            AddTurnAdvancedEvent();
        }

        _pendingTurnAwaitingCompletion = false;
        _pendingTurnRequiresYearlyProcessing = false;
        return true;
    }

    // A new habitat is bought only if the existing capacity is not enough
    private bool TryRehouseDisplacedAnimals(PendingHabitatEmergency pendingEmergency, out string failureReason)
    {
        var availableSlots = GetAvailableHabitatSlots(pendingEmergency.Species);
        var displacedCount = pendingEmergency.DisplacedAnimals.Count(animal => animal.IsAlive);

        if (availableSlots < displacedCount)
        {
            if (!BuyHabitat(pendingEmergency.Species))
            {
                failureReason = $"Not enough cash to buy a replacement {pendingEmergency.Species} habitat.";
                return false;
            }
        }

        foreach (var animal in pendingEmergency.DisplacedAnimals.Where(current => current.IsAlive))
        {
            if (TryPlaceAnimalInHabitat(animal))
                continue;

            // This should stay rare because capacity is checked before the loop
            failureReason = $"No free {pendingEmergency.Species} habitat slot is available for {animal.Name}.";
            return false;
        }

        AddEvent(
            ZooEventType.HabitatAnimalsRehoused,
            $"{displacedCount} animal(s) from the destroyed {pendingEmergency.Species} habitat were rehoused.");
        failureReason = string.Empty;
        return true;
    }

    // Non-rehoused animals are killed but kept in the animal list
    private void EuthanizeDisplacedAnimals(SpeciesType species, IReadOnlyList<ZooAnimal> displacedAnimals)
    {
        var euthanizedCount = 0;

        foreach (var animal in displacedAnimals.Where(current => current.IsAlive))
        {
            RemovePairing(animal.Id);
            animal.Kill();
            euthanizedCount++;
        }

        AddEvent(
            ZooEventType.HabitatAnimalsEuthanized,
            $"{euthanizedCount} animal(s) from the destroyed {species} habitat were euthanized.");
    }
}
