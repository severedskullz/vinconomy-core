# Meal spoilage regression checks

Requires the .NET 10 SDK, a local Vintage Story 1.22.7 installation, and Commercially 0.1.1. Game and mod dependency binaries are not included.

Run from the repository root:

```sh
dotnet run --project tests/MealSpoilage -c Release \
  -p:VINTAGE_STORY=/path/to/vintagestory \
  -p:CommerciallyPath=/path/to/Commercially.dll
```

To verify the reproduction against an original release DLL instead of building the patched project, add `-p:VinconomyAssembly=/path/to/original/Vinconomy.dll`. The original 6.0.3 DLL should fail the preservation checks.

The executable uses real Vinconomy inventories and the inherited inventory-provider callback, the game's `BlockCookedContainerBase` and `BlockMeal` nested-slot callbacks, and the actual item transition code. World services, ownership, and room temperature are fixtures; no rendering, game server, real world save, or mock spoilage formula is involved.

Checks cover 24 hours of actual ingredient aging in cooked pots and bowls, cold/normal/hot room multipliers, configured rates including zero, disabled decay, admin ownership, a non-unit incoming multiplier, preview and currency ingredients, the transfer slot, ordinary generic-shop food and containers, non-stall inventories, and non-perish transitions. Stack quantities are checked after aging. This does not replace an in-game multiplayer observation after installation.
