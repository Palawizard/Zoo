using System;
using System.Linq;
using Zoo.Domain.Animals;
using Zoo.Domain.Events;

namespace Zoo.Application.Simulation;

public sealed partial class ZooSimulationService
{
    public void TryApplyMonthlyExceptionalEvents(int dayOfMonth)
    {
        var daysInMonth = GetDaysInMonth(CurrentMonth);
        if (dayOfMonth < 1 || dayOfMonth > daysInMonth)
            throw new ArgumentOutOfRangeException(nameof(dayOfMonth), $"Day must be between 1 and {daysInMonth}.");
        if (dayOfMonth != 1 || _lastExceptionalEventsMonth == CurrentMonth)
            return;

        _lastExceptionalEventsMonth = CurrentMonth;

        TryApplyMonthlyFire();
        TryApplyMonthlyTheft();
        TryApplyMonthlyPests();
        TryApplyMonthlySpoiledMeat();
    }

    public void ProcessDailyFeeding()
    {
        foreach (var animal in _animals.Where(current => current.IsAlive))
        {
            var requiredKg = animal.GetDailyFoodNeedKg();
            var providedKg = ConsumeFromStock(animal.Profile.FoodType, requiredKg);
            animal.ApplyDailyFeeding(providedKg);

            var dailyOutcome = animal.AdvanceOneDay();
            LogDailyOutcome(animal, dailyOutcome);

            if (animal.TryCatchDiseaseToday())
            {
                AddEvent(
                    ZooEventType.Disease,
                    $"{animal.Name} became sick.");
            }
        }
    }

    public void NextTurn()
    {
        if (PendingHabitatEmergency is not null || _pendingTurnAwaitingCompletion)
            throw new InvalidOperationException("Resolve the pending habitat emergency before advancing the simulation.");

        TurnNumber++;
        ProcessDailyTurn();
        AdvanceCalendar();

        if (CurrentDayOfMonth == 1)
        {
            ProcessMonthlyTurn();

            if (CurrentMonth == 1)
                ProcessYearlyTurn();
        }

        AddTurnAdvancedEvent();
    }

    public TurnAdvanceState AdvanceTurnWithInterruptions()
    {
        if (!_interactiveHabitatEmergencies)
        {
            NextTurn();
            return TurnAdvanceState.Completed;
        }

        if (PendingHabitatEmergency is not null || _pendingTurnAwaitingCompletion)
            return TurnAdvanceState.AwaitingHabitatEmergencyDecision;

        TurnNumber++;
        ProcessDailyTurn();
        AdvanceCalendar();

        if (CurrentDayOfMonth == 1)
        {
            _pendingTurnRequiresYearlyProcessing = CurrentMonth == 1;

            if (!ProcessMonthlyTurnWithPossiblePause())
            {
                _pendingTurnAwaitingCompletion = true;
                return TurnAdvanceState.AwaitingHabitatEmergencyDecision;
            }

            if (_pendingTurnRequiresYearlyProcessing)
                ProcessYearlyTurn();
        }

        AddTurnAdvancedEvent();
        _pendingTurnRequiresYearlyProcessing = false;
        return TurnAdvanceState.Completed;
    }

    private void TryApplyMonthlyFire()
    {
        if (!IsEventTriggered(0.01m) || _habitats.Count == 0)
            return;

        var habitat = _habitats[Random.Shared.Next(_habitats.Count)];
        DestroyHabitat(
            habitat,
            ZooEventType.Fire,
            $"A fire destroyed one {habitat.Species} habitat.");
    }

    private void TryApplyMonthlyTheft()
    {
        if (!IsEventTriggered(0.01m))
            return;

        var candidates = _animals.Where(animal => animal.IsAlive).ToList();
        if (candidates.Count == 0)
            return;

        var victim = candidates[Random.Shared.Next(candidates.Count)];
        RemoveAnimalFromZoo(victim);

        AddEvent(
            ZooEventType.Theft,
            $"{victim.Name} was stolen from the zoo.");
    }

    private void TryApplyMonthlyPests()
    {
        if (!IsEventTriggered(0.20m) || SeedsStockKg <= 0m)
            return;

        var newStock = ReduceByPercent(SeedsStockKg, 0.10m);
        var lostKg = SeedsStockKg - newStock;
        SeedsStockKg = newStock;

        AddEvent(
            ZooEventType.Pests,
            $"Pests destroyed {lostKg:0.##} kg of seeds.");
    }

    private void TryApplyMonthlySpoiledMeat()
    {
        if (!IsEventTriggered(0.10m) || MeatStockKg <= 0m)
            return;

        var newStock = ReduceByPercent(MeatStockKg, 0.20m);
        var lostKg = MeatStockKg - newStock;
        MeatStockKg = newStock;

        AddEvent(
            ZooEventType.SpoiledMeat,
            $"{lostKg:0.##} kg of meat spoiled.");
    }

