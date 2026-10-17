using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Watermelon.GlobalUpgrades;

namespace Watermelon
{
    // QA helper: walks the mission chain in play mode and checks that every mission target
    // is visible and usable at the moment the mission starts (softlock detection).
    public static class MissionChainChecker
    {
        private const BindingFlags PRIVATE_FIELD = BindingFlags.NonPublic | BindingFlags.Instance;

        [MenuItem("Actions/QA/Check Active Mission", priority = 300)]
        private static void CheckActiveMissionMenu()
        {
            Debug.Log("[MissionCheck] " + CheckActiveMission());
        }

        [MenuItem("Actions/QA/Complete Active Mission", priority = 301)]
        private static void CompleteActiveMissionMenu()
        {
            Debug.Log("[MissionCheck] " + CheckActiveMission() + " -> " + CompleteActiveMission());
        }

        [MenuItem("Actions/QA/Check Active Mission", true)]
        [MenuItem("Actions/QA/Complete Active Mission", true)]
        private static bool IsGamePlaying()
        {
            return Application.isPlaying;
        }

        // Returns "OK ..." or "PROBLEM ..." for the active mission
        public static string CheckActiveMission()
        {
            Mission mission = MissionsController.ActiveMission;
            if (mission == null)
                return "no active mission";

            string header = $"#{GetMissionIndex(mission) + 1} \"{mission.DisplayTitle}\" ({mission.GetType().Name}, {mission.MissionStage}): ";
            List<string> problems = new List<string>();

            if (mission is BuildingMission buildingMission)
            {
                CheckComplex(buildingMission.LinkedBuilding, problems);
            }
            else if (mission is GroundUnlockMission groundMission)
            {
                CheckComplex(groundMission.GroundTile, problems);
            }
            else if (mission is HelperMission helperMission)
            {
                CheckHelper(helperMission.HelperBehavior, problems);
            }
            else if (mission is UpgradeMission upgradeMission)
            {
                CheckUpgrade(upgradeMission, problems);
            }
            else if (mission is CollectMission collectMission)
            {
                CheckCollect(collectMission.CurrencyType, problems);
            }
            else if (mission is KillEnemiesMission killMission)
            {
                CheckEnemies(killMission, problems);
            }
            else if (mission is SubworldEnterMission)
            {
                SubworldEntrance entrance = GetField<SubworldEntrance>(mission, "subworldEntranceRef");
                if (entrance == null || !entrance.gameObject.activeInHierarchy)
                    problems.Add("sub-world entrance is missing or hidden");
            }

            return (problems.Count == 0 ? "OK " : "PROBLEM ") + header + string.Join("; ", problems);
        }

        // Does the mission's job directly (purchase/construct, upgrade, add currency). Returns what was done.
        public static string CompleteActiveMission()
        {
            Mission mission = MissionsController.ActiveMission;
            if (mission == null)
                return "nothing to complete";

            if (mission.MissionStage == Mission.Stage.Active)
            {
                if (mission is BuildingMission buildingMission)
                {
                    Unlock(buildingMission.LinkedBuilding);
                }
                else if (mission is GroundUnlockMission groundMission)
                {
                    Unlock(groundMission.GroundTile);
                }
                else if (mission is HelperMission helperMission)
                {
                    UnlockHelper(helperMission.HelperBehavior);
                }
                else if (mission is UpgradeMission upgradeMission)
                {
                    IUpgrade upgrade = GetUpgrade(upgradeMission);
                    if (upgrade is AbstractLocalUpgrade notReady && !notReady.IsInitialised)
                        return "upgrade not initialised";

                    int targetLevel = GetField<int>(upgradeMission, "upgradeLevel");
                    for (int i = 0; i < 20 && upgrade.UpgradeLevel < targetLevel && !upgrade.IsMaxedOut; i++)
                    {
                        upgrade.UpgradeStage();
                    }
                }
                else if (mission is CollectMission collectMission)
                {
                    int amount = GetField<int>(collectMission, "amount");
                    CurrencyController.Add(collectMission.CurrencyType, amount);
                }
                else if (mission is KillEnemiesMission killMission)
                {
                    object save = GetField<object>(killMission, "save");
                    save.GetType().GetField("Killed").SetValue(save, killMission.Amount);
                    killMission.GetType().GetMethod("CheckStage", PRIVATE_FIELD).Invoke(killMission, null);
                }
                else if (mission is OwnCurrencyMission ownMission)
                {
                    int missing = ownMission.Amount - CurrencyController.GetCurrency(ownMission.CurrencyType).Amount;
                    if (missing > 0) CurrencyController.Add(ownMission.CurrencyType, missing);
                }
                else if (mission is SubworldEnterMission)
                {
                    mission.GetType().GetMethod("OnSubworldEnterTriggered", PRIVATE_FIELD).Invoke(mission, null);
                }
                else
                {
                    return "can't auto-complete " + mission.GetType().Name;
                }
            }

            if (mission.MissionStage == Mission.Stage.Finished)
            {
                MissionsController.CompleteMission();

                return "completed";
            }

            return "still " + mission.MissionStage;
        }

