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

        #region Enemy accessories (local space of the skeleton's Head / RightHand bone, head is ~0.38 tall)

        public static Mesh Bandana()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Dome(0.205f, 3, 9, new Vector3(0, 0.2f, 0.0f), new Vector3(-8, 0, 0), new Vector3(1.05f, 0.75f, 1.1f), Band.Red, 0.0f, 0.6f);
            mesh.Box(new Vector3(0.07f, 0.07f, 0.07f), 0.01f, new Vector3(0, 0.24f, -0.22f), Vector3.zero, Band.Red, 0.2f, 0.6f);
            mesh.Box(new Vector3(0.05f, 0.16f, 0.025f), 0.005f, new Vector3(0.04f, 0.17f, -0.25f), new Vector3(20, 0, 15), Band.Red, 0.2f, 0.7f);
            mesh.Box(new Vector3(0.05f, 0.14f, 0.025f), 0.005f, new Vector3(-0.04f, 0.18f, -0.25f), new Vector3(20, 0, -15), Band.Red, 0.2f, 0.7f);

            return mesh.Build("pirate_bandana");
        }

        public static Mesh TricornHat()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Cylinder(0.42f, 0.38f, 0.06f, 3, new Vector3(0, 0.33f, 0), new Vector3(0, 30, 0), Band.Black, 0.1f, 0.7f);
            mesh.Cylinder(0.44f, 0.44f, 0.02f, 3, new Vector3(0, 0.305f, 0), new Vector3(0, 30, 0), Band.Gold, 0.0f, 0.5f);
            mesh.Dome(0.22f, 3, 9, new Vector3(0, 0.34f, 0), Vector3.zero, new Vector3(1f, 1.2f, 1f), Band.Black, 0.05f, 0.65f);
            // skull emblem
            mesh.Sphere(0.055f, 2, 6, new Vector3(0, 0.46f, 0.205f), new Vector3(1f, 1f, 0.5f), Band.Bone, 0.0f, 0.4f);
            mesh.Box(new Vector3(0.13f, 0.025f, 0.02f), 0f, new Vector3(0, 0.4f, 0.215f), new Vector3(0, 0, 35), Band.Bone, 0.0f, 0.4f);
            mesh.Box(new Vector3(0.13f, 0.025f, 0.02f), 0f, new Vector3(0, 0.4f, 0.215f), new Vector3(0, 0, -35), Band.Bone, 0.0f, 0.4f);

            return mesh.Build("pirate_tricorn_hat");
        }

        public static Mesh HornedHelmet()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Dome(0.23f, 3, 9, new Vector3(0, 0.2f, 0), Vector3.zero, new Vector3(1.05f, 1f, 1.1f), Band.Iron, 0.0f, 0.7f);
            mesh.Cylinder(0.24f, 0.24f, 0.05f, 9, new Vector3(0, 0.2f, 0), Vector3.zero, Band.Steel, 0.2f, 0.8f);
            mesh.Box(new Vector3(0.04f, 0.18f, 0.04f), 0.01f, new Vector3(0, 0.27f, 0.23f), Vector3.zero, Band.Steel, 0.1f, 0.7f);
            mesh.Cylinder(0.06f, 0.0f, 0.26f, 6, new Vector3(0.24f, 0.36f, 0), new Vector3(0, 0, -50), Band.Bone, 0.0f, 0.6f);
            mesh.Cylinder(0.06f, 0.0f, 0.26f, 6, new Vector3(-0.24f, 0.36f, 0), new Vector3(0, 0, 50), Band.Bone, 0.0f, 0.6f);

            return mesh.Build("pirate_horned_helmet");
        }

        public static Mesh GhostBeard()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Cylinder(0.16f, 0.0f, 0.34f, 7, new Vector3(0, -0.03f, 0.09f), new Vector3(180 - 15, 0, 0), Band.Ghost, 0.0f, 0.7f);
            mesh.Cylinder(0.07f, 0.0f, 0.2f, 6, new Vector3(0.07f, -0.06f, 0.12f), new Vector3(180 - 25, 0, 15), Band.Ghost, 0.0f, 0.7f);
            mesh.Cylinder(0.07f, 0.0f, 0.2f, 6, new Vector3(-0.07f, -0.06f, 0.12f), new Vector3(180 - 25, 0, -15), Band.Ghost, 0.0f, 0.7f);

            return mesh.Build("pirate_ghost_beard");
        }

        public static Mesh GlowingEyes()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Sphere(0.04f, 2, 6, new Vector3(0.065f, 0.17f, 0.15f), Vector3.one, Band.Ghost, 0.0f, 0.2f);
            mesh.Sphere(0.04f, 2, 6, new Vector3(-0.065f, 0.17f, 0.15f), Vector3.one, Band.Ghost, 0.0f, 0.2f);

            return mesh.Build("pirate_glowing_eyes");
        }

        // along the bone's Y axis (the hand bone points to the fingers)
        public static Mesh Cutlass()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Cylinder(0.025f, 0.03f, 0.14f, 6, new Vector3(0, 0.0f, 0), Vector3.zero, Band.WoodDark, 0.1f, 0.6f);
            mesh.Box(new Vector3(0.16f, 0.025f, 0.05f), 0.005f, new Vector3(0, 0.08f, 0), Vector3.zero, Band.Gold, 0.0f, 0.6f);
            mesh.Box(new Vector3(0.06f, 0.42f, 0.018f), 0.004f, new Vector3(0.01f, 0.3f, 0), new Vector3(0, 0, -4), Band.Steel, 0.0f, 0.6f);
            mesh.Box(new Vector3(0.05f, 0.12f, 0.018f), 0.004f, new Vector3(0.035f, 0.55f, 0), new Vector3(0, 0, -20), Band.Steel, 0.0f, 0.4f);

            return mesh.Build("pirate_cutlass");
        }

        public static Mesh Skull()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Sphere(0.22f, 3, 9, new Vector3(0, 0.08f, 0), new Vector3(1f, 0.95f, 1f), Band.Bone, 0.0f, 0.55f);
            mesh.Box(new Vector3(0.24f, 0.12f, 0.2f), 0.03f, new Vector3(0, -0.1f, 0.05f), Vector3.zero, Band.Bone, 0.1f, 0.6f);
            mesh.Sphere(0.06f, 2, 6, new Vector3(0.08f, 0.06f, 0.18f), new Vector3(1f, 1.1f, 0.6f), Band.Black, 0.3f, 0.9f);
            mesh.Sphere(0.06f, 2, 6, new Vector3(-0.08f, 0.06f, 0.18f), new Vector3(1f, 1.1f, 0.6f), Band.Black, 0.3f, 0.9f);
            mesh.Box(new Vector3(0.035f, 0.05f, 0.03f), 0.005f, new Vector3(0, -0.04f, 0.215f), Vector3.zero, Band.Black, 0.3f, 0.9f);

            return mesh.Build("pirate_skull");
        }

        #endregion

        #region Buildings

        // ~7.5 long along +Z (bow at +Z), deck at y ~1.4
        public static Mesh PirateShip()
        {
            PirateMesh mesh = new PirateMesh();
            // hull: side profile (z, y) extruded across the width, plus a narrower bow
            Vector2[] profile = { new Vector2(-3.3f, 1.7f), new Vector2(-3.0f, -0.1f), new Vector2(-2.2f, -0.65f), new Vector2(2.0f, -0.65f), new Vector2(3.0f, 0.1f), new Vector2(3.2f, 1.4f) };
            mesh.Add(Shapes.Prism(profile, 2.6f), Vector3.zero, new Vector3(0, -90, 0), Vector3.one, Band.WoodDark, 0.05f, 0.85f);
            Vector2[] bow = { new Vector2(2.9f, 1.4f), new Vector2(2.9f, -0.2f), new Vector2(3.9f, 0.6f), new Vector2(4.2f, 1.6f) };
            mesh.Add(Shapes.Prism(bow, 1.3f), Vector3.zero, new Vector3(0, -90, 0), Vector3.one, Band.WoodDark, 0.05f, 0.8f);
            // gold trim and deck
            mesh.Box(new Vector3(2.7f, 0.12f, 6.4f), 0.03f, new Vector3(0, 1.45f, -0.05f), Vector3.zero, Band.Gold, 0.0f, 0.6f);
            mesh.Box(new Vector3(2.4f, 0.1f, 6.1f), 0.02f, new Vector3(0, 1.5f, -0.05f), Vector3.zero, Band.Wood, 0.0f, 0.4f);
            // railings
            for (int side = -1; side <= 1; side += 2)
            {
                mesh.Box(new Vector3(0.12f, 0.12f, 6.0f), 0.02f, new Vector3(side * 1.22f, 2.05f, 0), Vector3.zero, Band.WoodDark, 0.0f, 0.6f);
                for (int i = 0; i < 7; i++)
                    mesh.Box(new Vector3(0.08f, 0.5f, 0.08f), 0f, new Vector3(side * 1.22f, 1.78f, -2.8f + i * 0.93f), Vector3.zero, Band.WoodDark, 0.2f, 0.7f);
                // cannons
                for (int i = 0; i < 3; i++)
                    mesh.Cylinder(0.13f, 0.11f, 0.5f, 7, new Vector3(side * 1.4f, 0.95f, -1.4f + i * 1.3f), new Vector3(0, 0, 90), Band.Coal, 0.1f, 0.7f);
            }
            // stern cabin with lit windows
            mesh.Box(new Vector3(2.3f, 1.1f, 1.6f), 0.06f, new Vector3(0, 2.1f, -2.4f), Vector3.zero, Band.WoodDark, 0.0f, 0.7f);
            mesh.Box(new Vector3(2.5f, 0.12f, 1.8f), 0.03f, new Vector3(0, 2.7f, -2.4f), Vector3.zero, Band.Gold, 0.0f, 0.6f);
            mesh.Box(new Vector3(0.35f, 0.35f, 0.05f), 0f, new Vector3(-0.5f, 2.15f, -1.58f), Vector3.zero, Band.Glow, 0.0f, 0.4f);
            mesh.Box(new Vector3(0.35f, 0.35f, 0.05f), 0f, new Vector3(0.5f, 2.15f, -1.58f), Vector3.zero, Band.Glow, 0.0f, 0.4f);
            // masts, yards and sails
            float[] mastZ = { 0.9f, -0.9f };
            float[] mastHeight = { 5.6f, 4.6f };
            for (int i = 0; i < 2; i++)
            {
                float h = mastHeight[i];
                mesh.Cylinder(0.12f, 0.08f, h, 7, new Vector3(0, 1.5f + h * 0.5f, mastZ[i]), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
                mesh.Cylinder(0.06f, 0.06f, 2.9f, 6, new Vector3(0, 1.5f + h * 0.9f, mastZ[i]), new Vector3(0, 0, 90), Band.WoodDark, 0.1f, 0.7f);
                // billowing sail: a thin box bent forward
                mesh.Box(new Vector3(2.7f, h * 0.55f, 0.08f), 0.03f, new Vector3(0, 1.5f + h * 0.58f, mastZ[i] + 0.25f), new Vector3(-6, 0, 0), Band.Sail, 0.0f, 0.55f);
                mesh.Box(new Vector3(2.72f, 0.22f, 0.1f), 0f, new Vector3(0, 1.5f + h * 0.62f, mastZ[i] + 0.27f), new Vector3(-6, 0, 0), Band.Red, 0.0f, 0.5f);
            }
            // bowsprit + flag
            mesh.Cylinder(0.07f, 0.04f, 2.2f, 6, new Vector3(0, 2.0f, 4.6f), new Vector3(65, 0, 0), Band.WoodDark, 0.1f, 0.7f);
            mesh.Box(new Vector3(0.05f, 0.6f, 0.9f), 0f, new Vector3(0, 7.0f, 0.45f), Vector3.zero, Band.Black, 0.0f, 0.6f);
            mesh.Sphere(0.12f, 2, 6, new Vector3(0.04f, 7.05f, 0.45f), new Vector3(0.6f, 1f, 1f), Band.Bone, 0.0f, 0.4f);

            return mesh.Build("pirate_ship");
        }

        // stone furnace with chimney, glowing mouth, anvil; footprint ~2.6 x 1.6
        public static Mesh Forge()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Box(new Vector3(1.5f, 1.1f, 1.3f), 0.12f, new Vector3(-0.35f, 0.55f, 0), Vector3.zero, Band.Stone, 0.05f, 0.75f);
            mesh.Dome(0.75f, 3, 8, new Vector3(-0.35f, 1.08f, 0), Vector3.zero, new Vector3(1f, 0.6f, 0.9f), Band.Stone, 0.0f, 0.6f);
            mesh.Cylinder(0.26f, 0.22f, 1.4f, 7, new Vector3(-0.55f, 2.0f, -0.25f), Vector3.zero, Band.Stone, 0.05f, 0.7f);
            mesh.Cylinder(0.3f, 0.3f, 0.12f, 7, new Vector3(-0.55f, 2.72f, -0.25f), Vector3.zero, Band.Iron, 0.1f, 0.6f);
            // furnace mouth
            mesh.Box(new Vector3(0.7f, 0.5f, 0.08f), 0.04f, new Vector3(-0.35f, 0.55f, 0.64f), Vector3.zero, Band.Coal, 0.4f, 0.9f);
            // anvil on a stump
            mesh.Cylinder(0.3f, 0.34f, 0.5f, 8, new Vector3(0.85f, 0.25f, 0.15f), Vector3.zero, Band.Wood, 0.1f, 0.7f);
            mesh.Box(new Vector3(0.55f, 0.18f, 0.28f), 0.03f, new Vector3(0.85f, 0.62f, 0.15f), Vector3.zero, Band.Iron, 0.0f, 0.6f);
            mesh.Box(new Vector3(0.24f, 0.18f, 0.2f), 0.02f, new Vector3(0.85f, 0.48f, 0.15f), Vector3.zero, Band.Iron, 0.2f, 0.8f);
            mesh.Cylinder(0.12f, 0.0f, 0.22f, 6, new Vector3(1.19f, 0.66f, 0.15f), new Vector3(0, 0, -90), Band.Iron, 0.0f, 0.6f);
            // water bucket
            mesh.Cylinder(0.2f, 0.24f, 0.32f, 8, new Vector3(0.8f, 0.16f, -0.45f), Vector3.zero, Band.Wood, 0.1f, 0.7f);
            mesh.Cylinder(0.19f, 0.19f, 0.02f, 8, new Vector3(0.8f, 0.3f, -0.45f), Vector3.zero, Band.Water, 0.0f, 0.4f);

            return mesh.Build("pirate_forge");
        }

        public static Mesh ForgeFire()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Box(new Vector3(0.5f, 0.32f, 0.06f), 0.03f, new Vector3(-0.35f, 0.5f, 0.68f), Vector3.zero, Band.Flame, 0.0f, 0.8f);
            mesh.Sphere(0.1f, 2, 6, new Vector3(-0.45f, 0.48f, 0.7f), Vector3.one, Band.Glow, 0.0f, 0.4f);
            mesh.Sphere(0.08f, 2, 6, new Vector3(-0.22f, 0.46f, 0.7f), Vector3.one, Band.Glow, 0.0f, 0.4f);

            return mesh.Build("pirate_forge_fire");
        }

        // weaving frame with a half woven sail; footprint ~2.2 x 1
        public static Mesh SailLoom()
        {
            PirateMesh mesh = new PirateMesh();
            for (int side = -1; side <= 1; side += 2)
            {
                mesh.Box(new Vector3(0.14f, 2.0f, 0.14f), 0.02f, new Vector3(side * 1.0f, 1.0f, -0.2f), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
                mesh.Box(new Vector3(0.14f, 1.0f, 0.14f), 0.02f, new Vector3(side * 1.0f, 0.5f, 0.45f), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
                mesh.Box(new Vector3(0.12f, 0.12f, 0.8f), 0.02f, new Vector3(side * 1.0f, 0.95f, 0.12f), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
            }
            mesh.Box(new Vector3(2.2f, 0.14f, 0.14f), 0.02f, new Vector3(0, 1.95f, -0.2f), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
            mesh.Box(new Vector3(2.2f, 0.14f, 0.14f), 0.02f, new Vector3(0, 0.25f, -0.2f), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
            mesh.Box(new Vector3(1.8f, 1.5f, 0.04f), 0f, new Vector3(0, 1.1f, -0.2f), Vector3.zero, Band.Sail, 0.0f, 0.5f);
            mesh.Box(new Vector3(1.82f, 0.16f, 0.06f), 0f, new Vector3(0, 1.45f, -0.2f), Vector3.zero, Band.Red, 0.0f, 0.4f);
            mesh.Box(new Vector3(1.82f, 0.16f, 0.06f), 0f, new Vector3(0, 0.85f, -0.2f), Vector3.zero, Band.Red, 0.0f, 0.4f);
            // fiber spool
            mesh.Cylinder(0.2f, 0.2f, 0.5f, 8, new Vector3(0, 0.95f, 0.45f), new Vector3(0, 0, 90), Band.Grass, 0.0f, 0.6f);

            return mesh.Build("pirate_sail_loom");
        }

        // barrel full of torches (sits next to the torch work table)
        public static Mesh TorchBarrel()
        {
            PirateMesh mesh = Barrel(new PirateMesh(), Vector3.zero, 0.85f);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + 20f;
                Vector3 tilt = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 12f, 0, Mathf.Cos(angle * Mathf.Deg2Rad) * 12f);
                Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.12f, 0, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.12f);
                mesh.Cylinder(0.04f, 0.05f, 0.7f, 6, new Vector3(0, 0.95f, 0) + offset, tilt, Band.WoodDark, 0.1f, 0.6f);
                mesh.Sphere(0.08f, 2, 6, new Vector3(0, 1.32f, 0) + offset * 2.2f, new Vector3(1, 1.3f, 1), Band.Flame, 0.0f, 0.7f);
            }

            return mesh.Build("pirate_torch_barrel");
        }

        #endregion

        #region Props

        public static PirateMesh Barrel(PirateMesh mesh, Vector3 position, float height)
        {
            mesh.Cylinder(0.3f, 0.36f, height * 0.5f, 9, position + new Vector3(0, height * 0.25f, 0), Vector3.zero, Band.Wood, 0.2f, 0.75f);
            mesh.Cylinder(0.36f, 0.3f, height * 0.5f, 9, position + new Vector3(0, height * 0.75f, 0), Vector3.zero, Band.Wood, 0.0f, 0.5f);
            mesh.Cylinder(0.37f, 0.37f, 0.06f, 9, position + new Vector3(0, height * 0.5f, 0), Vector3.zero, Band.Iron, 0.1f, 0.6f);
            mesh.Cylinder(0.33f, 0.33f, 0.05f, 9, position + new Vector3(0, height * 0.12f, 0), Vector3.zero, Band.Iron, 0.1f, 0.6f);
            mesh.Cylinder(0.33f, 0.33f, 0.05f, 9, position + new Vector3(0, height * 0.88f, 0), Vector3.zero, Band.Iron, 0.1f, 0.6f);

            return mesh;
        }

        public static Mesh BarrelProp() => Barrel(new PirateMesh(), Vector3.zero, 0.9f).Build("pirate_barrel");

        public static Mesh Crate()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Box(new Vector3(0.8f, 0.8f, 0.8f), 0.05f, new Vector3(0, 0.4f, 0), Vector3.zero, Band.Wood, 0.0f, 0.7f);
            for (int side = -1; side <= 1; side += 2)
            {
                mesh.Box(new Vector3(0.84f, 0.1f, 0.06f), 0f, new Vector3(0, 0.72f, side * 0.4f), Vector3.zero, Band.WoodDark, 0.2f, 0.6f);
                mesh.Box(new Vector3(0.84f, 0.1f, 0.06f), 0f, new Vector3(0, 0.08f, side * 0.4f), Vector3.zero, Band.WoodDark, 0.2f, 0.6f);
                mesh.Box(new Vector3(1.0f, 0.09f, 0.05f), 0f, new Vector3(0, 0.4f, side * 0.41f), new Vector3(0, 0, 42), Band.WoodDark, 0.2f, 0.6f);
            }

            return mesh.Build("pirate_crate");
        }

        public static Mesh Cannon()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Box(new Vector3(0.7f, 0.3f, 1.0f), 0.04f, new Vector3(0, 0.28f, -0.1f), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
            for (int side = -1; side <= 1; side += 2)
            {
                mesh.Cylinder(0.18f, 0.18f, 0.08f, 8, new Vector3(side * 0.38f, 0.18f, 0.25f), new Vector3(0, 0, 90), Band.Wood, 0.1f, 0.6f);
                mesh.Cylinder(0.18f, 0.18f, 0.08f, 8, new Vector3(side * 0.38f, 0.18f, -0.45f), new Vector3(0, 0, 90), Band.Wood, 0.1f, 0.6f);
            }
            mesh.Cylinder(0.24f, 0.17f, 1.3f, 9, new Vector3(0, 0.6f, 0.2f), new Vector3(80, 0, 0), Band.Coal, 0.05f, 0.7f);
            mesh.Cylinder(0.2f, 0.2f, 0.1f, 9, new Vector3(0, 0.71f, 0.82f), new Vector3(80, 0, 0), Band.Iron, 0.1f, 0.6f);
            mesh.Sphere(0.2f, 2, 7, new Vector3(0, 0.55f, -0.45f), Vector3.one, Band.Coal, 0.05f, 0.7f);

            return mesh.Build("pirate_cannon");
        }

        public static Mesh SkullFlag()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Cylinder(0.07f, 0.05f, 3.6f, 6, new Vector3(0, 1.8f, 0), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
            mesh.Box(new Vector3(0.05f, 0.9f, 1.4f), 0.01f, new Vector3(0, 3.1f, 0.72f), new Vector3(0, 0, 4), Band.Black, 0.0f, 0.6f);
            mesh.Sphere(0.18f, 2, 7, new Vector3(0.05f, 3.18f, 0.7f), new Vector3(0.4f, 1f, 1f), Band.Bone, 0.0f, 0.4f);
            mesh.Box(new Vector3(0.03f, 0.08f, 0.55f), 0f, new Vector3(0.05f, 2.92f, 0.7f), new Vector3(35, 0, 0), Band.Bone, 0.0f, 0.4f);
            mesh.Box(new Vector3(0.03f, 0.08f, 0.55f), 0f, new Vector3(0.05f, 2.92f, 0.7f), new Vector3(-35, 0, 0), Band.Bone, 0.0f, 0.4f);

            return mesh.Build("pirate_skull_flag");
        }

        public static Mesh LanternPost()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Cylinder(0.08f, 0.1f, 2.0f, 6, new Vector3(0, 1.0f, 0), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
            mesh.Box(new Vector3(0.5f, 0.08f, 0.08f), 0f, new Vector3(0.22f, 1.95f, 0), Vector3.zero, Band.WoodDark, 0.1f, 0.7f);
            mesh.Box(new Vector3(0.24f, 0.32f, 0.24f), 0.03f, new Vector3(0.42f, 1.68f, 0), Vector3.zero, Band.Glow, 0.0f, 0.5f);
            mesh.Cylinder(0.18f, 0.0f, 0.16f, 4, new Vector3(0.42f, 1.92f, 0), new Vector3(0, 45, 0), Band.Iron, 0.1f, 0.6f);
            mesh.Box(new Vector3(0.26f, 0.04f, 0.26f), 0f, new Vector3(0.42f, 1.5f, 0), Vector3.zero, Band.Iron, 0.1f, 0.6f);

            return mesh.Build("pirate_lantern_post");
        }

        public static Mesh TreasurePile()
        {
            PirateMesh mesh = new PirateMesh();
            mesh.Dome(0.75f, 3, 9, Vector3.zero, Vector3.zero, new Vector3(1f, 0.45f, 0.8f), Band.Gold, 0.0f, 0.7f);
            System.Random random = new System.Random(7);
            for (int i = 0; i < 14; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = (float)random.NextDouble() * 0.7f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0.05f + (0.7f - radius) * 0.4f, Mathf.Sin(angle) * radius * 0.8f);
                mesh.Cylinder(0.09f, 0.09f, 0.03f, 8, position, new Vector3((float)random.NextDouble() * 40f, 0, (float)random.NextDouble() * 40f), Band.Gold, 0.0f, 0.4f);
            }
            mesh.Sphere(0.1f, 2, 6, new Vector3(0.2f, 0.36f, 0.05f), Vector3.one, Band.Red, 0.0f, 0.4f);
            mesh.Sphere(0.08f, 2, 6, new Vector3(-0.25f, 0.3f, -0.1f), Vector3.one, Band.Ghost, 0.0f, 0.4f);

            return mesh.Build("pirate_treasure_pile");
        }

        // purple cursed thorns used as the closed visuals of torch tiles
        public static Mesh CursedThorns(int seed)
        {
            PirateMesh mesh = new PirateMesh();
            System.Random random = new System.Random(seed);
            for (int i = 0; i < 11; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Sqrt((float)random.NextDouble()) * 2.6f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                float height = Mathf.Lerp(0.9f, 2.0f, (float)random.NextDouble());
                Vector3 tilt = new Vector3(((float)random.NextDouble() - 0.5f) * 40f, 0, ((float)random.NextDouble() - 0.5f) * 40f);
                mesh.Cylinder(0.22f, 0.0f, height, 5, position + Vector3.up * height * 0.5f, tilt, Band.Curse, 0.0f, 0.85f);
                mesh.Cylinder(0.08f, 0.0f, height * 0.45f, 4, position + new Vector3(0.15f, height * 0.4f, 0.05f), tilt + new Vector3(0, 0, -55), Band.Curse, 0.0f, 0.7f);
            }
            for (int i = 0; i < 5; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float radius = (float)random.NextDouble() * 2.2f;
                mesh.Rock(0.45f, seed * 13 + i, new Vector3(Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius), new Vector3(1.2f, 0.6f, 1.2f), Band.Curse, 0.3f, 0.95f);
            }

            return mesh.Build("pirate_cursed_thorns_" + seed);
        }

        public static Mesh DeadTree(int seed)
        {
            PirateMesh mesh = new PirateMesh();
            System.Random random = new System.Random(seed);
            mesh.Cylinder(0.28f, 0.16f, 2.4f, 6, new Vector3(0, 1.2f, 0), new Vector3(0, 0, 5), Band.DarkWood, 0.1f, 0.8f);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f + (float)random.NextDouble() * 40f;
                float height = 1.4f + i * 0.25f;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 branch = (direction + Vector3.up * 0.8f).normalized;
                mesh.Cylinder(0.09f, 0.04f, 1.0f, 5, new Vector3(0, height, 0) + branch * 0.5f, Quaternion.FromToRotation(Vector3.up, branch).eulerAngles, Band.DarkWood, 0.1f, 0.8f);
            }

            return mesh.Build("pirate_dead_tree_" + seed);
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
