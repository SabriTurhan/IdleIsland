using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandEscape
{
    public abstract class AnimationForUnlockable : MonoBehaviour
    {
        public abstract float TotalAnimationDuration { get; }

        public abstract void RunUnlockedAnimation();
    }
}
