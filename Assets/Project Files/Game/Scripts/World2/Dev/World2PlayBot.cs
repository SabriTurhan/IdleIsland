#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using Watermelon.GlobalUpgrades;

namespace Watermelon
{
    // Development only: plays World 2 like a player would (walks on the navmesh, harvests, carries resources to
    // converters and purchase points, hammers construction sites, fights, dodges the boss) and logs every problem.
    public class World2PlayBot : MonoBehaviour
    {
        public static World2PlayBot Instance { get; private set; }

        public static string LogPath => Path.Combine(Application.dataPath, "../Temp/playbot.log");

        public bool IsFinished { get; private set; }
        public int IssueCount { get; private set; }

        private const float WALK_SPEED = 5.5f;
        private const float MISSION_TIMEOUT = 900f; // game seconds

        private PlayerBehavior player;
        private NavMeshAgent agent;
        private HealthBehavior health;
        private readonly StringBuilder log = new StringBuilder();
        private float missionStartTime;
        private int killCount;
        private readonly Dictionary<CurrencyType, int> soldTotals = new Dictionary<CurrencyType, int>();

        public static World2PlayBot Launch(float timeScale)
        {
            GameObject go = new GameObject("World 2 Play Bot");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<World2PlayBot>();
            Time.timeScale = timeScale;
            File.WriteAllText(LogPath, "");
            Application.logMessageReceived += Instance.OnLog;

            return Instance;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            Time.timeScale = 1f;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
                string first = stackTrace.Split('\n')[0];
                Issue("CONSOLE " + type + ": " + condition.Split('\n')[0] + " @ " + first);
            }
        }

        private void Write(string line)
        {
            string text = $"[{Time.time,7:F0}s] {line}";
            log.AppendLine(text);
            File.AppendAllText(LogPath, text + "\n");
        }

