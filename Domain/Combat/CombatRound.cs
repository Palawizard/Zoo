namespace Zoo.Domain.Combat;

public sealed record CombatRound(
    string AttackerName,
    string DefenderName,
    int Damage,
    int DefenderHpAfter);
