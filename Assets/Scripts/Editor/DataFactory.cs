using UnityEditor;
using UnityEngine;
using Bastion.Core;
using Bastion.Towers;
using Bastion.Enemies;
using Bastion.Waves;
using Bastion.Grid;

namespace Bastion.EditorTools
{
    /// <summary>Crée les ScriptableObjects de départ (5 tours, 5 ennemis, 10 vagues, 1 niveau). Modifiez-les ensuite dans l'Inspector.</summary>
    public static class DataFactory
    {
        public static TowerData[] Towers()
        {
            var arrow = PlaceholderFactory.ProjectilePrefab("Arrow", false, MaterialLibrary.Metal, null, PrimitiveType.Cube, new Vector3(0.08f, 0.08f, 0.6f));
            var ball = PlaceholderFactory.ProjectilePrefab("Cannonball", true, MaterialLibrary.Metal, null, PrimitiveType.Sphere, Vector3.one * 0.35f);
            var frost = PlaceholderFactory.ProjectilePrefab("FrostShard", false, MaterialLibrary.Ice, MeshLibrary.Crystal(), PrimitiveType.Sphere, new Vector3(0.25f, 0.5f, 0.25f));
            var fire = PlaceholderFactory.ProjectilePrefab("Fireball", true, MaterialLibrary.Lava, null, PrimitiveType.Sphere, Vector3.one * 0.4f);

            return new[]
            {
                Tower("crossbow", "Arbalète", "Dégâts directs, cadence élevée. Touche le sol et le vol.", "#F2A83B",
                      AttackKind.Direct, DamageType.Physical, TargetPriority.First, true, true, arrow, 22f, false,
                      L(60, 5.0f, 9f, 2.0f), L(70, 5.5f, 15f, 2.4f), L(110, 6.2f, 24f, 3.0f)),
                Tower("cannon", "Canon", "Boulet explosif en zone. Ne vise pas les volants.", "#A34CFF",
                      AttackKind.AreaOfEffect, DamageType.Physical, TargetPriority.Strongest, true, false, ball, 12f, false,
                      L(100, 5.5f, 28f, 0.6f, splash: 1.6f), L(120, 6.0f, 45f, 0.7f, splash: 1.9f), L(180, 6.5f, 75f, 0.8f, splash: 2.3f)),
                Tower("frost", "Cristal de givre", "Ralentit tout ce qu'il touche. Dégâts légers de givre.", "#7CC6FF",
                      AttackKind.Slow, DamageType.Frost, TargetPriority.First, true, true, frost, 16f, false,
                      L(80, 4.5f, 5f, 1.2f, splash: 1.2f, slow: 0.55f, dur: 2f), L(90, 5.0f, 9f, 1.4f, splash: 1.5f, slow: 0.45f, dur: 2.5f), L(140, 5.5f, 14f, 1.6f, splash: 1.8f, slow: 0.35f, dur: 3f)),
                Tower("furnace", "Fournaise", "Boule de feu qui enflamme la zone : dégâts sur la durée.", "#FF6A1F",
                      AttackKind.Burn, DamageType.Fire, TargetPriority.Strongest, true, false, fire, 11f, false,
                      L(110, 4.5f, 12f, 0.7f, splash: 1.5f, dur: 3f, dot: 8f), L(130, 5.0f, 18f, 0.8f, splash: 1.8f, dur: 3.5f, dot: 14f), L(200, 5.5f, 28f, 0.9f, splash: 2.2f, dur: 4f, dot: 24f)),
                Tower("arcane", "Tour arcane", "Tir instantané à très longue portée. Lent, mais dévastateur.", "#D46CFF",
                      AttackKind.Sniper, DamageType.Magic, TargetPriority.Strongest, true, true, null, 0f, true,
                      L(150, 9.0f, 70f, 0.35f), L(170, 10.5f, 130f, 0.4f), L(260, 12f, 240f, 0.45f)),
            };
        }

        private static TowerLevel L(int cost, float range, float dmg, float rate, float splash = 0, float slow = 0.5f, float dur = 2f, float dot = 0)
            => new() { cost = cost, range = range, damage = dmg, fireRate = rate, splashRadius = splash, slowFactor = slow, effectDuration = dur, dotDamagePerSecond = dot };

