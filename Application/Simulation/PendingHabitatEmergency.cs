using Zoo.Domain.Animals;
using Zoo.Domain.Events;

namespace Zoo.Application.Simulation;

/// <summary>
/// Describes a habitat emergency waiting for a player decision
/// </summary>
public sealed record PendingHabitatEmergency(
    Guid HabitatId,
    SpeciesType Species,
    ZooEventType CauseType,
    string CauseDescription,
    IReadOnlyList<ZooAnimal> DisplacedAnimals,
    decimal ReplacementHabitatCost
);
