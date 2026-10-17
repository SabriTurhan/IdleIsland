using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IslandEscape
{
    [CustomOverlayElement("Construction UI", "OnToggleValueChanged")]
    public class ConstructionPointBehavior : AbstractHitableBehavior, IDistanceToggle, IWorldElement
    {
        public int InitialisationOrder => 0;

        public override int HittableID => 10000000;
        public override bool IsMutlipleObjectsHitRestricted => true;

        [Space]
        [SerializeField] bool isHelperTaskActive = true;
        public bool IsHelperTaskActive => isHelperTaskActive;

        [Space]
        [SerializeField] Canvas canvas;
        [SerializeField] Image constructionIconImage;
        [SerializeField] TMP_Text progressText;
        [SerializeField] protected string textFormat = "{0} / {1}";
        [SerializeField] float canvasHideDistance = 8f;

        [Space]
        [SerializeField] AudioClip constructionAudioClip;

        private ConstructingPointSave save;

        public int HitsMade
        {
            get => save.Value;
            set => save.Value = value;
        }

        public event SimpleCallback OnConstructed;
        public event SimpleCallback OnGotHit;

        public bool DistanceToggleActivated { get; private set; }
        public bool IsDistanceToggleInCloseMode { get; private set; }

        public float ActivationDistanceOfDT => canvasHideDistance;

        public bool IsBuilt => save.IsBought || HitsMade >= UnlocableComplex.ConstructionHitsRequired;

        public Vector3 OriginPositionOfDT => canvas.transform.position;

        private IUnlockableComplex UnlocableComplex { get; set; }

        private TweenCase canvasAppearCase;
        private Vector3 canvasDefaultScale;

        private ConstructionTask constructionTask;

        public BaseWorldBehavior LinkedWorldBehavior { get; set; }

        private UnlockableTool unlockableTool;

        private BoxCollider boxCollider;
        public BoxCollider BoxCollider => boxCollider;

        // the construction visuals carve the navmesh almost up to the trigger edge,
        // so on most sides the player couldn't get close enough to start building
        private const float TRIGGER_MARGIN = 0.5f;

        // "stand here to build" hint, shown only for the first few constructions
        private const string BUILD_HINT_TEXT = "STAND HERE TO BUILD!";
        private const int BUILD_HINT_CONSTRUCTIONS_LIMIT = 3;
        private const float BUILD_HINT_DISTANCE = 2.0f;
        private const float BUILD_HINT_DELAY = 0.5f;
        private const float BUILD_HINT_COOLDOWN = 4.0f;
        private static readonly int FLOATING_TEXT_HASH = "Floating".GetHashCode();

        private static SimpleIntSave buildHintSave;
        private static float nextBuildHintTime;
        private float playerNearTime;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                Vector3 scale = boxCollider.transform.lossyScale;
                boxCollider.size += new Vector3(TRIGGER_MARGIN * 2 / Mathf.Max(scale.x, 0.01f), 0, TRIGGER_MARGIN * 2 / Mathf.Max(scale.z, 0.01f));
            }
        }

        private void Update()
        {
            if (!IsActive || !IsDistanceToggleInCloseMode || UnlocableComplex == null)
                return;

            PlayerBehavior player = PlayerBehavior.GetBehavior();
            if (player == null)
                return;

            if (buildHintSave == null)
                buildHintSave = SaveController.GetSaveObject<SimpleIntSave>("constructionBuildHint");

            if (buildHintSave.Value >= BUILD_HINT_CONSTRUCTIONS_LIMIT)
                return;

            Vector3 playerPosition = PlayerBehavior.Position;
            bool isPlayerNear = boxCollider == null || Vector3.Distance(boxCollider.ClosestPoint(playerPosition), playerPosition) <= BUILD_HINT_DISTANCE;

            if (!isPlayerNear || player.IsHittingObject(this))
            {
                playerNearTime = 0;
                return;
            }

            playerNearTime += Time.deltaTime;

            if (playerNearTime >= BUILD_HINT_DELAY && Time.time >= nextBuildHintTime)
            {
                FloatingTextController.SpawnFloatingText(FLOATING_TEXT_HASH, BUILD_HINT_TEXT, playerPosition + new Vector3(0, 2.5f, 0), Quaternion.identity, 1.0f, Color.white);

                nextBuildHintTime = Time.time + BUILD_HINT_COOLDOWN;
            }
        }

        public bool Init(IUnlockableComplex unlockableComplex)
        {
            UnlocableComplex = unlockableComplex;

            if (save == null)
            {
                save = SaveController.GetSaveObject<ConstructingPointSave>(unlockableComplex.ID + "_building_point");
            }

            if (save.IsBought || HitsMade >= unlockableComplex.ConstructionHitsRequired)
            {
                Destroy();
                return false;
            }

            InteractionAnimationData interactionData = PlayerBehavior.GetBehavior().PlayerGraphics.GetInteractionData(interactionAnimationType);
            if(interactionData.AnimationType == interactionAnimationType && interactionData.InteractionIcon != null)
            {
                constructionIconImage.sprite = interactionData.InteractionIcon;
            }
            progressText.text = string.Format(textFormat, HitsMade, unlockableComplex.ConstructionHitsRequired);
            canvasDefaultScale = canvas.transform.localScale;

            // Add object to distance toggle
            DistanceToggle.AddObject(this);

            constructionTask = new ConstructionTask(this);
            constructionTask.Activate();
            constructionTask.Register(LinkedWorldBehavior.TaskHandler);

            unlockableTool = UnlockableToolsController.GetUnlockableTool(interactionAnimationType);

            return true;
        }

        public bool LookUpConstructed(IUnlockableComplex unlockableComplex)
        {
            if (save == null)
            {
                save = SaveController.GetSaveObject<ConstructingPointSave>(unlockableComplex.ID + "_building_point");
            }

            return save.IsBought || HitsMade >= unlockableComplex.ConstructionHitsRequired;
        }

        public void OnWorldLoaded()
        {

        }

        public void OnWorldUnloaded()
        {

        }

        public override void GetHit(Vector3 hitSourcePosition, bool drop, IHitter resourcePicked = null)
        {
            if (IsBuilt)
                return;

            HitsMade += 1;

            // instantly completing if Actions -> Instant Construction is enabled in editor
            if (InstantConstructionActionMenu.IsConstructionInstant())
                HitsMade = UnlocableComplex.ConstructionHitsRequired;

            if (HitsMade >= UnlocableComplex.ConstructionHitsRequired)
            {
                if (constructionTask != null)
                    constructionTask.Disable();

                save.IsBought = true;

                if (buildHintSave == null)
                    buildHintSave = SaveController.GetSaveObject<SimpleIntSave>("constructionBuildHint");
                buildHintSave.Value += 1;

                PlayerBehavior.GetBehavior().OnHittableOutsideRangeOrCompleted(this);
                UnlocableComplex.Construct();
                OnConstructed?.Invoke();
            }
            else
            {
                progressText.text = string.Format(textFormat, HitsMade, UnlocableComplex.ConstructionHitsRequired);
            }

            if(constructionAudioClip != null)
                AudioController.PlaySound(constructionAudioClip, transform.position);

            EnergyController.OnConstructionHit();

            OnGotHit?.Invoke();
        }

        public override bool IsHittable()
        {
            if (unlockableTool != null)
                return unlockableTool.IsUnlocked;

            return true;
        }

        public void Enable()
        {
            IsActive = true;

            // the next 2 fields responcible for canvas hiding and showing depending on player's position
            DistanceToggleActivated = true;
            IsDistanceToggleInCloseMode = false;

            canvas.gameObject.SetActive(false);
        }

        public void Disable()
        {
            IsActive = false;

            // the next 2 fields responcible for canvas hiding and showing depending on player's position
            DistanceToggleActivated = false;
            IsDistanceToggleInCloseMode = false;

            canvas.gameObject.SetActive(false);
        }

        public void Destroy()
        {
            // remove object from distance toggle system
            DistanceToggle.RemoveObject(this);

            if (constructionTask != null)
            {
                constructionTask.Disable();
                constructionTask.Destroy();
            }

            Destroy(gameObject);
        }

        public void PlayerEnteredZone()
        {
            IsDistanceToggleInCloseMode = true;
            canvas.gameObject.SetActive(true);

            canvasAppearCase.KillActive();

            canvas.transform.localScale = canvasDefaultScale;
            canvasAppearCase = DistanceToggle.RunShowAnimation(canvas.transform);
        }

        public void PlayerLeavedZone()
        {
            IsDistanceToggleInCloseMode = false;

            canvasAppearCase.KillActive();
            canvasAppearCase = DistanceToggle.RunHideAnimation(canvas.transform, () =>
            {
                canvas.gameObject.SetActive(false);
            });
        }

        public Sprite GetConstructionIcon()
        {
            return constructionIconImage.sprite;
        }

        #region Development

        public void OnToggleValueChanged(bool enabled)
        {
            if (canvas == null)
                return;

            canvas.gameObject.SetActive(enabled);
        }

        public void UpdateCostInEditor(int hitsRequired)
        {
            progressText.text = string.Format(textFormat, 0, hitsRequired);
        }



        #endregion
    }
}