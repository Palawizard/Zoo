using Zoo.Domain.Animals;

namespace Zoo.Domain.Combat;

//record non-héritable pour encap
public sealed record CombatResult(
    Animal Winner,
    Animal? Loser,
    bool IsDraw,
    IReadOnlyList<CombatRound> Rounds);