        private void Issue(string line)
        {
            IssueCount++;
            Write("ISSUE " + line);
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        // ------------------------------------------------------------------------------------------------

        private IEnumerator Run()
        {
            while (PlayerBehavior.GetBehavior() == null || World2Story.IsPlaying || MissionsController.ActiveMission == null)
                yield return null;

            player = PlayerBehavior.GetBehavior();
            agent = player.GetComponent<NavMeshAgent>();
            health = Reflect<HealthBehavior>(player, "healthBehavior");
            BaseEnemyBehavior.OnEnemyKilled += enemy => killCount++;

            Write("start, coins=" + Amount(CurrencyType.Coins));
            Mission last = null;

            while (true)
            {
                Mission mission = MissionsController.ActiveMission;
                if (mission == null) { yield return new WaitForSeconds(1f); continue; }

                if (mission.MissionStage == Mission.Stage.Collected)
                {
                    if (mission.transform.GetSiblingIndex() == mission.transform.parent.childCount - 1)
                        break;
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                if (mission.MissionStage == Mission.Stage.Finished)
                {
                    // resource rewards complete by themselves, tool/generic rewards need the button
                    yield return new WaitForSeconds(1f);
                    if (mission.MissionStage == Mission.Stage.Finished && mission.RewardType != MissionRewardType.Resources)
                        MissionsController.CompleteMission();
                    yield return new WaitForSeconds(5.5f);
                    if (mission.MissionStage == Mission.Stage.Finished) MissionsController.CompleteMission();
                    continue;
                }

                if (mission.MissionStage != Mission.Stage.Active) { yield return new WaitForSeconds(0.5f); continue; }

                if (mission != last)
                {
                    last = mission;
                    missionStartTime = Time.time;
                    Write($"MISSION #{mission.transform.GetSiblingIndex() + 1} \"{mission.Title}\" ({mission.GetType().Name})");
                }

                yield return Play(mission);

                if (mission.MissionStage == Mission.Stage.Active && Time.time - missionStartTime > MISSION_TIMEOUT)
                {
                    Issue($"STUCK on \"{mission.Title}\" for {MISSION_TIMEOUT}s, giving up");
                    break;
                }
            }

            Write("chain done, coins=" + Amount(CurrencyType.Coins) + ", sold: " + string.Join(", ", SoldSummary()));
            IsFinished = true;
        }

        private IEnumerable<string> SoldSummary()
        {
            foreach (var pair in soldTotals) yield return pair.Key + " " + pair.Value;
        }

        private IEnumerator Play(Mission mission)
        {
            switch (mission)
            {
                case CollectMission collect:
                    yield return Obtain(collect.CurrencyType, Amount(collect.CurrencyType) + 1, true);
                    break;
                case BuildingMission building:
                    yield return Unlock(building.LinkedBuilding);
                    break;
                case GroundUnlockMission ground:
                    yield return Unlock(ground.GroundTile);
                    break;
                case HelperMission helper:
                    yield return FreeHelper(helper.HelperBehavior);
                    break;
                case KillEnemiesMission kill:
                    yield return Fight(kill);
                    break;
                case UpgradeMission upgrade:
                    yield return BuyUpgrade(upgrade);
                    break;
                case SubworldEnterMission:
                    yield return EnterCrypt();
                    break;
                case WorldChangeMission:
                    yield return SetSail();
                    break;
                default:
                    yield return new WaitForSeconds(1f);
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------------
        #region Movement

        private IEnumerator WalkTo(Vector3 target, float stopDistance, string what)
        {
            if (!NavMesh.SamplePosition(target, out NavMeshHit end, 3f, NavMesh.AllAreas))
            {
                Issue("no navmesh near " + what + " " + target);
                yield break;
            }

            NavMeshPath path = new NavMeshPath();
            NavMesh.CalculatePath(player.transform.position, end.position, NavMesh.AllAreas, path);
            if (path.status != NavMeshPathStatus.PathComplete)
            {
                Issue($"no complete path to {what} ({path.status}) from {player.transform.position:F0} to {target:F0}");
                if (path.status == NavMeshPathStatus.PathInvalid) yield break;
            }

            int corner = 1;
            float lastProgressTime = Time.time;
            float bestDistance = float.MaxValue;

            while (corner < path.corners.Length)
            {
                if (player.IsDead) yield break;

                Vector3 toTarget = (target - player.transform.position).SetY(0);
                if (toTarget.magnitude <= stopDistance) break;

                Vector3 next = path.corners[corner];
                Vector3 direction = (next - player.transform.position).SetY(0);
                if (direction.magnitude < 0.25f) { corner++; continue; }

                Vector3 step = direction.normalized * Mathf.Min(WALK_SPEED * Time.deltaTime, direction.magnitude);
                if (agent.enabled) agent.Move(step);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.LookRotation(direction), 0.3f);

                float distance = toTarget.magnitude;
                if (distance < bestDistance - 0.1f) { bestDistance = distance; lastProgressTime = Time.time; }
                if (Time.time - lastProgressTime > 4f)
                {
                    Issue($"stuck walking to {what} at {player.transform.position:F1}");
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        #endregion

        // ------------------------------------------------------------------------------------------------
        #region Resources

        private static int Amount(CurrencyType type) => CurrencyController.GetCurrency(type).Amount;

        // gets "target" of a currency: harvests raw resources, crafts products, fishes
        private IEnumerator Obtain(CurrencyType type, int target, bool collectMission = false)
        {
            float start = Time.time;
            int attempts = 0;
            while (Amount(type) < target && Time.time - start < MISSION_TIMEOUT && attempts++ < 400)
            {
                if (collectMission && MissionsController.ActiveMission != null && MissionsController.ActiveMission.MissionStage != Mission.Stage.Active)
                    yield break;

                if (player.Inventory.IsFull()) SellUnneeded(type);

                ResourceConverterBuildingBehavior converter = FindConverter(type);
                if (converter != null)
                {
                    yield return Craft(converter, type, target);
                    continue;
                }

                if (type == CurrencyType.Fish)
                {
                    yield return Fish(target);
                    continue;
                }

                ResourceSourceBehavior source = FindSource(type);
                if (source == null)
                {
                    Issue("nothing reachable gives " + type);
                    yield return Wait(5f);
                    continue;
                }

                yield return WalkTo(source.transform.position, 1.1f, type + " source");
                float hitStart = Time.time;
                int before = Amount(type);
                while (source.IsActive && source.gameObject.activeInHierarchy && Time.time - hitStart < 12f && !player.IsDead)
                    yield return null;
                yield return Wait(1.2f); // drops fly to the player

                if (Amount(type) <= before && !player.Inventory.IsFull())
                    Issue($"harvested {source.name} but {type} did not increase");
            }
        }

        private ResourceSourceBehavior FindSource(CurrencyType type)
        {
            ResourceSourceBehavior best = null;
            float bestCost = float.MaxValue;
            foreach (ResourceSourceBehavior source in FindObjectsByType<ResourceSourceBehavior>(FindObjectsSortMode.None))
            {
                if (!source.gameObject.activeInHierarchy || !source.IsActive || source.Drop == null || !source.Drop.Exists(r => r.currency == type)) continue;
                if (!UnlockableToolsController.IsToolUnlocked(source.InteractionAnimationType)) continue;
                if (!IsReachable(source.transform.position)) continue;

                float cost = (source.transform.position - player.transform.position).sqrMagnitude;
                if (cost < bestCost) { bestCost = cost; best = source; }
            }

            return best;
        }

        private bool IsReachable(Vector3 position)
        {
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 2.5f, NavMesh.AllAreas)) return false;
            NavMeshPath path = new NavMeshPath();
            return NavMesh.CalculatePath(player.transform.position, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        private ResourceConverterBuildingBehavior FindConverter(CurrencyType product)
        {
            foreach (ResourceConverterBuildingBehavior converter in FindObjectsByType<ResourceConverterBuildingBehavior>(FindObjectsSortMode.None))
            {
                Recipe recipe = Reflect<Recipe>(converter, "recipe");
                BuildingComplexBehavior complex = converter.GetComponentInParent<BuildingComplexBehavior>();
                if (recipe != null && recipe.ResultResourceType == product && complex != null && complex.IsOpen && converter.gameObject.activeInHierarchy)
                    return converter;
            }

            return null;
        }

        private IEnumerator Craft(ResourceConverterBuildingBehavior converter, CurrencyType product, int target)
        {
            Recipe recipe = Reflect<Recipe>(converter, "recipe");
            int needed = Mathf.Max(1, target - Amount(product));
            int batch = Mathf.Min(needed, 6);

            for (int i = 0; i < recipe.ComponentsAmount; i++)
            {
                RecipeComponent component = recipe.GetComponent(i);
                yield return Obtain(component.ResourceType, component.Amount * batch);
            }

            // drop the inputs into the converter
            Component inStorage = Reflect<Component>(converter, "inStorage");
            Transform takingPoint = inStorage.transform.Find("Taking Point") ?? inStorage.transform;
            yield return WalkTo(takingPoint.position, 0.6f, converter.name + " input");
            yield return Wait(2.5f);

            // wait for the products and pick them up
            Component outStorage = converter.OutStorage;
            float start = Time.time;
            int before = Amount(product);
            yield return WalkTo(outStorage.transform.position, 0.6f, converter.name + " output");
            while (Amount(product) < Mathf.Min(target, before + batch) && Time.time - start < 120f)
            {
                yield return Wait(1f);
                if ((outStorage.transform.position - player.transform.position).SetY(0).magnitude > 1.2f)
                    yield return WalkTo(outStorage.transform.position, 0.6f, converter.name + " output");
            }

            if (Amount(product) <= before)
                Issue($"crafting {product} at {converter.name} gave nothing");
        }

        private IEnumerator Fish(int target)
        {
            if (!UnlockableToolsController.IsToolUnlocked(InteractionAnimationType.Fishing))
            {
                Issue("fishing rod not unlocked");
                yield return Wait(5f);
                yield break;
            }

            FishingPlaceBehavior best = null;
            float bestDistance = float.MaxValue;
            foreach (FishingPlaceBehavior place in FindObjectsByType<FishingPlaceBehavior>(FindObjectsSortMode.None))
            {
                if (!place.gameObject.activeInHierarchy || !place.IsActive) continue;
                float distance = (place.transform.position - player.transform.position).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = place; }
            }

            if (best == null)
            {
                Issue("no fishing place");
                yield return Wait(5f);
                yield break;
            }

            yield return WalkTo(best.transform.position, 1.6f, "fishing place");
            float start = Time.time;
            while (Amount(CurrencyType.Fish) < target && best.IsActive && Time.time - start < 20f) yield return null;
            yield return Wait(1f);
        }

        // what a player does with a full bag: sell the things the current goal doesn't need
        private void SellUnneeded(CurrencyType keep)
        {
            foreach (Currency currency in CurrencyController.Currencies)
            {
                if (!currency.Data.UseInventory || currency.CurrencyType == keep || currency.Amount <= 0) continue;
                if (IsNeededSoon(currency.CurrencyType)) continue;

                int amount = currency.Amount;
                CurrencyController.Add(CurrencyType.Coins, amount * currency.Data.MoneyConversionRate);
                CurrencyController.Set(currency.CurrencyType, 0);
                soldTotals[currency.CurrencyType] = (soldTotals.TryGetValue(currency.CurrencyType, out int sold) ? sold : 0) + amount;
            }
        }

        private static bool IsNeededSoon(CurrencyType type)
        {
            return type == CurrencyType.Wood || type == CurrencyType.Fiber || type == CurrencyType.IronOre || type == CurrencyType.IronBar || type == CurrencyType.Planks || type == CurrencyType.Rope || type == CurrencyType.Sailcloth;
        }

        #endregion

        // ------------------------------------------------------------------------------------------------
        #region Building

        private IEnumerator Unlock(IUnlockableComplex complex)
        {
            Component component = complex as Component;
            float start = Time.time;

            // hidden behind a gate tile? (the building mission then shows the tile)
            while (!component.gameObject.activeInHierarchy && Time.time - start < 3f) yield return null;

            if (complex.CanBePurchased)
            {
                foreach (Resource cost in complex.CostLeft)
                {
                    if (cost.amount <= 0) continue;
                    if (cost.currency == CurrencyType.Coins)
                    {
                        if (Amount(CurrencyType.Coins) < cost.amount) Issue($"not enough coins for {component.name} ({Amount(CurrencyType.Coins)}/{cost.amount})");
                        continue;
                    }
                    yield return Obtain(cost.currency, cost.amount);
                }

                Component purchaser = Reflect<Component>(complex, "purchaser");
                Vector3 point = purchaser != null ? purchaser.transform.position : component.transform.position;
                yield return WalkTo(point, 0.8f, component.name + " purchase point");

                float payStart = Time.time;
                while (complex.CanBePurchased && Time.time - payStart < 25f) yield return null;
                if (complex.CanBePurchased)
                {
                    string left = "";
                    foreach (Resource cost in complex.CostLeft) left += cost.currency + " " + cost.amount + " ";
                    Issue($"could not pay {component.name}, left: {left}");
                    yield break;
                }
            }

            if (complex.CanBeConstructed)
            {
                ConstructionPointBehavior construction = Reflect<ConstructionPointBehavior>(complex, "constructionPoint");
                yield return WalkTo(construction.transform.position, 0.5f, component.name + " construction");
                float buildStart = Time.time;
                while (complex.CanBeConstructed && Time.time - buildStart < 90f) yield return null;
                if (complex.CanBeConstructed) Issue($"construction of {component.name} did not finish (hits {complex.HitsMade}/{complex.ConstructionHitsRequired})");
            }

            yield return Wait(1f);
        }

        private IEnumerator FreeHelper(HelperBehavior helper)
        {
            GroundTileComplexBehavior[] tiles = Reflect<GroundTileComplexBehavior[]>(helper, "linkedTiles");
            if (tiles != null)
            {
                foreach (GroundTileComplexBehavior tile in tiles)
                {
                    if (tile != null && !tile.IsOpen)
                    {
                        yield return Unlock(tile);
                        yield break;
                    }
                }
            }

            yield return Wait(2f);
        }

        private IEnumerator BuyUpgrade(UpgradeMission mission)
        {
            IUpgrade upgrade = mission.UpgradeType == UpgradeType.Global
                ? (IUpgrade)GlobalUpgradesController.GetUpgrade<AbstactGlobalUpgrade>(mission.GlobalUpgradeType)
                : mission.LocalUpgrade;

            // walk to where it is sold (the forge for the sword)
            Vector3 shop = mission.GetPreviewPosition();
            yield return WalkTo(shop, 2.5f, "upgrade shop");

            Resource price = upgrade.GetUpgradeCost(upgrade.UpgradeIndex + 1);
            if (Amount(price.currency) < price.amount)
            {
                Issue($"cannot afford {upgrade.Title}: {Amount(price.currency)}/{price.amount} {price.currency}");
                yield return Wait(10f);
                yield break;
            }

            CurrencyController.Substract(price.currency, price.amount);
            upgrade.UpgradeStage();
            Write($"bought {upgrade.Title} level {upgrade.UpgradeLevel} for {price.amount} {price.currency}");
            yield return Wait(1f);
        }

        #endregion

        // ------------------------------------------------------------------------------------------------
        #region Combat

        private IEnumerator Fight(KillEnemiesMission mission)
        {
            int target = killCount + mission.Amount;
            bool boss = !mission.AnyEnemyType && mission.EnemyType == EnemyType.GhostCaptain;
            float start = Time.time;
            int deaths = 0;

            if (boss) yield return EnterCrypt();

            while (mission.MissionStage == Mission.Stage.Active && Time.time - start < MISSION_TIMEOUT)
            {
                if (player.IsDead)
                {
                    deaths++;
                    Write("player died (" + deaths + ")");
                    while (player.IsDead) yield return null;
                    yield return Wait(2f);
                    if (boss) yield return EnterCrypt();
                    continue;
                }

                // low health: back off and let health regenerate
                if (health.CurrentHealth < health.MaxHealth * 0.3f)
                {
                    Vector3 away = player.transform.position - NearestEnemyPosition(player.transform.position);
                    yield return WalkTo(player.transform.position + away.SetY(0).normalized * 10f, 1f, "retreat");
                    float healStart = Time.time;
                    while (health.CurrentHealth < health.MaxHealth * 0.9f && Time.time - healStart < 15f) yield return null;
                    continue;
                }

                if (boss && DodgeSlam()) { yield return null; continue; }

                BaseEnemyBehavior enemy = FindEnemy(mission);
                if (enemy == null)
                {
                    EnemySpawnPoint spawn = FindSpawn(mission);
                    if (spawn == null && !boss)
                    {
                        Issue("no spawn point for " + (mission.AnyEnemyType ? "any enemy" : mission.EnemyType.ToString()));
                        yield break;
                    }

                    Vector3 position = boss ? FindFirstObjectByType<BossArena>().transform.position : spawn.transform.position;
                    yield return WalkTo(position, 2.5f, "enemy spawn");
                    yield return Wait(1.5f);
                    continue;
                }

                yield return WalkTo(enemy.transform.position, enemy.AttackRange * 0.7f, enemy.name);
                yield return Wait(0.4f);
            }

            Write($"fight done: kills={killCount - (target - mission.Amount)} deaths={deaths} time={Time.time - start:F0}s");

            if (boss)
            {
                // back outside
                SubworldExit exit = FindFirstObjectByType<SubworldExit>();
                if (exit != null && WorldController.WorldBehavior.SubworldHandler.IsSubworldActive)
                {
                    yield return WalkTo(exit.transform.position, 0.3f, "crypt exit");
                    yield return Wait(3f);
                }
            }
        }

        private bool DodgeSlam()
        {
            GameObject ring = GameObject.Find("Slam Telegraph");
            if (ring == null || !ring.activeInHierarchy) return false;

            Vector3 center = ring.transform.position;
            Vector3 offset = (player.transform.position - center).SetY(0);
            if (offset.magnitude > 3.9f) return false;

            Vector3 direction = offset.magnitude < 0.1f ? Vector3.right : offset.normalized;
            if (agent.enabled) agent.Move(direction * WALK_SPEED * Time.deltaTime);
            return true;
        }

        private Vector3 NearestEnemyPosition(Vector3 from)
        {
            Vector3 best = from + Vector3.forward;
            float bestDistance = float.MaxValue;
            foreach (BaseEnemyBehavior enemy in FindObjectsByType<BaseEnemyBehavior>(FindObjectsSortMode.None))
            {
                if (enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;
                float distance = (enemy.transform.position - from).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = enemy.transform.position; }
            }

            return best;
        }

        private BaseEnemyBehavior FindEnemy(KillEnemiesMission mission)
        {
            BaseEnemyBehavior best = null;
            float bestDistance = 400f;
            foreach (BaseEnemyBehavior enemy in FindObjectsByType<BaseEnemyBehavior>(FindObjectsSortMode.None))
            {
                if (enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;
                if (!mission.AnyEnemyType && enemy.EnemyType != mission.EnemyType && mission.EnemyType != EnemyType.GhostCaptain) continue;
                float distance = (enemy.transform.position - player.transform.position).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = enemy; }
            }

            return best;
        }

        private EnemySpawnPoint FindSpawn(KillEnemiesMission mission)
        {
            EnemySpawnPoint best = null;
            float bestDistance = float.MaxValue;
            foreach (EnemySpawnPoint point in FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None))
            {
                if (!point.gameObject.activeInHierarchy) continue;
                EnemyType type = Reflect<EnemyType>(point, "enemyType");
                if (!mission.AnyEnemyType && type != mission.EnemyType) continue;
                if (!IsReachable(point.transform.position)) continue;
                float distance = (point.transform.position - player.transform.position).sqrMagnitude;
                if (distance < bestDistance) { bestDistance = distance; best = point; }
            }

            return best;
        }

        #endregion

        // ------------------------------------------------------------------------------------------------
        #region Story

        private IEnumerator EnterCrypt()
        {
            if (WorldController.WorldBehavior.SubworldHandler.IsSubworldActive) yield break;

            SubworldEntrance crypt = null;
            foreach (SubworldEntrance entrance in FindObjectsByType<SubworldEntrance>(FindObjectsSortMode.None))
                if (entrance.name.Contains("Crypt")) crypt = entrance;

            if (crypt == null) { Issue("crypt entrance not found"); yield break; }

            Transform exitPoint = Reflect<Transform>(crypt, "exitSpawnPoint");
            yield return WalkTo(exitPoint.position, 0.3f, "crypt door");
            yield return WalkTo(crypt.transform.position, 0.05f, "crypt door");
            float start = Time.time;
            while (!WorldController.WorldBehavior.SubworldHandler.IsSubworldActive && Time.time - start < 6f) yield return null;
            if (!WorldController.WorldBehavior.SubworldHandler.IsSubworldActive) Issue("walking into the crypt door did not enter the crypt");
            yield return Wait(2f);
        }

        private IEnumerator SetSail()
        {
            WorldChangeZoneBehavior zone = FindFirstObjectByType<WorldChangeZoneBehavior>();
            yield return WalkTo(zone.transform.position, 0.2f, "ship");
            yield return Wait(1.5f);

            UIGame game = UIController.GetPage<UIGame>();
            game.WorldTransitionPopUp.ContinueButton();
            float start = Time.time;
            while (!World2Story.IsPlaying && Time.time - start < 10f) yield return null;
            if (!World2Story.IsPlaying) { Issue("ending did not start"); yield break; }

            // press "keep exploring" when it appears
            while (World2Story.IsPlaying && Time.time - start < 60f)
            {
                GameObject button = GameObject.Find("World 2 Story UI/Button");
                if (button != null) button.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                yield return Wait(1f);
            }

            Write("ending finished, player at " + player.transform.position.ToString("F0") + ", controls=" + player.enabled);
        }

        #endregion

        private static T Reflect<T>(object target, string field)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (info != null) return (T)info.GetValue(target);
            }

            return default;
        }
    }
}
#endif