        private static TowerData Tower(string id, string name, string desc, string hex, AttackKind kind, DamageType dt, TargetPriority prio,
                                       bool ground, bool flying, Projectile proj, float speed, bool hitscan, params TowerLevel[] levels)
        {
            var d = BastionPaths.GetOrCreate($"{BastionPaths.DataTowers}/Tower_{id}.asset", () =>
            {
                var t = ScriptableObject.CreateInstance<TowerData>();
                t.towerId = id; t.displayName = name; t.description = desc; t.accentColor = MaterialLibrary.Hex(hex);
                t.attackKind = kind; t.damageType = dt; t.defaultPriority = prio; t.canTargetGround = ground; t.canTargetFlying = flying;
                t.projectilePrefab = proj; t.projectileSpeed = speed; t.hitscan = hitscan; t.levels = levels;
                return t;
            });
            // Les visuels sont (re)liés à chaque génération : c'est le point de remplacement documenté.
            for (int i = 0; i < d.levels.Length; i++)
                if (d.levels[i].visualPrefab == null) d.levels[i].visualPrefab = PlaceholderFactory.TowerVisual(id, i);
            EditorUtility.SetDirty(d);
            return d;
        }

        public static EnemyData[] Enemies()
        {
            return new[]
            {
                Enemy("goblin", "Gobelin", "#6CC24A", 38f, 3.4f, 0f, 6, 1, false, false, 0f, 1f, 1f, 1f, 1f, 1f, threat: 1),
                Enemy("ogre", "Ogre", "#7A5A3A", 210f, 1.4f, 4f, 18, 2, false, false, 0f, 1f, 0.8f, 1.2f, 1f, 1f, threat: 4),
                Enemy("bat", "Chauve-souris", "#A34CFF", 55f, 3.0f, 0f, 9, 1, true, false, 1.6f, 0.9f, 1f, 1f, 1f, 1f, threat: 2),
                Enemy("frostgolem", "Golem de givre", "#7CC6FF", 160f, 1.8f, 2f, 16, 2, false, false, 0f, 1f, 1f, 1.6f, 0.3f, 1f, slowImmune: true, threat: 4),
                Enemy("warlord", "Seigneur de guerre", "#FF3A3A", 1400f, 1.1f, 6f, 120, 10, false, true, 0f, 1.3f, 0.85f, 1f, 1f, 0.9f, threat: 40),
                Enemy("icewyrm", "Wyrm de glace", "#7CC6FF", 1100f, 1.6f, 2f, 140, 10, true, true, 2.2f, 1.2f, 1f, 1.5f, 0.2f, 1f, slowImmune: true, threat: 40),
            };
        }

        private static EnemyData Enemy(string id, string name, string hex, float hp, float speed, float armor, int gold, int lives, bool flying, bool boss, float hover,
                                       float scale, float phys, float fire, float frost, float magic, bool slowImmune = false, int threat = 2)
        {
            var d = BastionPaths.GetOrCreate($"{BastionPaths.DataEnemies}/Enemy_{id}.asset", () =>
            {
                var e = ScriptableObject.CreateInstance<EnemyData>();
                e.enemyId = id; e.displayName = name; e.accentColor = MaterialLibrary.Hex(hex); e.threat = threat;
                e.maxHealth = hp; e.speed = speed; e.armor = armor; e.goldReward = gold; e.livesCost = lives;
                e.isFlying = flying; e.isBoss = boss; e.hoverHeight = hover; e.visualScale = scale;
                e.physicalMultiplier = phys; e.fireMultiplier = fire; e.frostMultiplier = frost; e.magicMultiplier = magic; e.slowImmune = slowImmune;
                return e;
            });
            if (d.visualPrefab == null) { d.visualPrefab = PlaceholderFactory.EnemyVisual(id); EditorUtility.SetDirty(d); }
            return d;
        }

