using UnityEngine;

namespace IslandEscape
{
    public interface ICharacterGraphics<T>
    {
        public Transform Transform { get; }

        public void OnGraphicsUpdated(T characterGraphics);
        public void OnGraphicsUnloaded(T currentGraphics);
    }
}