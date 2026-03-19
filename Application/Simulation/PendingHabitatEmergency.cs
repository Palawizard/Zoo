using Zoo.Domain.Animals;
using Zoo.Domain.Events;

namespace Zoo.Application.Simulation;

public sealed record PendingHabitatEmergency(
    Guid HabitatId,
    SpeciesType Species,
    ZooEventType CauseType,
    string CauseDescription,
    IReadOnlyList<ZooAnimal> DisplacedAnimals,
    decimal ReplacementHabitatCost
);