    private void LogDailyOutcome(ZooAnimal animal, AnimalDailyOutcome dailyOutcome)
    {
        if (dailyOutcome.DiedOfOldAge)
        {
            AddEvent(ZooEventType.EndOfLife, $"{animal.Name} died of old age.");
            return;
        }

        if (dailyOutcome.DiedOfDisease)
        {
            AddEvent(ZooEventType.DiseaseDeath, $"{animal.Name} died from disease.");
            return;
        }

        if (dailyOutcome.DiedOfHunger)
        {
            AddEvent(ZooEventType.HungerDeath, $"{animal.Name} died from starvation.");
            return;
        }

        if (dailyOutcome.RecoveredFromDisease)
        {
            AddEvent(ZooEventType.DiseaseRecovered, $"{animal.Name} recovered from disease.");
        }
    }

    private void ProcessDailyTurn()
    {
        ProcessDailyFeeding();
        ProcessGestations();
        ProcessEggIncubations();
        TryStartPregnancies();
    }

    private void ProcessMonthlyTurn()
    {
        TryApplyMonthlyExceptionalEvents(CurrentDayOfMonth);
        CompleteMonthlyTurn();
    }

    private bool ProcessMonthlyTurnWithPossiblePause()
    {
        TryApplyMonthlyExceptionalEvents(CurrentDayOfMonth);
        if (PendingHabitatEmergency is not null)
            return false;

        CompleteMonthlyTurn();
        return true;
    }

    private void CompleteMonthlyTurn()
    {
        ProgressMonthlyReproductionCooldowns();
        TryEggLayingForCurrentMonth();
        ProcessMonthlyHabitatOutcomes();
        CollectMonthlyVisitorRevenue();
    }

    private void ProcessMonthlyHabitatOutcomes()
    {
        foreach (var habitat in _habitats)
        {
            var monthlyOutcome = habitat.ProcessMonth(Random.Shared);

            foreach (var animal in monthlyOutcome.NewlySickAnimals)
            {
                AddEvent(
                    ZooEventType.Disease,
                    $"{animal.Name} became sick in the {habitat.Species} habitat.");
            }

            foreach (var animal in monthlyOutcome.NaturalLosses)
            {
                AddEvent(
                    ZooEventType.HabitatMonthlyLoss,
                    $"{animal.Name} died during monthly habitat losses in the {habitat.Species} habitat.");
            }

            foreach (var animal in monthlyOutcome.OverpopulationLosses)
            {
                AddEvent(
                    ZooEventType.OverpopulationDeath,
                    $"{animal.Name} died because of overpopulation in the {habitat.Species} habitat.");
            }
        }
    }

    private void ProgressMonthlyReproductionCooldowns()
    {
        foreach (var animal in _animals.Where(current => current.IsAlive))
            animal.ProgressReproductionOneMonth();
    }

    private decimal CollectMonthlyVisitorRevenue()
    {
        return CollectVisitorRevenue(IsHighSeason);
    }

    private void ProcessYearlyTurn()
    {
        var tigerCount = _animals.Count(animal => animal.IsAlive && animal.Species == SpeciesType.Tiger);
        var eagleCount = _animals.Count(animal => animal.IsAlive && animal.Species == SpeciesType.Eagle);
        var subsidy = (tigerCount * 43800m) + (eagleCount * 2190m);

        if (subsidy <= 0m)
            return;

        AddCash(subsidy, "Protected species annual subsidy", "Subsidy");
        AddEvent(
            ZooEventType.AnnualSubsidy,
            $"Protected species subsidy added {subsidy:0.##}€.");
    }

    private void AdvanceCalendar()
    {
        var daysInMonth = GetDaysInMonth(CurrentMonth);
        if (CurrentDayOfMonth < daysInMonth)
        {
            CurrentDayOfMonth++;
            return;
        }

        CurrentDayOfMonth = 1;
        if (CurrentMonth < 12)
        {
            CurrentMonth++;
            return;
        }

        CurrentMonth = 1;
        CurrentYear++;
    }

    private void AddTurnAdvancedEvent()
    {
        AddEvent(
            ZooEventType.TurnAdvanced,
            $"Turn {TurnNumber} completed. Current date is {CurrentDayOfMonth}/{CurrentMonth}/{CurrentYear}.");
    }

    private static int GetDaysInMonth(int month)
    {
        return month switch
        {
            1 => 31,
            2 => 28,
            3 => 31,
            4 => 30,
            5 => 31,
            6 => 30,
            7 => 31,
            8 => 31,
            9 => 30,
            10 => 31,
            11 => 30,
            12 => 31,
            _ => throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.")
        };
    }

    private static decimal ReduceByPercent(decimal value, decimal percent)
    {
        if (value <= 0m)
            return 0m;
        if (percent <= 0m)
            return value;
        if (percent >= 1m)
            return 0m;

        return value * (1m - percent);
    }

    private static bool IsEventTriggered(decimal probability)
    {
        if (probability <= 0m)
            return false;
        if (probability >= 1m)
            return true;

        return (decimal)Random.Shared.NextDouble() < probability;
    }
}
