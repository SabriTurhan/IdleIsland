using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Player Skins Database", menuName = "Data/Player Skins Database")]
    public class PlayerSkinsDatabase : GenericSkinDatabase<PlayerSkinData>
    {
        [BoxFoldout("CCT", label: "Skin Creation Tools", order: 100)]
        [SerializeField] GameObject templatePrefab;
        public GameObject TemplatePrefab => templatePrefab;

        [BoxFoldout("CCT")]
        [SerializeField] Object defaultAnimator;
        public Object DefaultAnimator => defaultAnimator;

    }
}
