using UnityEngine;

namespace IslandEscape
{
    [CreateAssetMenu(fileName = "Interaction Data", menuName = "Data/Interaction Data")]
    public class InteractionData : ScriptableObject
    {
        [SerializeField] InteractionAnimationData defaultAnimationData;
        public InteractionAnimationData DefaultAnimationData => defaultAnimationData;

        [SerializeField] InteractionAnimationData[] animationsData;
        public InteractionAnimationData[] AnimationsData => animationsData;
    }
}