        private static void CheckComplex(IUnlockableComplex complex, List<string> problems)
        {
            Component component = complex as Component;
            if (component == null)
            {
                problems.Add("target is missing");
                return;
            }

            if (IsOpen(complex))
                return;

            if (!component.gameObject.activeInHierarchy)
            {
                problems.Add($"target '{component.name}' is hidden");
                return;
            }

            if (!complex.CanBePurchased && !complex.CanBeConstructed)
            {
                problems.Add($"target '{component.name}' can't be purchased or constructed");
                return;
            }

            if (complex.CanBePurchased)
            {
                Component purchaser = GetField<Component>(complex, "purchaser");
                if (purchaser != null && !purchaser.gameObject.activeInHierarchy)
                    problems.Add($"purchase point of '{component.name}' is hidden");

                foreach (Resource resource in complex.CostLeft)
                {
                    if (resource.amount > 0)
                        CheckCollect(resource.currency, problems, $"cost of '{component.name}': ");
                }
            }
        }

        private static void CheckHelper(HelperBehavior helper, List<string> problems)
        {
            if (helper.IsOpened)
                return;

            GroundTileComplexBehavior[] tiles = GetField<GroundTileComplexBehavior[]>(helper, "linkedTiles");
            BuildingComplexBehavior[] buildings = GetField<BuildingComplexBehavior[]>(helper, "linkedBuildings");

            List<string> linkedProblems = new List<string>();
            bool anyUsable = false;

            if (tiles != null)
            {
                foreach (var tile in tiles)
                {
                    if (tile == null) continue;
                    List<string> tileProblems = new List<string>();
                    CheckComplex(tile, tileProblems);
                    if (tileProblems.Count == 0) anyUsable = true; else linkedProblems.AddRange(tileProblems);
                }
            }

            if (buildings != null)
            {
                foreach (var building in buildings)
                {
                    if (building == null) continue;
                    List<string> buildingProblems = new List<string>();
                    CheckComplex(building, buildingProblems);
                    if (buildingProblems.Count == 0) anyUsable = true; else linkedProblems.AddRange(buildingProblems);
                }
            }

            if (!anyUsable)
            {
                problems.Add("no linked tile/building can be unlocked to free the helper");
                problems.AddRange(linkedProblems);
            }
        }

        private static void CheckUpgrade(UpgradeMission mission, List<string> problems)
        {
            IUpgrade upgrade = GetUpgrade(mission);
            if (upgrade == null)
            {
                problems.Add("upgrade is missing");
                return;
            }

            int targetLevel = GetField<int>(mission, "upgradeLevel");

            if (upgrade is AbstractLocalUpgrade notReady && !notReady.IsInitialised)
            {
                problems.Add($"building of upgrade '{upgrade.Title}' is not open (upgrade not initialised)");
                return;
            }

            if (upgrade.UpgradeLevel >= targetLevel)
                return;

            if (upgrade.IsMaxedOut)
                problems.Add($"upgrade '{upgrade.Title}' is maxed at level {upgrade.UpgradeLevel} < {targetLevel}");

            if (upgrade is AbstractLocalUpgrade localUpgrade)
            {
                if (localUpgrade.IsLocked)
                    problems.Add($"upgrade '{upgrade.Title}' is locked");

                IUnlockableComplex building = localUpgrade.GetComponentInParent<IUnlockableComplex>();
                if (!localUpgrade.gameObject.activeInHierarchy || (building != null && !IsOpen(building)))
                    problems.Add($"building of upgrade '{upgrade.Title}' is not open");
            }
            else
            {
                // global upgrades are bought in the upgrades building
                bool upgradesBuildingOpen = false;
                foreach (UpgradesBuilding upgradesBuilding in Object.FindObjectsByType<UpgradesBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    IUnlockableComplex complex = upgradesBuilding.GetComponentInParent<IUnlockableComplex>();
                    if (complex == null || IsOpen(complex))
                        upgradesBuildingOpen = true;
                }

                if (!upgradesBuildingOpen)
                    problems.Add("upgrades building is not available");
            }
        }

        // there must be a place where this enemy appears (spawn point in an open area or the boss arena)
        private static void CheckEnemies(KillEnemiesMission mission, List<string> problems)
        {
            if (mission.EnemyType == EnemyType.GhostCaptain && !mission.AnyEnemyType)
            {
                if (Object.FindFirstObjectByType<BossArena>(FindObjectsInactive.Include) == null)
                    problems.Add("no boss arena");
                return;
            }

            foreach (EnemySpawnPoint point in Object.FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None))
            {
                if (!point.gameObject.activeInHierarchy) continue;
                EnemyType type = GetField<EnemyType>(point, "enemyType");
                if (mission.AnyEnemyType || type == mission.EnemyType) return;
            }

            problems.Add("no active spawn point for " + (mission.AnyEnemyType ? "any enemy" : mission.EnemyType.ToString()));
        }

