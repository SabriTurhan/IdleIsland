using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandEscape
{
    public interface IGroundOpenable
    {
        void OnGroundOpen(bool immediately = false);
        void OnGroundHidden(bool immediately = false);
    }
}