        public static WaveData[] Waves(EnemyData[] e)
        {
            EnemyData Find(string id) => System.Array.Find(e, x => x.enemyId == id);
            var gob = Find("goblin"); var ogre = Find("ogre"); var bat = Find("bat"); var golem = Find("frostgolem"); var boss = Find("warlord");
            return new[]
            {
                Wave(1, "Éclaireurs", false, G(gob, 6, 1.0f)),
                Wave(2, "La meute", false, G(gob, 10, 0.7f)),
                Wave(3, "Premiers ogres", false, G(gob, 6, 0.7f), G(ogre, 2, 2.5f, 2f)),
                Wave(4, "Ailes noires", false, G(bat, 8, 0.8f), G(gob, 8, 0.5f, 3f)),
                Wave(5, "Seigneur de guerre", true, G(gob, 6, 0.6f), G(boss, 1, 1f, 4f)),
                Wave(6, "Gel", false, G(golem, 3, 2f), G(gob, 12, 0.5f, 2f)),
                Wave(7, "Assaut mixte", false, G(ogre, 4, 1.8f), G(bat, 10, 0.6f, 1f), G(gob, 10, 0.4f, 2f)),
                Wave(8, "Nuée", false, G(bat, 18, 0.45f), G(golem, 2, 2f, 3f)),
                Wave(9, "Muraille", false, G(ogre, 6, 1.5f), G(golem, 4, 1.5f, 2f), G(gob, 14, 0.4f, 1f)),
                Wave(10, "Le retour du Seigneur", true, G(ogre, 3, 2f), G(bat, 8, 0.5f), G(boss, 2, 6f, 3f)),
            };
        }

        private static SpawnGroup G(EnemyData e, int n, float interval, float delay = 0f) => new() { enemy = e, count = n, interval = interval, delayBefore = delay };

        /// <summary>Niveau 2 : 14 vagues, le Wyrm volant force les tours anti-air.</summary>
        public static WaveData[] Waves2(EnemyData[] e)
        {
            EnemyData Find(string id) => System.Array.Find(e, x => x.enemyId == id);
            var gob = Find("goblin"); var ogre = Find("ogre"); var bat = Find("bat"); var golem = Find("frostgolem"); var boss = Find("warlord"); var wyrm = Find("icewyrm");
            return new[]
            {
                W2(1, "Patrouille", false, G(gob, 8, 0.8f)),
                W2(2, "Givre", false, G(golem, 3, 2f), G(gob, 8, 0.6f, 2f)),
                W2(3, "Ailes", false, G(bat, 12, 0.6f)),
                W2(4, "Colosses", false, G(ogre, 5, 1.6f), G(golem, 3, 1.5f, 3f)),
                W2(5, "Wyrm de glace", true, G(bat, 8, 0.5f), G(wyrm, 1, 1f, 4f)),
                W2(6, "Nuée", false, G(gob, 18, 0.35f), G(bat, 10, 0.5f, 2f)),
                W2(7, "Blizzard", false, G(golem, 6, 1.4f), G(bat, 8, 0.6f, 2f)),
                W2(8, "Muraille", false, G(ogre, 8, 1.3f), G(gob, 12, 0.4f, 2f)),
                W2(9, "Ciel noir", false, G(bat, 24, 0.4f)),
                W2(10, "Deux seigneurs", true, G(ogre, 4, 1.5f), G(boss, 2, 6f, 3f)),
                W2(11, "Avalanche", false, G(golem, 8, 1.2f), G(gob, 20, 0.35f, 2f), G(bat, 10, 0.5f, 4f)),
                W2(12, "Siège", false, G(ogre, 10, 1.1f), G(golem, 5, 1.3f, 3f)),
                W2(13, "Tempête", false, G(bat, 20, 0.4f), G(gob, 20, 0.3f, 2f), G(ogre, 5, 1.2f, 3f)),
                W2(14, "Le nid du Wyrm", true, G(bat, 12, 0.5f), G(wyrm, 2, 8f, 3f), G(boss, 1, 1f, 12f)),
            };
        }

