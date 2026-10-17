using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    // Story beats of World 2 (Pirate Cove):
    // - arrival: the raft is caught by a storm at sea, a wave throws the player onto the beach (plays once)
    // - ending: the repaired pirate ship sails away, "the curse is broken" screen
    public class World2Story : MonoBehaviour
    {
        private const string INTRO_SAVE = "world2_intro_played";
        private const string ENDING_SAVE = "world2_ending_seen";

        [Header("Arrival")]
        [SerializeField] EnvironmentWeatherPreset stormWeather;
        [SerializeField] EnvironmentWeatherPreset arrivalWeather;
        [SerializeField] GameObject raftModel;
        [SerializeField] GameObject splashParticle;
        [SerializeField] float seaDistance = 32f;

        [Header("UI")]
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Sprite buttonSprite;

        [Header("Ending")]
        [SerializeField] Transform endingRespawnPoint;

        private static World2Story instance;

        private Canvas canvas;
        private Image black;
        private Image flash;
        private TextMeshProUGUI caption;
        private TextMeshProUGUI title;
        private TextMeshProUGUI subtitle;

        public static bool IsPlaying { get; private set; }

        private void Awake()
        {
            instance = this;
            WorldController.OnWorldLoaded += OnWorldLoaded;
        }

        private void OnDestroy()
        {
            WorldController.OnWorldLoaded -= OnWorldLoaded;
            if (instance == this) instance = null;
            if (canvas != null) Destroy(canvas.gameObject);
            IsPlaying = false;
        }

        private void OnWorldLoaded()
        {
            if (SaveController.GetSaveObject<SimpleIntSave>(INTRO_SAVE).Value > 0)
                return;

            StartCoroutine(ArrivalCinematic());
        }

        #region Arrival

        private IEnumerator ArrivalCinematic()
        {
            IsPlaying = true;
            CreateUI();
            black.color = Color.black;

            // wait for the world to finish loading (navmesh, missions)
            yield return null;
            yield return null;

            PlayerBehavior player = PlayerBehavior.GetBehavior();
            Transform playerParent = player.transform.parent;
            Transform spawn = WorldController.WorldBehavior.SpawnPoint;
            VirtualCamera virtualCamera = CameraController.ActiveVirtualCamera;
            Transform cameraTarget = virtualCamera != null ? virtualCamera.Target : null;

            Control.DisableMovementControl();
            UIController.HidePage<UIGame>();
            EnvironmentController.ApplyWeather(stormWeather);

            // the raft in the open sea, south of the beach, drifting towards it
            Vector3 seaPoint = spawn.position + new Vector3(-4f, 0f, -seaDistance);
            GameObject raft = new GameObject("Storm Raft");
            raft.transform.position = seaPoint;
            raft.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            if (raftModel != null)
            {
                GameObject model = Instantiate(raftModel, raft.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }

            Transform seat = new GameObject("Seat").transform;
            seat.SetParent(raft.transform, false);
            seat.localPosition = new Vector3(0, 0.35f, 0);
            seat.localRotation = Quaternion.identity;

            player.Disable();
            player.transform.SetParent(seat, false);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;
            player.PlayerGraphics.Animator.Play("Sitting", -1, 0);

            if (virtualCamera != null) virtualCamera.SetTarget(raft.transform);

            yield return Fade(black, 1f, 0f, 1.2f);
            yield return ShowCaption("A storm caught your raft at sea...", 2.2f, false);

            // tossed by the waves, lightning all around
            float duration = 6.5f;
            float[] lightning = { 0.6f, 2.1f, 3.4f, 4.6f };
            int nextLightning = 0;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;

                float bob = Mathf.Sin(time * 2.2f) * 0.35f + Mathf.Sin(time * 3.7f) * 0.15f;
                raft.transform.position = seaPoint + new Vector3(Mathf.Sin(time * 0.7f) * 1.5f, -0.65f + bob, time * 2.2f);
                raft.transform.rotation = Quaternion.Euler(Mathf.Sin(time * 1.9f) * 14f, Mathf.Sin(time * 0.8f) * 10f, Mathf.Sin(time * 2.4f) * 18f);

                if (nextLightning < lightning.Length && time >= lightning[nextLightning])
                {
                    nextLightning++;
                    StartCoroutine(Lightning());
                }

                yield return null;
            }

            // the big wave
            HideCaption();
            StartCoroutine(Lightning());
            if (virtualCamera != null) virtualCamera.Shake(0.05f, 0.6f, 1.2f, 2.2f);
            Vector3 waveStart = raft.transform.position;
            time = 0f;
            while (time < 1.1f)
            {
                time += Time.deltaTime;
                float t = time / 1.1f;
                raft.transform.position = waveStart + new Vector3(0, Mathf.Sin(t * Mathf.PI) * 2.6f, t * 4f);
                raft.transform.rotation = Quaternion.Euler(-25f * t, 0, Mathf.Lerp(10f, 120f, t * t));

                if (t > 0.55f && black.color.a < 0.01f)
                {
                    if (splashParticle != null) Destroy(Instantiate(splashParticle, raft.transform.position, Quaternion.identity), 4f);
                    StartCoroutine(Fade(black, 0f, 1f, 0.45f));
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            // washed ashore
            player.transform.SetParent(playerParent, true);
            Destroy(raft);

            EnvironmentController.ApplyWeather(arrivalWeather);
            if (virtualCamera != null) virtualCamera.SetTarget(cameraTarget != null ? cameraTarget : player.transform);

            player.transform.rotation = spawn.rotation;
            player.enabled = true;
            player.OnNavMeshUpdated();
            player.Warp(spawn);
            player.PlayerGraphics.Animator.Rebind();

            yield return ShowCaption("Hours later...", 1.6f, true);
            HideCaption();
            yield return Fade(black, 1f, 0f, 1.4f);

            yield return ShowTitle("PIRATE COVE", "The Curse of Captain Grimbeard", 3.2f);

            FloatingTextController.SpawnFloatingText("Floating".GetHashCode(), "Where... am I?", player.transform.position + new Vector3(0, 2.6f, 0), Quaternion.identity, 1.3f, Color.white);
            yield return new WaitForSeconds(1.2f);

            EnvironmentController.WeatherEnabled = true;
            UIController.ShowPage<UIGame>();
            Control.EnableMovementControl();

            SaveController.GetSaveObject<SimpleIntSave>(INTRO_SAVE).Value = 1;
            SaveController.MarkAsSaveIsRequired();

            IsPlaying = false;
        }

        private IEnumerator Lightning()
        {
            flash.color = new Color(1f, 1f, 1f, 0.85f);
            Light sun = RenderSettings.sun;
            float intensity = sun != null ? sun.intensity : 1f;
            if (sun != null) sun.intensity = intensity * 2.5f;

            yield return new WaitForSeconds(0.06f);
            flash.color = new Color(1f, 1f, 1f, 0.2f);
            yield return new WaitForSeconds(0.05f);
            flash.color = new Color(1f, 1f, 1f, 0.7f);

            if (CameraController.ActiveVirtualCamera != null)
                CameraController.ActiveVirtualCamera.Shake(0.02f, 0.3f, 0.3f, 0.8f);

            yield return Fade(flash, 0.7f, 0f, 0.35f, Color.white);
            if (sun != null) sun.intensity = intensity;
        }

        #endregion

        #region Ending

        public static void PlayEnding(WorldChangeZoneBehavior zone)
        {
            if (instance == null) return;
            instance.StartCoroutine(instance.EndingSequence(zone));
        }

        private IEnumerator EndingSequence(WorldChangeZoneBehavior zone)
        {
            IsPlaying = true;
            CreateUI();
            UIController.HidePage<UIGame>();

            // the ship sails away with the player on deck
            yield return new WaitForSeconds(3.2f);
            yield return Fade(black, 0f, 0.85f, 1.2f, new Color(0.1f, 0.06f, 0.18f));

            int coins = CurrencyController.GetCurrency(CurrencyType.Coins).Amount;
            title.text = "THE CURSE IS BROKEN!";
            subtitle.text = "You escaped Pirate Cove with Grimbeard's treasure.\n<size=70%>Coins: " + coins + "  <sprite name=Coins></size>\n\n<color=#FFE36E>THANKS FOR PLAYING!</color>";
            yield return PopIn(title.rectTransform);
            yield return PopIn(subtitle.rectTransform);

            bool pressed = false;
            Button button = CreateButton("KEEP EXPLORING", () => pressed = true);
            yield return PopIn((RectTransform)button.transform);
            while (!pressed) yield return null;

            Destroy(button.gameObject);
            title.text = "";
            subtitle.text = "";
            yield return Fade(black, 0.85f, 1f, 0.3f, new Color(0.1f, 0.06f, 0.18f));

            // back on the dock: the ship stays at anchor, the island can still be explored
            PlayerBehavior player = PlayerBehavior.GetBehavior();
            WorldChangeRaftBehavior ship = zone.GetComponentInParent<BuildingComplexBehavior>()?.GetComponentInChildren<WorldChangeRaftBehavior>(true);
            player.transform.SetParent(null, true);
            if (ship != null) ship.GetComponent<Animator>().Play("Idle", -1, 0);

            Transform respawn = endingRespawnPoint != null ? endingRespawnPoint : WorldController.WorldBehavior.SpawnPoint;
            player.enabled = true;
            player.OnNavMeshUpdated();
            player.Warp(respawn);
            player.PlayerGraphics.Animator.Rebind();
            zone.enabled = true;

            SaveController.GetSaveObject<SimpleIntSave>(ENDING_SAVE).Value = 1;
            SaveController.MarkAsSaveIsRequired();

            yield return Fade(black, 1f, 0f, 0.8f, Color.black);
            UIController.ShowPage<UIGame>();
            Control.EnableMovementControl();

            IsPlaying = false;
        }

        #endregion

        #region UI

        private void CreateUI()
        {
            if (canvas != null) return;

            canvas = new GameObject("World 2 Story UI").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            black = FullScreen("Black", new Color(0, 0, 0, 0));
            flash = FullScreen("Flash", new Color(1, 1, 1, 0));

            caption = Text("Caption", new Vector2(0.5f, 0.2f), 54, new Vector2(960, 300));
            title = Text("Title", new Vector2(0.5f, 0.62f), 110, new Vector2(1000, 260));
            title.color = new Color(1f, 0.89f, 0.43f);
            subtitle = Text("Subtitle", new Vector2(0.5f, 0.47f), 56, new Vector2(960, 420));
        }

        private Image FullScreen(string name, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.rectTransform.SetParent(canvas.transform, false);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private TextMeshProUGUI Text(string name, Vector2 anchor, float size, Vector2 box)
        {
            TextMeshProUGUI text = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(canvas.transform, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor;
            text.rectTransform.sizeDelta = box;
            if (font != null) text.font = font;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = "";

            return text;
        }

        private Button CreateButton(string label, UnityEngine.Events.UnityAction onClick)
        {
            Image image = new GameObject("Button", typeof(RectTransform)).AddComponent<Image>();
            image.rectTransform.SetParent(canvas.transform, false);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.25f);
            image.rectTransform.sizeDelta = new Vector2(560, 170);
            image.sprite = buttonSprite;
            image.type = buttonSprite != null ? Image.Type.Sliced : Image.Type.Simple;

            Button button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            TextMeshProUGUI text = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(image.rectTransform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(10, 18);
            text.rectTransform.offsetMax = new Vector2(-10, -6);
            if (font != null) text.font = font;
            text.fontSize = 58;
            text.alignment = TextAlignmentOptions.Center;
            text.text = label;

            return button;
        }

        private IEnumerator ShowCaption(string text, float hold, bool keepBlack)
        {
            caption.text = text;
            caption.alpha = 0f;
            float time = 0f;
            while (time < 0.4f)
            {
                time += Time.deltaTime;
                caption.alpha = time / 0.4f;
                yield return null;
            }
            caption.alpha = 1f;

            yield return new WaitForSeconds(hold);
        }

        private void HideCaption()
        {
            caption.text = "";
        }

        private IEnumerator ShowTitle(string titleText, string subtitleText, float hold)
        {
            title.text = titleText;
            subtitle.text = subtitleText;
            yield return PopIn(title.rectTransform);
            yield return new WaitForSeconds(hold);

            float time = 0f;
            while (time < 0.5f)
            {
                time += Time.deltaTime;
                title.alpha = subtitle.alpha = 1f - time / 0.5f;
                yield return null;
            }

            title.text = subtitle.text = "";
            title.alpha = subtitle.alpha = 1f;
        }

        private static IEnumerator PopIn(RectTransform target)
        {
            float time = 0f;
            while (time < 0.35f)
            {
                time += Time.deltaTime;
                float t = time / 0.35f;
                // back-out easing like the template popups
                float s = 1f + 2.2f * Mathf.Pow(t - 1f, 3) + 1.2f * Mathf.Pow(t - 1f, 2);
                target.localScale = Vector3.one * s;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private static IEnumerator Fade(Image image, float from, float to, float duration, Color? color = null)
        {
            Color baseColor = color ?? image.color;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                baseColor.a = Mathf.Lerp(from, to, time / duration);
                image.color = baseColor;
                yield return null;
            }
            baseColor.a = to;
            image.color = baseColor;
        }

        #endregion
    }
}
