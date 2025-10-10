using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class PlayerSkinData : AbstractSkinData
    {
        [SkinPreview]
        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;
    }
}