        /// <summary>Niveau 3 : 18 vagues, tout le bestiaire, boss doublés.</summary>
        public static WaveData[] Waves3(EnemyData[] e)
        {
            EnemyData Find(string id) => System.Array.Find(e, x => x.enemyId == id);
            var gob = Find("goblin"); var ogre = Find("ogre"); var bat = Find("bat"); var golem = Find("frostgolem"); var boss = Find("warlord"); var wyrm = Find("icewyrm");
            var list = new System.Collections.Generic.List<WaveData>();
            for (int i = 1; i <= 18; i++)
            {
                bool bossWave = i % 6 == 0;
                var groups = new System.Collections.Generic.List<SpawnGroup> { G(gob, 6 + i * 2, Mathf.Max(0.3f, 0.8f - i * 0.025f)) };
                if (i % 2 == 0) groups.Add(G(ogre, 2 + i / 2, 1.4f, 2f));
                if (i % 3 == 0) groups.Add(G(bat, 6 + i, 0.5f, 3f));
                if (i % 4 == 0) groups.Add(G(golem, 2 + i / 3, 1.3f, 4f));
                if (bossWave) groups.Add(G(i % 12 == 0 ? wyrm : boss, i / 6, 6f, 5f));
                list.Add(WaveP("L3_", i, bossWave ? (i % 12 == 0 ? "Wyrms jumeaux" : "Seigneurs de la forge") : $"Assaut {i}", bossWave, groups.ToArray()));
            }
            return list.ToArray();
        }

        private static WaveData W2(int i, string title, bool boss, params SpawnGroup[] groups) => WaveP("L2_", i, title, boss, groups);

        private static WaveData Wave(int i, string title, bool boss, params SpawnGroup[] groups) => WaveP("", i, title, boss, groups);

        private static WaveData WaveP(string prefix, int i, string title, bool boss, params SpawnGroup[] groups)
            => BastionPaths.GetOrCreate($"{BastionPaths.DataWaves}/{prefix}Wave_{i:00}.asset", () =>
            {
                var w = ScriptableObject.CreateInstance<WaveData>();
                w.title = title; w.isBossWave = boss; w.groups = groups; return w;
            });

        public static LevelData Level(WaveData[] waves, TowerData[] towers, EnemyData[] enemies)
        {
            EnemyData E(string id) => System.Array.Find(enemies, x => x.enemyId == id);
            TowerData T(string id) => System.Array.Find(towers, x => x.towerId == id);
            var level = BastionPaths.GetOrCreate($"{BastionPaths.DataLevels}/Level_01.asset", () =>
            {
                var l = ScriptableObject.CreateInstance<LevelData>();
                l.levelId = "level_01"; l.displayName = "La Vallée"; l.width = 14; l.height = 10; l.cellSize = 2f;
                // Un chemin en S : entrée à gauche, sortie à droite, trois virages = trois « coins » à défendre.
                var path = new System.Collections.Generic.List<Vector2Int>();
                for (int x = 0; x <= 3; x++) path.Add(new Vector2Int(x, 7));
                for (int y = 6; y >= 2; y--) path.Add(new Vector2Int(3, y));
                for (int x = 4; x <= 7; x++) path.Add(new Vector2Int(x, 2));
                for (int y = 3; y <= 7; y++) path.Add(new Vector2Int(7, y));
                for (int x = 8; x <= 10; x++) path.Add(new Vector2Int(x, 7));
                for (int y = 6; y >= 3; y--) path.Add(new Vector2Int(10, y));
                for (int x = 11; x <= 13; x++) path.Add(new Vector2Int(x, 3));
                l.path = path.ToArray();
                l.blocked = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(13, 9), new Vector2Int(12, 9), new Vector2Int(5, 9), new Vector2Int(0, 9), new Vector2Int(13, 0) };
                l.startingGold = 180; l.startingLives = 20; l.waveClearBonus = 30; l.waves = waves; l.autoStartDelay = 25f; l.bossEvery = 5;
                l.description = "Un chemin en S entre trois collines.";
                // Apprentissage : trois tours seulement, les deux autres arrivent au niveau 2.
                l.availableTowers = new[] { T("crossbow"), T("cannon"), T("frost") };
                l.endlessPool = new[] { E("goblin"), E("ogre"), E("bat"), E("frostgolem") };
                l.endlessBossPool = new[] { E("warlord") };
                l.assaultPool = new[] { E("goblin"), E("bat"), E("ogre"), E("frostgolem"), E("warlord") };
                l.assaultDuration = 240f;
                return l;
            });
            return level;
        }

