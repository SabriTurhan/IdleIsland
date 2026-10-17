using UnityEngine;

namespace IslandEscape
{
    [CreateAssetMenu(fileName = "Unlockable Tools Database", menuName = "Data/Unlockable Tools")]
    public class UnlockableToolsDatabase : ScriptableObject
    {
        [SerializeField] UnlockableTool[] unlockableTools;
        public UnlockableTool[] UnlockableTools => unlockableTools;
    }
}
