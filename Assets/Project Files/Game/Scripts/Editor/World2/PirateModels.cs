using UnityEngine;

namespace Watermelon.World2
{
    // Low-poly models of World 2 (Pirate Cove). Sizes follow the template: pickables ~0.4-0.8 units.
    public static class PirateModels
    {
        #region Pickables

        public static Mesh IronOre()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Rock(0.22f, 11, new Vector3(0, 0.0f, 0), new Vector3(1.05f, 0.85f, 0.95f), Band.Iron, 0.05f, 0.7f);
            mesh.Rock(0.07f, 3, new Vector3(0.12f, 0.1f, 0.08f), Vector3.one, Band.Ore, 0.0f, 0.5f, 0.3f);
            mesh.Rock(0.06f, 5, new Vector3(-0.1f, 0.12f, -0.06f), Vector3.one, Band.Ore, 0.0f, 0.5f, 0.3f);
            mesh.Rock(0.05f, 8, new Vector3(0.02f, 0.16f, -0.12f), Vector3.one, Band.Ore, 0.0f, 0.5f, 0.3f);

            return mesh.Build("pirate_iron_ore");
        }

        public static Mesh IronBar()
        {
            PirateMesh mesh = new PirateMesh();
            // trapezoid ingot: wider at the bottom
            mesh.Cylinder(0.24f, 0.16f, 0.24f, 4, Vector3.zero, new Vector3(0, 45, 0), Band.Steel, 0.15f, 0.9f);

            Mesh built = mesh.Build("pirate_iron_bar");
            ScaleMesh(built, new Vector3(1.35f, 1f, 0.75f));

            return built;
        }

        public static Mesh Sailcloth()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Cylinder(0.15f, 0.15f, 0.62f, 9, Vector3.zero, new Vector3(0, 0, 90), Band.Sail, 0.0f, 0.55f);
            mesh.Cylinder(0.06f, 0.06f, 0.66f, 6, Vector3.zero, new Vector3(0, 0, 90), Band.Wood, 0.2f, 0.6f);
            mesh.Cylinder(0.158f, 0.158f, 0.05f, 9, new Vector3(-0.17f, 0, 0), new Vector3(0, 0, 90), Band.Red, 0.0f, 0.5f);
            mesh.Cylinder(0.158f, 0.158f, 0.05f, 9, new Vector3(0.17f, 0, 0), new Vector3(0, 0, 90), Band.Red, 0.0f, 0.5f);

            return mesh.Build("pirate_sailcloth");
        }

        public static Mesh Torch()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Cylinder(0.04f, 0.055f, 0.42f, 6, new Vector3(0, 0.04f, 0), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
            mesh.Cylinder(0.085f, 0.07f, 0.12f, 7, new Vector3(0, 0.23f, 0), Vector3.zero, Band.Rope, 0.1f, 0.6f);
            // round flame: a squashed sphere with a pointed top
            mesh.Sphere(0.15f, 3, 8, new Vector3(0, 0.4f, 0), new Vector3(1f, 1.05f, 1f), Band.Flame, 0.0f, 0.8f);
            mesh.Cylinder(0.11f, 0.0f, 0.24f, 8, new Vector3(0, 0.58f, 0), new Vector3(0, 20, 0), Band.Flame, 0.0f, 0.35f);
            mesh.Sphere(0.08f, 3, 7, new Vector3(0.03f, 0.41f, -0.09f), Vector3.one, Band.Glow, 0.0f, 0.5f);

            return mesh.Build("pirate_torch");
        }

        public static Mesh MapPiece()
        {
            PirateMesh mesh = new PirateMesh();
            // torn parchment corner
            Vector2[] outline =
            {
                new Vector2(-0.24f, -0.2f), new Vector2(0.22f, -0.24f), new Vector2(0.27f, 0.08f),
                new Vector2(0.12f, 0.24f), new Vector2(-0.18f, 0.2f), new Vector2(-0.28f, 0.02f),
            };
            mesh.Add(Shapes.Prism(outline, 0.025f), Vector3.zero, new Vector3(90, 0, 0), Vector3.one, Band.Parchment, 0.0f, 0.5f);
            // red "X" and a dotted path
            mesh.Box(new Vector3(0.16f, 0.02f, 0.035f), 0f, new Vector3(0.08f, 0.02f, 0.06f), new Vector3(0, 45, 0), Band.Red, 0.0f, 0.3f);
            mesh.Box(new Vector3(0.16f, 0.02f, 0.035f), 0f, new Vector3(0.08f, 0.02f, 0.06f), new Vector3(0, -45, 0), Band.Red, 0.0f, 0.3f);
            mesh.Box(new Vector3(0.04f, 0.02f, 0.025f), 0f, new Vector3(-0.04f, 0.02f, -0.02f), new Vector3(0, 30, 0), Band.WoodDark, 0.3f, 0.6f);
            mesh.Box(new Vector3(0.04f, 0.02f, 0.025f), 0f, new Vector3(-0.11f, 0.02f, -0.07f), new Vector3(0, 30, 0), Band.WoodDark, 0.3f, 0.6f);
            mesh.Box(new Vector3(0.04f, 0.02f, 0.025f), 0f, new Vector3(-0.18f, 0.02f, -0.1f), new Vector3(0, 10, 0), Band.WoodDark, 0.3f, 0.6f);

            Mesh built = mesh.Build("pirate_map_piece");
            ScaleMesh(built, Vector3.one * 1.3f);

            return built;
        }

        // orange ore nuggets that stick out of a stone layer
        public static Mesh OreNuggets(int seed, float radius, int count)
        {
            PirateMesh mesh = new PirateMesh();
            System.Random random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float angle = (i + (float)random.NextDouble() * 0.6f) / count * Mathf.PI * 2f;
                float height = (float)random.NextDouble() * radius * 0.5f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                float size = Mathf.Lerp(0.08f, 0.14f, (float)random.NextDouble());
                mesh.Rock(size, seed * 31 + i, position, Vector3.one, Band.Ore, 0.0f, 0.55f, 0.3f);
            }

            return mesh.Build("pirate_ore_nuggets_" + seed);
        }

        #endregion

        public static void ScaleMesh(Mesh mesh, Vector3 scale)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = Vector3.Scale(vertices[i], scale);
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }
    }
}
