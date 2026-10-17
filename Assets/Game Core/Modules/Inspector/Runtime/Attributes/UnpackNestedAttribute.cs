using System;

namespace IslandEscape
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class UnpackNestedAttribute : Attribute
    {
        public UnpackNestedAttribute() { }
    }
}
