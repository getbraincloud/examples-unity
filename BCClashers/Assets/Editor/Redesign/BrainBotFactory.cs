using UnityEngine;
using UnityEditor;

namespace BCClashers.Redesign
{
    public enum BotClass { Grunt = 0, Soldier = 1, Shooter = 2 }

    /// <summary>
    /// Procedurally builds the brainBot-styled troop visuals (jointed primitive hierarchy) and
    /// the shared URP materials they use. The visual is attached as a child named "BotVisual"
    /// under a troop root so the root's TroopAI / NavMeshAgent / BoxCollider / Animator are left
    /// untouched. Front of the model faces +Z to match transform.forward used by TroopAI.
    ///
    /// Limbs hang from pivot nodes (ArmPivotL/R at the shoulders, LegPivotL/R at the hips) so
    /// animation clips can rotate the pivots for a natural swing. The right-hand weapon is
    /// parented to ArmPivotR so it swings with the attack. The pivot origin sits at foot level,
    /// so scaling BotVisual keeps the feet grounded.
    ///
    /// Palette sampled from the brainCloud 6.0 "brainBot" mascot.
    /// </summary>
    public static class BrainBotFactory
    {
        public const string MatDir = "Assets/ArtAssets/Generated/Materials";

        public static class Palette
        {
            public static readonly Color Blue      = new Color(0.118f, 0.608f, 1f);
            public static readonly Color Red        = new Color(0.886f, 0.231f, 0.180f);
            public static readonly Color BlueCape   = new Color(0.09f, 0.45f, 0.85f);
            public static readonly Color RedCape    = new Color(0.78f, 0.14f, 0.11f);
            public static readonly Color Gold       = new Color(1f, 0.761f, 0.055f);
            public static readonly Color Dark       = new Color(0.11f, 0.125f, 0.188f);
            public static readonly Color Steel      = new Color(0.78f, 0.83f, 0.89f);
            public static readonly Color GlowAlbedo = new Color(1f, 0.85f, 0.30f);
            public static readonly Color GlowEmis   = new Color(1f, 0.72f, 0.12f) * 2.0f;
        }

        static Material MakeMat(string name, Color c, float metallic, float smooth, Color? emission = null)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public struct Mats { public Material body, cape, gold, dark, steel, glow; }

        public static Mats EnsureMaterials(bool redTeam)
        {
            if (!AssetDatabase.IsValidFolder("Assets/ArtAssets/Generated")) AssetDatabase.CreateFolder("Assets/ArtAssets", "Generated");
            if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets/ArtAssets/Generated", "Materials");
            return new Mats
            {
                gold  = MakeMat("M_Bot_Gold",  Palette.Gold,  0.4f, 0.85f),
                dark  = MakeMat("M_Bot_Dark",  Palette.Dark,  0.2f, 0.55f),
                steel = MakeMat("M_Bot_Steel", Palette.Steel, 0.7f, 0.88f),
                glow  = MakeMat("M_Bot_Glow",  Palette.GlowAlbedo, 0f, 0.9f, Palette.GlowEmis),
                body  = redTeam ? MakeMat("M_Bot_Red",  Palette.Red)  : MakeMat("M_Bot_Blue", Palette.Blue),
                cape  = redTeam ? MakeMat("M_Bot_CapeRed", Palette.RedCape, 0.05f, 0.5f) : MakeMat("M_Bot_CapeBlue", Palette.BlueCape, 0.05f, 0.5f),
            };
        }
        static Material MakeMat(string n, Color c) => MakeMat(n, c, 0.25f, 0.75f);

        static Mesh _cube, _sphere, _cyl, _cap;
        static Mesh Prim(PrimitiveType t)
        {
            switch (t)
            {
                case PrimitiveType.Sphere:   return _sphere ??= Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                case PrimitiveType.Cylinder: return _cyl    ??= Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
                case PrimitiveType.Capsule:  return _cap    ??= Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                default:                     return _cube   ??= Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            }
        }

        static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos;
            return g.transform;
        }

        static void Part(Transform parent, PrimitiveType t, Vector3 pos, Vector3 scale, Vector3 euler, Material m, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = Prim(t);
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
        }

