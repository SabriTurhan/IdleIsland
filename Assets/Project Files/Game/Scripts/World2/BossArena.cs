using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    // Spawns the Ghost Captain when the player walks into the crypt, shows the boss bar, remembers the victory
    public class BossArena : MonoBehaviour
    {
        private const string SAVE_KEY = "world2_ghost_captain_defeated";

        [SerializeField] EnemyType bossType = EnemyType.GhostCaptain;
        [SerializeField] Transform bossSpawnPoint;
        [SerializeField] float triggerRadius = 9f;

        [Space]
        [SerializeField] TMP_FontAsset titleFont;
        [SerializeField] Sprite barSprite;

        public static bool IsBossDefeated => SaveController.GetSaveObject<SimpleIntSave>(SAVE_KEY).Value > 0;

        public static event SimpleCallback OnBossDefeated;

        private GhostCaptainBehavior boss;
        private BossHealthBar healthBar;

        private void Update()
        {
            if (PlayerBehavior.GetBehavior() == null || IsBossDefeated)
                return;

            if (boss == null)
            {
                if (Vector3.Distance(PlayerBehavior.Position, bossSpawnPoint.position) < triggerRadius)
                    SpawnBoss();

                return;
            }

            bool showBar = !boss.IsDead && boss.gameObject.activeInHierarchy && Vector3.Distance(PlayerBehavior.Position, boss.transform.position) < boss.LeashDistance;
            if (healthBar != null)
            {
                healthBar.SetVisible(showBar);
                healthBar.SetValue(boss.MaxHealth > 0 ? boss.CurrentHealth / boss.MaxHealth : 0f);
            }
        }

        private void SpawnBoss()
        {
            BaseEnemyBehavior enemy = GameController.Data.EnemiesDatabase.GetEnemyBehavior(bossType);
            boss = enemy as GhostCaptainBehavior;
            if (boss == null)
            {
                Debug.LogError("[BossArena] Boss prefab must have GhostCaptainBehavior");
                return;
            }

            boss.Spawn(bossSpawnPoint);
            boss.ResetBoss();
            boss.OnBossDefeated += HandleBossDefeated;

            if (healthBar == null)
                healthBar = BossHealthBar.Create(boss.BossTitle, titleFont, barSprite);

            healthBar.SetValue(1f);
        }

        private void HandleBossDefeated()
        {
            boss.OnBossDefeated -= HandleBossDefeated;

            SaveController.GetSaveObject<SimpleIntSave>(SAVE_KEY).Value = 1;
            SaveController.MarkAsSaveIsRequired();

            if (healthBar != null) healthBar.Hide();

            OnBossDefeated?.Invoke();
        }

        private void OnDisable()
        {
            if (healthBar != null) healthBar.Hide();
        }

        private void OnDestroy()
        {
            if (healthBar != null) Destroy(healthBar.gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (bossSpawnPoint == null) return;
            Gizmos.color = new Color(0.6f, 0.3f, 1f);
            Gizmos.DrawWireSphere(bossSpawnPoint.position, triggerRadius);
        }
    }

    // Screen-space boss health bar in the template's UI style (Fredoka title with outline, rounded bar)
    public class BossHealthBar : MonoBehaviour
    {
        private Image fill;
        private CanvasGroup group;
        private float shownValue = 1f;
        private float targetValue = 1f;

        public static BossHealthBar Create(string title, TMP_FontAsset font, Sprite sprite)
        {
            GameObject root = new GameObject("Boss Health Bar");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            BossHealthBar bar = root.AddComponent<BossHealthBar>();
            bar.group = root.AddComponent<CanvasGroup>();
            bar.group.alpha = 0f;
            bar.group.blocksRaycasts = false;

            RectTransform panel = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(root.transform, false);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0, -330);
            panel.sizeDelta = new Vector2(760, 130);

            TextMeshProUGUI text = new GameObject("Title", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(panel, false);
            text.rectTransform.anchorMin = new Vector2(0, 0.5f);
            text.rectTransform.anchorMax = new Vector2(1, 1);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            if (font != null) text.font = font;
            text.text = title;
            text.fontSize = 44;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.78f, 1f, 0.96f);

            Image background = CreateImage("Background", panel, sprite, new Color32(0x32, 0x32, 0x32, 0xff));
            background.rectTransform.anchorMin = new Vector2(0, 0);
            background.rectTransform.anchorMax = new Vector2(1, 0.5f);
            background.rectTransform.offsetMin = new Vector2(0, 6);
            background.rectTransform.offsetMax = new Vector2(0, -6);

            Image fillBack = CreateImage("Fill Back", background.rectTransform, sprite, new Color32(0x5b, 0x3f, 0xa8, 0xff));
            fillBack.rectTransform.anchorMin = Vector2.zero;
            fillBack.rectTransform.anchorMax = Vector2.one;
            fillBack.rectTransform.offsetMin = new Vector2(8, 8);
            fillBack.rectTransform.offsetMax = new Vector2(-8, -8);

            bar.fill = CreateImage("Fill", fillBack.rectTransform, null, new Color32(0x4d, 0xe8, 0xd6, 0xff));
            bar.fill.rectTransform.anchorMin = Vector2.zero;
            bar.fill.rectTransform.anchorMax = Vector2.one;
            bar.fill.rectTransform.offsetMin = bar.fill.rectTransform.offsetMax = Vector2.zero;
            bar.fill.rectTransform.pivot = new Vector2(0, 0.5f);

            return bar;
        }

        private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;

            return image;
        }

        public void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        }

        public void Hide()
        {
            if (group != null) group.alpha = 0f;
        }

        public void SetValue(float value)
        {
            targetValue = Mathf.Clamp01(value);
        }

        private void Update()
        {
            shownValue = Mathf.MoveTowards(shownValue, targetValue, Time.deltaTime * 0.8f);
            if (fill != null)
                fill.rectTransform.anchorMax = new Vector2(shownValue, 1f);
        }
    }
}
