using UnityEngine;

namespace IslandEscape
{
    [System.Serializable]
    public class PlayerSkinData : AbstractSkinData
    {
        [SkinPreview]
        [SerializeField] GameObject prefab;
        public GameObject Prefab => prefab;
    }
}
