using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using RoR2;
using RoR2.Navigation;
using Random = System.Random;


namespace ProvidirectorGame
{
    public enum  EliteTierIndex {
        Invalid = -1,
        Normal = 0,
        Tier1 = 1,
        Honor1 = 2,
        Tier2 = 3
    }
    public abstract class DirectorState: MonoBehaviour
    {
        public static float baseCreditGain = 1.5f;
        public static float creditGainPerLevel = 0.2f;
        public static float baseWalletSize = 80;
        public static float walletGainPerLevel = 20;
        public static int directorSelfSpawnCap = 40;
        public static CombatDirector.EliteTierDef[] eliteTiers = new CombatDirector.EliteTierDef[5] {
            new CombatDirector.EliteTierDef
            {
                costMultiplier = 1f,
                eliteTypes = new EliteDef[1]
            },
            new CombatDirector.EliteTierDef
            {
                costMultiplier = 6f,
                eliteTypes = new EliteDef[4]
                {
                    RoR2Content.Elites.Lightning,
                    RoR2Content.Elites.Ice,
                    RoR2Content.Elites.Fire,
                    DLC1Content.Elites.Earth
                }
            },
            new CombatDirector.EliteTierDef
            {
                costMultiplier = 3f,
                eliteTypes = new EliteDef[4]
                {
                    RoR2Content.Elites.LightningHonor,
                    RoR2Content.Elites.IceHonor,
                    RoR2Content.Elites.FireHonor,
                    DLC1Content.Elites.EarthHonor
                }
            },
            new CombatDirector.EliteTierDef
            {
                costMultiplier = 6f * 6f,
                eliteTypes = new EliteDef[2]
                {
                    RoR2Content.Elites.Poison,
                    RoR2Content.Elites.Haunted
                }
            },
            new CombatDirector.EliteTierDef
            {
                costMultiplier = 6f,
                eliteTypes = new EliteDef[1] { RoR2Content.Elites.Lunar }
            }
        };
        public static bool snapToNearestNode = false;
        protected static Random rng = new Random();
        public static Inventory monsterInv;
        public static float instanceLevel
        {
            get
            {
                if (!Run.instance) return 0;
                return Run.instance.ambientLevel;
            }
        }
        protected const float burstLossRate = 100f / 15;
        public int maxCredits
        {
            get
            {
                return (int)(baseWalletSize + walletGainPerLevel * (((int)instanceLevel)));
            }
        }
        [HideInInspector]
        public float burstCharge;
        [HideInInspector]
        public bool bursting;
        protected static EliteDef GetRandomEliteDef(EliteDef[] pool)
        {
            return pool[rng.Next(0, pool.Length)];
        }

        public static SpawnCardDisplayData ExtractDisplayData(SpawnCard card)
        {
            if (card == null) return new SpawnCardDisplayData();
            CharacterMaster cmprefab = card.prefab.GetComponent<CharacterMaster>();
            GameObject bodyprefab = cmprefab.bodyPrefab;
            CharacterBody b = bodyprefab.GetComponent<CharacterBody>();
            return new SpawnCardDisplayData()
            {
                iconName = cmprefab.name.Replace("Master", "Icon"),
                bodyName = b.GetDisplayName(),
                bodyColor = b.bodyColor,
                baseMaxHealth = b.baseMaxHealth,
                baseDamage = b.baseDamage,
                price = (int)card.directorCreditCost
            };
        }

        protected static List<SpawnCard> GetNewMonsterSelectionInternal()
        {
            List<SpawnCard> spawnCardTemplates = new List<SpawnCard>();
            spawnCardTemplates.Clear();
            if (Run.instance == null || ClassicStageInfo.instance == null || ClassicStageInfo.instance.monsterSelection == null || ClassicStageInfo.instance.monsterSelection.choices == null)
            {
                Debug.LogWarning("No list of spawncards available!");
                return spawnCardTemplates;
            }
            WeightedSelection<DirectorCard>.ChoiceInfo[] monsterlist = ClassicStageInfo.instance.monsterSelection.choices;
            foreach (var c in monsterlist)
            {
                //Debug.LogFormat("Attempting to add a new spawncard...");
                if (c.value == null)
                {
                    //Debug.LogFormat("No directorcard available!");
                    continue;
                }
                SpawnCard toAdd = c.value.spawnCard;
                if (toAdd == null)
                {
                    //Debug.LogFormat("No spawncard available!");
                    continue;
                }
                spawnCardTemplates.Add(toAdd);
                //Debug.LogFormat("Added new spawncard {0}!", toAdd.prefab.name.Replace("Master",""));
            }
            Debug.LogFormat("Total {0} spawncards have been generated.", spawnCardTemplates.Count);
            spawnCardTemplates.Sort((a, b) => { return a.directorCreditCost - b.directorCreditCost; });
            return spawnCardTemplates;
        }
    }
}