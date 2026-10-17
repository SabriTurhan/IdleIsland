using System;

namespace IslandEscape
{
    public class WorldObjectAttribute : Attribute
    {
        private int order;
        public int Order => order;

        public WorldObjectAttribute(int order)
        {
            this.order = order;
        }
    }
}