<div align="center">

# Zoo

### Simulateur de zoo en C# / .NET 10 avec moteur de règles, interface console et dashboard desktop Avalonia

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Desktop UI](https://img.shields.io/badge/Desktop-Avalonia_11.3.12-0F172A?style=for-the-badge)](https://avaloniaui.net/)

[Features](#features) • [Quick Start](#quick-start) • [Architecture](#architecture) • [Tests](#tests)

</div>

---

## Features

<table>
<tr>
<td width="50%">

### Simulation & règles métier
- Avancement au tour par tour: `1 tour = 1 jour`
- Gestion de la faim, de la maladie, du vieillissement et de la mortalité
- Saisons haute/basse influençant la fréquentation
- Historique complet des événements de simulation

</td>
<td width="50%">

### Économie du zoo
- Budget initial de `80 000 EUR`
- Achat / vente d'animaux et d'habitats
- Achat de nourriture selon le type requis
- Grand livre comptable avec toutes les transactions

</td>
</tr>
<tr>
<td>

### Reproduction & population
- Gestation, incubation, ponte et naissances
- Mortalité infantile selon l'espèce
- Blocage temporaire de reproduction après arrivée
- Contraintes d'espace avant autorisation de reproduction

</td>
<td>

### Interfaces disponibles
- Application console interactive
- Interface desktop Avalonia pour piloter la simulation
- Dialogues de confirmation, urgences d'habitat et événements
- Nommage des nouveau-nés dans l'interface desktop

</td>
</tr>
</table>

---

## Quick Start

### Prérequis
- `.NET SDK 10.0+`
- Git

### Installation

```bash
git clone https://github.com/Palawizard/Zoo.git
cd Zoo
dotnet restore
```

### Lancer la version console

```bash
dotnet run
```

Application lancée depuis `Program.cs`.

### Lancer l'interface desktop

```bash
dotnet run --project Zoo.Desktop/Zoo.Desktop.csproj
```

### Lancer les tests

```bash
dotnet test Zoo.sln
```

Validation locale effectuée sur ce dépôt:

- SDK détecté: `.NET 10.0.104`
- Tests: `22 / 22` passés
- Build desktop: OK

Pourquoi cette stack ?

    C# / .NET 10 : code métier clair, typé, structuré et simple à tester

    Avalonia : interface desktop moderne et multiplateforme

    xUnit : couverture rapide des règles critiques

    Architecture par couches : Domain / Application / Presentation séparés

---

## Architecture

Le projet est organisé autour d'un moteur de simulation central (`ZooSimulationService`) qui pilote les animaux, habitats, finances, visiteurs, événements et règles mensuelles / annuelles.

### Points d'entrée
- `Program.cs` démarre l'application console
- `Zoo.Desktop/Program.cs` démarre le dashboard Avalonia

### Structure du projet

```text
├── Program.cs                           # Entrée console
├── Zoo.csproj                          # Projet principal .NET 10
├── Zoo.sln                             # Solution complète
├── Application/
│   └── Simulation/
│       ├── ZooSimulationService.cs     # Moteur principal de la simulation
│       ├── TurnAdvanceState.cs         # État d'avancement avec interruption
│       ├── PendingHabitatEmergency.cs  # Urgence d'habitat en attente
│       └── HabitatEmergencyResolution.cs
├── Domain/
│   ├── Animals/                        # Profils, états, espèces, sexe, cycle de vie
│   ├── Habitats/                       # Capacités, coûts, pertes, usines d'habitats
│   ├── Finance/                        # Marché animalier, ledger, transactions
│   ├── Feeding/                        # Types de nourriture et coûts au kg
│   ├── Visitors/                       # Flux visiteurs et calcul des revenus
│   ├── Reproduction/                   # Règles et service de reproduction
│   └── Events/                         # Journal d'événements métier
├── Presentation/
│   └── Console/
│       ├── ZooConsoleApp.cs            # Boucle de menu console
│       ├── ZooConsolePrinter.cs        # Rendu console
│       ├── ConsoleInput.cs             # Lecture et validation des saisies
│       └── MenuOption.cs
├── Zoo.Desktop/
│   ├── MainWindow.axaml                # Dashboard principal
│   ├── MainWindowViewModel.cs          # Logique UI + synchronisation avec le moteur
│   ├── AnimalNamingDialog.cs           # Renommage des nouveau-nés
│   ├── HabitatEmergencyDialog.cs       # Décision de relogement / euthanasie
│   ├── EventDialog.cs                  # Popups d'événements
│   └── ConfirmationDialog.cs           # Confirmations d'actions
└── Tests/
    └── Zoo.Tests/
        ├── Unit/                       # Tests unitaires métier
        └── Integration/                # Scénario long de simulation
```

---

## Règles de simulation

### Espèces gérées
- `Tiger`
- `Eagle`
- `Rooster`

### Nourriture
- Les tigres et aigles consomment de la `Meat`
- Les coqs / poules consomment des `Seeds`
- Une femelle en gestation consomme `2x` sa ration journalière

### Santé & survie
- Un animal peut être `Healthy`, `Sick` ou `Dead`
- La faim progresse si le stock ne couvre pas le besoin journalier
- Les maladies apparaissent selon une probabilité annuelle propre à l'espèce
- En fin de maladie, l'animal guérit ou meurt selon un tirage aléatoire
- La mort peut aussi venir de la faim ou de la vieillesse

### Reproduction
- Les adultes arrivés au zoo ne peuvent pas se reproduire pendant `30 jours`
- Les tigres utilisent une gestation avec portée et cooldown entre portées
- Les aigles sont monogames
- Les aigles et poules pondent des oeufs avec incubation
- La reproduction n'est déclenchée que si la capacité future des habitats est suffisante

### Visiteurs & revenus
- Seuls les animaux vivants, exposés au public et présents dans un habitat génèrent des revenus
- Haute saison: de `mai` à `septembre`
- Basse saison: d'`octobre` à `avril`
- Le revenu varie selon l'espèce et une légère part aléatoire

### Événements exceptionnels mensuels
- Incendie d'habitat
- Vol d'un animal
- Parasites détruisant une partie des graines
- Viande avariée détruisant une partie du stock

### Subventions annuelles
- Les tigres et aigles donnent lieu à une subvention annuelle d'espèces protégées

---

## Expérience utilisateur

### Version console
- Avancer d'un jour
- Acheter de la nourriture
- Acheter un habitat
- Acheter / ajouter un animal dans un habitat compatible
- Vendre un animal
- Vendre un habitat vide
- Afficher l'état complet du zoo

### Version desktop
- Avancer de `1 jour`, `1 semaine`, `1 mois` ou `N jours`
- Suivre les métriques principales: cash, stock, population, exposition, revenu projeté
- Parcourir les événements récents, importants et le ledger financier
- Sélectionner un habitat puis inspecter ses animaux
- Acheter / vendre via dialogues de confirmation
- Résoudre les destructions d'habitats par relogement ou euthanasie
- Renommer les nouveau-nés dès leur arrivée

Note: l'interface desktop est la plus complète pour exploiter les interruptions de simulation et les dialogues métier.

---

## Tests

La solution contient une suite `xUnit` couvrant les règles principales:

- faim et décès par starvation
- besoins alimentaires pendant la gestation
- maladies et durée d'infection
- reproduction, monogamie, cooldowns et contraintes d'espace
- économie: achats, ventes, ledger, urgences d'habitats
- visiteurs et variation de revenus
- scénario d'intégration sur `24 mois`

Commande:

```bash
dotnet test Zoo.sln
```

---

## Commandes utiles

```bash
dotnet restore
dotnet run
dotnet run --project Zoo.Desktop/Zoo.Desktop.csproj
dotnet build Zoo.Desktop/Zoo.Desktop.csproj
dotnet test Zoo.sln
```

Conventional Commits :

```text
feat(scope): ajout d'une fonctionnalité
fix(scope): correction d'un bug
docs(readme): mise à jour du README
test(scope): ajout ou mise à jour des tests
```

<div align="center">
Créé en C# / .NET 10 / Avalonia / xUnit
</div>
