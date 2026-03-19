using Zoo.Domain.Animals;

namespace Zoo.Domain.Combat;

public static class CombatService
{
    private const int StartingHp = 100;
    private const int MaxRounds  = 50;

    public static CombatResult Fight(Animal first, Animal second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var statsA = CombatStatsCatalog.GetStats(first);
        var statsB = CombatStatsCatalog.GetStats(second);

        var (attacker, attackerStats, defender, defenderStats) =
            DetermineOrder(first, statsA, second, statsB);

        var attackerHp = StartingHp;
        var defenderHp = StartingHp;
        var rounds = new List<CombatRound>();

        for (var i = 0; i < MaxRounds; i++)
        {
            var damage = ComputeDamage(attackerStats.Force, defenderStats.Defense);
            defenderHp -= damage;
            rounds.Add(new CombatRound(attacker.Name, defender.Name, damage, Math.Max(0, defenderHp)));

            if (defenderHp <= 0)
                return new CombatResult(attacker, defender, IsDraw: false, rounds);

            var counterDamage = ComputeDamage(defenderStats.Force, attackerStats.Defense);
            attackerHp -= counterDamage;
            rounds.Add(new CombatRound(defender.Name, attacker.Name, counterDamage, Math.Max(0, attackerHp)));

            if (attackerHp <= 0)
                return new CombatResult(defender, attacker, IsDraw: false, rounds);
        }

        // maxHP gagne
        if (attackerHp > defenderHp)
            return new CombatResult(attacker, defender, IsDraw: false, rounds);
        if (defenderHp > attackerHp)
            return new CombatResult(defender, attacker, IsDraw: false, rounds);

        return new CombatResult(attacker, Loser: null, IsDraw: true, rounds);
    }

    private static (Animal Attacker, CombatStats AttackerStats, Animal Defender, CombatStats DefenderStats)
        DetermineOrder(Animal a, CombatStats statsA, Animal b, CombatStats statsB)
    {
        if (statsA.Vitesse > statsB.Vitesse) {
            return (a, statsA, b, statsB);
            };
        if (statsB.Vitesse > statsA.Vitesse) {
            return (b, statsB, a, statsA);
        };

        // Égalité, tirage au sort
        return Random.Shared.Next(2) == 0
            ? (a, statsA, b, statsB)
            : (b, statsB, a, statsA);
    }

    private static int ComputeDamage(int force, int defense) =>
        Math.Max(1, force - defense);
}