        /// <summary>Builds (or rebuilds) the "BotVisual" child under root. Local units ~2.7 tall, front +Z, feet at pivot origin.</summary>
        public static GameObject BuildVisual(Transform root, BotClass klass, bool redTeam)
        {
            var existing = root.Find("BotVisual");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var mats = EnsureMaterials(redTeam);
            var p = Pivot(root, "BotVisual", Vector3.zero);
            int k = (int)klass;

            // Distinct body types: Grunt = SHORT, Soldier = FAT, Shooter = TALL
            float torsoW = k == 0 ? 0.98f : k == 1 ? 1.55f : 0.60f;
            float torsoH = k == 0 ? 0.56f : k == 1 ? 0.95f : 1.40f;
            float legH   = k == 0 ? 0.16f : k == 1 ? 0.28f : 0.80f;
            float legW   = k == 0 ? 0.26f : k == 1 ? 0.34f : 0.17f;
            float armW   = k == 0 ? 0.22f : k == 1 ? 0.30f : 0.15f;
            float armLen = k == 2 ? 0.52f : 0.40f;
            float shoul  = k == 0 ? 0.34f : k == 1 ? 0.44f : 0.26f;
            float hand   = k == 0 ? 0.32f : k == 1 ? 0.32f : 0.20f;
            float torsoY = 0.8f + legH + torsoH / 2 * 0.9f;
            float headY  = torsoY + torsoH / 2 + 0.42f;
            bool hasCape = k == 2, hasCrown = k == 1;

            if (hasCape) Part(p, PrimitiveType.Cube, new Vector3(0, torsoY, -0.38f), new Vector3(torsoW + 0.3f, torsoH + 0.5f, 0.06f), new Vector3(9, 0, 0), mats.cape, "Cape");
            Part(p, PrimitiveType.Cube, new Vector3(0, torsoY, 0), new Vector3(torsoW, torsoH, 0.62f), Vector3.zero, mats.body, "Torso");
            Part(p, PrimitiveType.Cube, new Vector3(0, torsoY - torsoH / 2 + 0.02f, 0), new Vector3(torsoW + 0.05f, 0.16f, 0.66f), Vector3.zero, mats.gold, "Belt");
            Part(p, PrimitiveType.Cube, new Vector3(0, torsoY + 0.06f, 0.33f), new Vector3(0.32f, 0.4f, 0.06f), Vector3.zero, mats.glow, "Emblem");
            Part(p, PrimitiveType.Cube, new Vector3(0, headY, 0), new Vector3(0.76f, 0.6f, 0.64f), Vector3.zero, mats.body, "Head");
            Part(p, PrimitiveType.Cube, new Vector3(0, headY + 0.03f, 0.33f), new Vector3(0.62f, 0.32f, 0.06f), Vector3.zero, mats.dark, "Visor");
            Part(p, PrimitiveType.Sphere, new Vector3(-0.15f, headY + 0.04f, 0.4f), Vector3.one * 0.13f, Vector3.zero, mats.glow, "EyeL");
            Part(p, PrimitiveType.Sphere, new Vector3(0.15f, headY + 0.04f, 0.4f), Vector3.one * 0.13f, Vector3.zero, mats.glow, "EyeR");

            float ax = torsoW / 2 + 0.16f;
            float shy = torsoY + torsoH / 2 - 0.1f;           // shoulder height
            float armCenterY = (torsoY - 0.05f) - shy;         // relative to shoulder pivot
            float handY = (torsoY - 0.5f) - shy;

            // shoulder caps stay on body
            Part(p, PrimitiveType.Sphere, new Vector3(-ax, shy, 0), Vector3.one * shoul, Vector3.zero, mats.gold, "ShoulderCapL");
            Part(p, PrimitiveType.Sphere, new Vector3(ax, shy, 0), Vector3.one * shoul, Vector3.zero, mats.gold, "ShoulderCapR");

            var armL = Pivot(p, "ArmPivotL", new Vector3(-ax, shy, 0));
            Part(armL, PrimitiveType.Capsule, new Vector3(0, armCenterY, 0), new Vector3(armW, armLen, armW), Vector3.zero, mats.body, "ArmL");
            Part(armL, PrimitiveType.Sphere, new Vector3(0, handY, 0), Vector3.one * hand, Vector3.zero, mats.steel, "HandL");
            var armR = Pivot(p, "ArmPivotR", new Vector3(ax, shy, 0));
            Part(armR, PrimitiveType.Capsule, new Vector3(0, armCenterY, 0), new Vector3(armW, armLen, armW), Vector3.zero, mats.body, "ArmR");
            Part(armR, PrimitiveType.Sphere, new Vector3(0, handY, 0), Vector3.one * hand, Vector3.zero, mats.steel, "HandR");

            float hipY = 0.8f;
            float legCenterY = (0.4f + legH / 2 - 0.2f) - hipY;
            float footY = 0.08f - hipY;
            Part(p, PrimitiveType.Sphere, new Vector3(-0.28f, hipY, 0), Vector3.one * 0.28f, Vector3.zero, mats.gold, "HipCapL");
            Part(p, PrimitiveType.Sphere, new Vector3(0.28f, hipY, 0), Vector3.one * 0.28f, Vector3.zero, mats.gold, "HipCapR");
            var legL = Pivot(p, "LegPivotL", new Vector3(-0.28f, hipY, 0));
            Part(legL, PrimitiveType.Capsule, new Vector3(0, legCenterY, 0), new Vector3(legW, legH, legW), Vector3.zero, mats.body, "LegL");
            Part(legL, PrimitiveType.Cube, new Vector3(0, footY, 0.1f), new Vector3(0.32f, 0.16f, 0.5f), Vector3.zero, mats.body, "FootL");
            var legR = Pivot(p, "LegPivotR", new Vector3(0.28f, hipY, 0));
            Part(legR, PrimitiveType.Capsule, new Vector3(0, legCenterY, 0), new Vector3(legW, legH, legW), Vector3.zero, mats.body, "LegR");
            Part(legR, PrimitiveType.Cube, new Vector3(0, footY, 0.1f), new Vector3(0.32f, 0.16f, 0.5f), Vector3.zero, mats.body, "FootR");

            float hy = torsoY - 0.5f;
            if (hasCrown)
            {
                Part(p, PrimitiveType.Cylinder, new Vector3(0, headY + 0.4f, 0), new Vector3(0.5f, 0.12f, 0.5f), Vector3.zero, mats.gold, "CrownBand");
                Part(p, PrimitiveType.Cube, new Vector3(0, headY + 0.58f, 0), new Vector3(0.1f, 0.22f, 0.1f), Vector3.zero, mats.gold, "SpikeM");
                Part(p, PrimitiveType.Cube, new Vector3(-0.2f, headY + 0.54f, 0), new Vector3(0.1f, 0.18f, 0.1f), Vector3.zero, mats.gold, "SpikeL");
                Part(p, PrimitiveType.Cube, new Vector3(0.2f, headY + 0.54f, 0), new Vector3(0.1f, 0.18f, 0.1f), Vector3.zero, mats.gold, "SpikeR");
            }
            else
            {
                Part(p, PrimitiveType.Cylinder, new Vector3(0, headY + 0.42f, 0), new Vector3(0.05f, 0.16f, 0.05f), Vector3.zero, mats.gold, "AntStalk");
                Part(p, PrimitiveType.Sphere, new Vector3(0, headY + 0.62f, 0), Vector3.one * 0.13f, Vector3.zero, mats.glow, "AntBall");
            }

            // weapons: right-hand items parented to ArmPivotR (swing with attack); positions relative to that pivot
            if (klass == BotClass.Soldier)
            {
                Part(armR, PrimitiveType.Cube, new Vector3(0, (hy + 0.85f) - shy, 0.12f), new Vector3(0.1f, 1.1f, 0.03f), Vector3.zero, mats.steel, "Blade");
                Part(armR, PrimitiveType.Cube, new Vector3(0, (hy + 0.32f) - shy, 0.12f), new Vector3(0.34f, 0.1f, 0.13f), Vector3.zero, mats.gold, "Guard");
                Part(armL, PrimitiveType.Cylinder, new Vector3(0, (hy + 0.35f) - shy, 0.3f), new Vector3(0.6f, 0.05f, 0.6f), new Vector3(90, 0, 0), mats.steel, "Shield");
                Part(armL, PrimitiveType.Cylinder, new Vector3(0, (hy + 0.35f) - shy, 0.36f), new Vector3(0.26f, 0.04f, 0.26f), new Vector3(90, 0, 0), mats.gold, "ShieldBoss");
            }
            else if (klass == BotClass.Shooter)
            {
                Part(p, PrimitiveType.Cube, new Vector3(0.33f, headY + 0.03f, 0.3f), new Vector3(0.12f, 0.12f, 0.2f), Vector3.zero, mats.dark, "Scope");
                Part(p, PrimitiveType.Sphere, new Vector3(0.33f, headY + 0.03f, 0.44f), Vector3.one * 0.1f, Vector3.zero, mats.glow, "Lens");
                Part(armR, PrimitiveType.Cube, new Vector3(0, (hy + 0.3f) - shy, 0.34f), new Vector3(0.19f, 0.22f, 0.58f), new Vector3(-8, 0, 0), mats.dark, "GunBody");
                Part(armR, PrimitiveType.Cylinder, new Vector3(0, (hy + 0.35f) - shy, 0.66f), new Vector3(0.06f, 0.2f, 0.06f), new Vector3(90, 0, 0), mats.steel, "Barrel");
                Part(armR, PrimitiveType.Sphere, new Vector3(0, (hy + 0.36f) - shy, 0.88f), Vector3.one * 0.11f, Vector3.zero, mats.glow, "Muzzle");
            }
            // Grunt: fists only.

            // team-color hook: body + cape recolor by team at runtime (invader blue / defender red)
            var painter = p.gameObject.AddComponent<BotTeamPainter>();
            painter.bodyBlue = MakeMat("M_Bot_Blue", Palette.Blue);
            painter.bodyRed  = MakeMat("M_Bot_Red", Palette.Red);
            painter.capeBlue = MakeMat("M_Bot_CapeBlue", Palette.BlueCape, 0.05f, 0.5f);
            painter.capeRed  = MakeMat("M_Bot_CapeRed", Palette.RedCape, 0.05f, 0.5f);

            return p.gameObject;
        }

