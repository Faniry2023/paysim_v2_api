# API-PAYSIM

Projet API PAYSIM — API RESTful en .NET 8 pour la gestion des développeurs, projets et paiements.

## Description

Cette API fournit des endpoints pour gérer les utilisateurs, développeurs, projets, historiques et paiements. Elle utilise Entity Framework Core pour la persistance et contient une implémentation SignalR pour le hub de paiement (`HubService/PayHub.cs`).

## Prérequis

- .NET 8 SDK
- SQL Server (ou autre provider configuré dans `appsettings.json`)
- Outils EF Core (`dotnet-ef`) pour appliquer les migrations

## Structure importante

- [Program.cs](Program.cs) : point d'entrée
- `Controllers/` : endpoints API
- `Data/DataContext.cs` : DbContext EF
- `Migrations/` : migrations EF Core (déjà présentes)
- `HubService/PayHub.cs` : SignalR hub pour les paiements
- `appsettings.json` / `appsettings.Development.json` : configuration

## Installation et exécution

1. Restaurez les dépendances :

```bash
dotnet restore
```

2. (Optionnel) Si `dotnet-ef` n'est pas installé :

```bash
dotnet tool install --global dotnet-ef
```

3. Mettez à jour la chaîne de connexion dans `appsettings.json` (clé `ConnectionStrings:DefaultConnection`).

4. Appliquez les migrations :

```bash
dotnet ef database update
```

5. Exécutez l'application :

```bash
dotnet run
```

L'API sera disponible par défaut sur `https://localhost:5001` ou l'URL configurée dans `Properties/launchSettings.json`.

## Endpoints principaux

Voir le dossier `Controllers/` pour la liste complète. Exemples :

- `UserController` — gestion des utilisateurs
- `DeveloperAndProjectController` / `ProjectController` — gestion des projets et développeurs
- Contrôleurs liés aux paiements se trouvent dans `PayHelper/` et `PaymentModel` dans `Models/`

## Base de données & migrations

Les migrations EF Core sont déjà présentes dans `Migrations/`. Utilisez `dotnet ef database update` pour créer/mettre à jour la base.

Un fichier d'initialisation de données est disponible : `TestDeveloper/InitializeBdDeveloper.cs`.

## SignalR

Le hub SignalR de paiement est implémenté dans `HubService/PayHub.cs`. Configurez `Startup/Program` pour mapper le hub si nécessaire.

## Tests & développement

- Pour le développement local, utilisez `appsettings.Development.json`.
- Les helpers utiles sont dans `Helpers/` (ex. `JwtHelper`, `EncryptionPasswordHelper`).

## Contribuer

1. Forkez le dépôt
2. Créez une branche descriptive
3. Ouvrez une PR avec description des changements

## Licence

À préciser (aucune licence ajoutée pour l'instant).

---

Fichier créé à la racine du projet. N'hésitez pas à me dire si vous voulez ajouter des sections (ex. exemple d'API, diagramme, checklist de déploiement).