        // checks that the player has a way to get this currency right now
        private static void CheckCollect(CurrencyType currencyType, List<string> problems, string prefix = "")
        {
            if (currencyType == CurrencyType.Coins)
                return;

            // raw resources
            bool hasSourceType = false;
            foreach (ResourceSourceBehavior source in Object.FindObjectsByType<ResourceSourceBehavior>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (source.Drop == null || !source.Drop.Exists(r => r.currency == currencyType))
                    continue;

                hasSourceType = true;

                if (source.gameObject.activeInHierarchy && UnlockableToolsController.IsToolUnlocked(source.InteractionAnimationType))
                    return;
            }

            // fish
            if (currencyType == CurrencyType.Fish)
            {
                hasSourceType = true;

                if (UnlockableToolsController.IsToolUnlocked(InteractionAnimationType.Fishing))
                {
                    foreach (FishingPlaceBehavior place in Object.FindObjectsByType<FishingPlaceBehavior>(FindObjectsSortMode.None))
                    {
                        if (place.gameObject.activeInHierarchy)
                            return;
                    }
                }
            }

            // crafted resources and food
            foreach (ResourceConverterBuildingBehavior converter in Object.FindObjectsByType<ResourceConverterBuildingBehavior>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Recipe recipe = GetField<Recipe>(converter, "recipe");
                if (recipe == null || recipe.ResultResourceType != currencyType)
                    continue;

                hasSourceType = true;

                IUnlockableComplex complex = converter.GetComponentInParent<IUnlockableComplex>();
                if (converter.gameObject.activeInHierarchy && (complex == null || IsOpen(complex)))
                {
                    // the inputs have to be obtainable too
                    List<string> inputProblems = new List<string>();
                    for (int i = 0; i < recipe.ComponentsAmount; i++)
                        CheckCollect(recipe.GetComponent(i).ResourceType, inputProblems);

                    if (inputProblems.Count == 0)
                        return;
                }
            }

            problems.Add(prefix + (hasSourceType ? $"no usable source of {currencyType} right now" : $"nothing in the world produces {currencyType}"));
        }

        private static void Unlock(IUnlockableComplex complex)
        {
            if (complex == null || IsOpen(complex))
                return;

            // pay through the purchase point so its save and events behave like a real purchase
            if (complex.CanBePurchased)
            {
                PurchasePoint purchasePoint = GetField<PurchasePoint>(complex, "purchaser");
                if (purchasePoint != null)
                {
                    object pointSave = purchasePoint.GetType().GetProperty("Save", PRIVATE_FIELD).GetValue(purchasePoint);
                    pointSave.GetType().GetProperty("IsBought").SetValue(pointSave, true);

                    complex.Purchase();

                    System.Delegate onPurchased = GetField<System.Delegate>(purchasePoint, "OnPurhcased");
                    onPurchased?.DynamicInvoke();
                }
                else
                {
                    complex.Purchase();
                }
            }

            // hammer the construction point like the player does
            ConstructionPointBehavior constructionPoint = GetField<ConstructionPointBehavior>(complex, "constructionPoint");
            for (int i = 0; i < 1000 && complex.CanBeConstructed && constructionPoint != null; i++)
            {
                constructionPoint.GetHit(constructionPoint.transform.position, false, null);
            }
        }

        private static bool IsOpen(IUnlockableComplex complex)
        {
            PropertyInfo property = complex.GetType().GetProperty("IsOpen");

            return property != null && (bool)property.GetValue(complex);
        }

        private static void UnlockHelper(HelperBehavior helper)
        {
            GroundTileComplexBehavior[] tiles = GetField<GroundTileComplexBehavior[]>(helper, "linkedTiles");
            BuildingComplexBehavior[] buildings = GetField<BuildingComplexBehavior[]>(helper, "linkedBuildings");

            if (tiles != null)
            {
                foreach (var tile in tiles)
                {
                    if (tile != null && !IsOpen(tile) && tile.gameObject.activeInHierarchy && (tile.CanBePurchased || tile.CanBeConstructed))
                    {
                        Unlock(tile);
                        return;
                    }
                }
            }

            if (buildings != null)
            {
                foreach (var building in buildings)
                {
                    if (building != null && !IsOpen(building) && building.gameObject.activeInHierarchy)
                    {
                        Unlock(building);
                        return;
                    }
                }
            }
        }

        private static IUpgrade GetUpgrade(UpgradeMission mission)
        {
            if (mission.UpgradeType == UpgradeType.Global)
                return GlobalUpgradesController.GetUpgrade<AbstactGlobalUpgrade>(mission.GlobalUpgradeType);

            return mission.LocalUpgrade;
        }

        private static int GetMissionIndex(Mission mission)
        {
            return mission.transform.GetSiblingIndex();
        }

        private static T GetField<T>(object target, string fieldName)
        {
            System.Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, PRIVATE_FIELD | BindingFlags.Public);
                if (field != null)
                    return (T)field.GetValue(target);

                type = type.BaseType;
            }

            return default;
        }
    }
}
