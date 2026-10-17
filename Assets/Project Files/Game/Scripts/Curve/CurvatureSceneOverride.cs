using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandEscape
{
    public class CurvatureSceneOverride : MonoBehaviour
    {
        [UnpackNested]
        [SerializeField] CurveData data;

        public void Apply()
        {
            if (CurvatureManager.Instance != null)
            {
                CurvatureManager.Instance.SetCurveOverride(data);
            }
        }

        public void Clear()
        {
            if (CurvatureManager.Instance != null)
            {
                CurvatureManager.Instance.RemoveCurveOverride();
            }
        }
    }
}