        /// <summary>Niveau 2 « Les Marais gelés » : biome givré, grille plus large, chemin en spirale ouverte.</summary>
        public static LevelData Level2(WaveData[] waves, EnemyData[] enemies)
        {
            EnemyData E(string id) => System.Array.Find(enemies, x => x.enemyId == id);
            return BastionPaths.GetOrCreate($"{BastionPaths.DataLevels}/Level_02.asset", () =>
            {
                var l = ScriptableObject.CreateInstance<LevelData>();
                l.levelId = "level_02"; l.displayName = "Les Marais gelés"; l.width = 16; l.height = 11; l.cellSize = 2f;
                l.description = "Une spirale de glace ; le Wyrm vient par les airs.";
                l.starsToUnlock = 2;
                var path = new System.Collections.Generic.List<Vector2Int>();
                for (int y = 0; y <= 5; y++) path.Add(new Vector2Int(0, y));
                for (int x = 1; x <= 12; x++) path.Add(new Vector2Int(x, 5));
                for (int y = 6; y <= 9; y++) path.Add(new Vector2Int(12, y));
                for (int x = 11; x >= 4; x--) path.Add(new Vector2Int(x, 9));
                for (int y = 8; y >= 2; y--) path.Add(new Vector2Int(4, y));
                for (int x = 5; x <= 8; x++) path.Add(new Vector2Int(x, 2));
                for (int y = 1; y >= 0; y--) path.Add(new Vector2Int(8, y));
                for (int x = 9; x <= 15; x++) path.Add(new Vector2Int(x, 0));
                l.path = path.ToArray();
                l.blocked = new[] { new Vector2Int(2, 2), new Vector2Int(14, 3), new Vector2Int(15, 10), new Vector2Int(1, 10), new Vector2Int(9, 7), new Vector2Int(6, 7), new Vector2Int(14, 8) };
                l.startingGold = 220; l.startingLives = 20; l.waveClearBonus = 35; l.waves = waves; l.autoStartDelay = 25f; l.bossEvery = 5;
                l.healthScalingPerWave = 0.14f;
                l.buildableColor = MaterialLibrary.Hex("#4A6B8A"); l.pathColor = MaterialLibrary.Hex("#E8F0F8"); l.blockedColor = MaterialLibrary.Hex("#2E3A4A");
                l.sunColor = MaterialLibrary.Hex("#DCEBFF"); l.ambientSky = MaterialLibrary.Hex("#6F8FC4"); l.ambientGround = MaterialLibrary.Hex("#101620"); l.fogColor = MaterialLibrary.Hex("#0E1520");
                l.endlessPool = new[] { E("goblin"), E("ogre"), E("bat"), E("frostgolem") };
                l.endlessBossPool = new[] { E("warlord"), E("icewyrm") };
                l.endlessBudgetBase = 40; l.endlessBudgetGrowth = 8;
                l.assaultPool = new[] { E("goblin"), E("bat"), E("ogre"), E("frostgolem"), E("warlord"), E("icewyrm") };
                l.assaultDuration = 300f; l.assaultAiStartingGold = 320;
                return l;
            });
        }

        /// <summary>Niveau 3 « Les Forges » : biome de lave, long serpentin, 18 vagues.</summary>
        public static LevelData Level3(WaveData[] waves, EnemyData[] enemies)
        {
            EnemyData E(string id) => System.Array.Find(enemies, x => x.enemyId == id);
            return BastionPaths.GetOrCreate($"{BastionPaths.DataLevels}/Level_03.asset", () =>
            {
                var l = ScriptableObject.CreateInstance<LevelData>();
                l.levelId = "level_03"; l.displayName = "Les Forges"; l.width = 18; l.height = 12; l.cellSize = 2f;
                l.description = "Un serpentin de basalte ; dix-huit vagues, deux boss.";
                l.starsToUnlock = 5;
                var path = new System.Collections.Generic.List<Vector2Int>();
                for (int x = 0; x <= 15; x++) path.Add(new Vector2Int(x, 10));
                for (int y = 9; y >= 7; y--) path.Add(new Vector2Int(15, y));
                for (int x = 14; x >= 2; x--) path.Add(new Vector2Int(x, 7));
                for (int y = 6; y >= 4; y--) path.Add(new Vector2Int(2, y));
                for (int x = 3; x <= 15; x++) path.Add(new Vector2Int(x, 4));
                for (int y = 3; y >= 1; y--) path.Add(new Vector2Int(15, y));
                for (int x = 16; x <= 17; x++) path.Add(new Vector2Int(x, 1));
                l.path = path.ToArray();
                l.blocked = new[] { new Vector2Int(0, 0), new Vector2Int(17, 11), new Vector2Int(8, 0), new Vector2Int(9, 11), new Vector2Int(0, 5), new Vector2Int(17, 8), new Vector2Int(6, 2) };
                l.startingGold = 260; l.startingLives = 20; l.waveClearBonus = 40; l.waves = waves; l.autoStartDelay = 25f; l.bossEvery = 6;
                l.healthScalingPerWave = 0.15f;
                l.buildableColor = MaterialLibrary.Hex("#3A2E33"); l.pathColor = MaterialLibrary.Hex("#7A4A2E"); l.blockedColor = MaterialLibrary.Hex("#1E1418");
                l.sunColor = MaterialLibrary.Hex("#FFD2A8"); l.ambientSky = MaterialLibrary.Hex("#7A4A3A"); l.ambientGround = MaterialLibrary.Hex("#1A0E0A"); l.fogColor = MaterialLibrary.Hex("#160C0A");
                l.endlessPool = new[] { E("goblin"), E("ogre"), E("bat"), E("frostgolem") };
                l.endlessBossPool = new[] { E("warlord"), E("icewyrm") };
                l.endlessBudgetBase = 50; l.endlessBudgetGrowth = 10;
                l.assaultPool = new[] { E("goblin"), E("bat"), E("ogre"), E("frostgolem"), E("warlord"), E("icewyrm") };
                l.assaultDuration = 360f; l.assaultAiStartingGold = 400; l.assaultAiIncomePerSecond = 3.5f;
                return l;
            });
        }