        /// <summary>Builds (or rebuilds) a "StructureVisual" child: a brainCloud data-core tower. tier 0=small,1=med,2=large. Front (glowing core) faces +Z.</summary>
        public static GameObject BuildStructure(Transform root, int tier)
        {
            var existing = root.Find("StructureVisual");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
            var m = EnsureMaterials(false);
            var p = Pivot(root, "StructureVisual", Vector3.zero);

            float w = tier == 0 ? 0.8f : tier == 1 ? 0.92f : 1.08f;
            float bodyH = tier == 0 ? 0.7f : tier == 1 ? 0.9f : 1.05f;
            float baseH = 0.26f;

            Part(p, PrimitiveType.Cube, new Vector3(0, baseH/2, 0), new Vector3(w + 0.16f, baseH, w + 0.16f), Vector3.zero, m.steel, "Base");
            float bodyY = baseH + bodyH/2;
            Part(p, PrimitiveType.Cube, new Vector3(0, bodyY, 0), new Vector3(w, bodyH, w*0.9f), Vector3.zero, m.body, "Body");
            Part(p, PrimitiveType.Cube, new Vector3(0, baseH + bodyH, 0), new Vector3(w + 0.05f, 0.08f, w*0.9f + 0.05f), Vector3.zero, m.gold, "Trim");
            // glowing core "screen" on the front
            Part(p, PrimitiveType.Cube, new Vector3(0, bodyY + 0.03f, w*0.45f + 0.02f), new Vector3(w*0.5f, bodyH*0.5f, 0.06f), Vector3.zero, m.glow, "Core");
            // side vents (dark)
            Part(p, PrimitiveType.Cube, new Vector3(w*0.46f, bodyY, 0), new Vector3(0.05f, bodyH*0.6f, w*0.5f), Vector3.zero, m.dark, "VentR");
            Part(p, PrimitiveType.Cube, new Vector3(-w*0.46f, bodyY, 0), new Vector3(0.05f, bodyH*0.6f, w*0.5f), Vector3.zero, m.dark, "VentL");

            float topY = baseH + bodyH;
            if (tier >= 1)
            {
                float w2 = w*0.6f, h2 = tier == 1 ? 0.4f : 0.58f;
                Part(p, PrimitiveType.Cube, new Vector3(0, topY + h2/2, 0), new Vector3(w2, h2, w2), Vector3.zero, m.body, "Tier2");
                Part(p, PrimitiveType.Cube, new Vector3(0, topY + h2*0.5f, w2*0.5f + 0.02f), new Vector3(w2*0.5f, h2*0.5f, 0.05f), Vector3.zero, m.glow, "Core2");
                topY += h2;
                Part(p, PrimitiveType.Cube, new Vector3(0, topY, 0), new Vector3(w2 + 0.04f, 0.06f, w2 + 0.04f), Vector3.zero, m.gold, "Trim2");
            }
            if (tier == 2)
            {
                float w3 = w*0.34f, h3 = 0.5f;
                Part(p, PrimitiveType.Cube, new Vector3(0, topY + h3/2, 0), new Vector3(w3, h3, w3), Vector3.zero, m.body, "Tier3");
                topY += h3;
            }
            // antenna beacon on top
            Part(p, PrimitiveType.Cylinder, new Vector3(0, topY + 0.14f, 0), new Vector3(0.04f, 0.14f, 0.04f), Vector3.zero, m.gold, "Beacon");
            Part(p, PrimitiveType.Sphere, new Vector3(0, topY + 0.32f, 0), Vector3.one * 0.1f, Vector3.zero, m.glow, "BeaconGlow");
            return p.gameObject;
        }
    }
}
