using UnityEngine;

namespace Watermelon
{
    public class WorldChangeZoneBehavior : MonoBehaviour, IGroundOpenable
    {
        // -1: the final voyage, plays the ending instead of loading another world
        [WorldPicker]
        [SerializeField] int worldIndex;
        public int WorldIndex => worldIndex;

        [SerializeField] WorldChangeSpecialBehavior changeSpecialBehavior;

        private Vector3 defaultScale;

        private WorldData worldData;
        public WorldData WorldData => worldData;

        public event SimpleCallback OnWorldChangeZoneEntered;

        private void Awake()
        {
            defaultScale = transform.localScale;

            worldData = worldIndex >= 0 ? WorldController.GetWorldData(worldIndex) : null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PhysicsHelper.TAG_PLAYER))
            {
                UIGame gameUI = UIController.GetPage<UIGame>();

                gameUI.WorldTransitionPopUp.Show(() =>
                {
                    if(changeSpecialBehavior != null)
                    {
                        changeSpecialBehavior.OnWorldChanged(() =>
                        {
                            LoadNextWorld();
                        });
                    }
                    else
                    {
                        LoadNextWorld();
                    }

                    enabled = false;
                });
            }
        }

        private void LoadNextWorld()
        {
            if (worldIndex < 0)
            {
                OnWorldChangeZoneEntered?.Invoke();
                World2Story.PlayEnding(this);
                return;
            }

            if(worldData != null)
            {
                OnWorldChangeZoneEntered?.Invoke();

                GameController.LoadWorld(worldData.ID);
            }
            else
            {
                Debug.LogError("Incorrect world index!", gameObject);
            }
        }

        public void OnGroundOpen(bool immediately)
        {
            ValidateWorldIndex();

            gameObject.SetActive(true);

            if (immediately)
            {
                transform.localScale = defaultScale;
            }
            else
            {
                transform.localScale = Vector3.zero;
                transform.DOScale(defaultScale, 0.3f).SetEasing(Ease.Type.SineOut);
            }

            if (changeSpecialBehavior != null)
            {
                changeSpecialBehavior.OnGroundTileOpened(immediately);
            }
        }

        public void OnGroundHidden(bool immediately)
        {
            ValidateWorldIndex();

            if (immediately)
            {
                gameObject.SetActive(false);
            }
            else
            {
                transform.DOScale(0, 0.3f).SetEasing(Ease.Type.SineOut).OnComplete(() => gameObject.SetActive(false));
            }
        }

        private void ValidateWorldIndex()
        {
            if (worldIndex >= 0 && !WorldController.IsWorldExists(worldIndex))
            {
                Debug.LogError("Incorrect world index!", gameObject);
            }
        }
    }
}