        /// <summary>Améliorations permanentes : 10 rangs chacune, coûts géométriques — un puits de gemmes pour des dizaines d'heures.</summary>
        public static MetaUpgradeData[] Upgrades()
        {
            return new[]
            {
                Meta("damage", "Forge", "+6 % de dégâts des tours par rang.", MetaEffect.TowerDamage, 0.06f, 10, 20, "#F2A83B"),
                Meta("cost", "Guilde des bâtisseurs", "-3 % sur le coût des tours et améliorations par rang.", MetaEffect.TowerCost, 0.03f, 10, 25, "#7CC6FF"),
                Meta("gold", "Trésor de guerre", "+20 or de départ par rang.", MetaEffect.StartingGold, 20f, 10, 15, "#FFD65A"),
                Meta("lives", "Remparts", "+1 vie de départ par rang.", MetaEffect.StartingLives, 1f, 10, 30, "#FF6A6A"),
                Meta("bounty", "Pillage", "+5 % d'or par ennemi vaincu par rang.", MetaEffect.EnemyGold, 0.05f, 10, 25, "#6CC24A"),
                Meta("refund", "Recyclage", "+4 % de remboursement à la vente par rang.", MetaEffect.SellRefund, 0.04f, 10, 15, "#D46CFF"),
                Meta("horde", "Horde (Assaut)", "+5 % de vie de vos monstres par rang.", MetaEffect.MonsterHealth, 0.05f, 10, 20, "#FF6A3A"),
                Meta("flux", "Flux (Assaut)", "+4 % de régénération de mana par rang.", MetaEffect.ManaRegen, 0.04f, 10, 20, "#7CC6FF"),
            };
        }

        private static MetaUpgradeData Meta(string id, string name, string desc, MetaEffect effect, float value, int maxRank, int baseCost, string hex)
            => BastionPaths.GetOrCreate($"{BastionPaths.DataLevels}/Meta_{id}.asset", () =>
            {
                var m = ScriptableObject.CreateInstance<MetaUpgradeData>();
                m.upgradeId = id; m.displayName = name; m.description = desc; m.effect = effect; m.valuePerRank = value;
                m.maxRank = maxRank; m.baseCost = baseCost; m.costGrowth = 1.55f; m.accent = MaterialLibrary.Hex(hex);
                return m;
            });

        public static LevelCatalog Catalog(LevelData[] levels, MetaUpgradeData[] upgrades)
        {
            BastionPaths.EnsureFolder("Assets/Resources");
            var c = BastionPaths.GetOrCreate($"Assets/Resources/{LevelCatalog.ResourcePath}.asset", () => ScriptableObject.CreateInstance<LevelCatalog>());
            c.levels = levels; c.upgrades = upgrades;
            EditorUtility.SetDirty(c);
            return c;
        }
    }
}
