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
    public class DirectorState : MonoBehaviour
    {
        public static float baseCreditGain = 2.6f;
        public static float creditGainPerLevel = 0.4f;
        public static float baseWalletSize = 35;
        public static float walletGainPerLevel = 65;
        public static CombatDirector.EliteTierDef[] eliteTiers;

        public static bool snapToNearestNode = false;

        public bool serverSpawnMode;
        
        public enum RateModifier
        {
            None = 0,
            TeleporterBoosted = 1,
            Locked = 2
        }

        private static Random rng;

        public static Inventory monsterInv;

        private bool _secondPage;

        public bool secondPage
        {
            get { return _secondPage; }
            set
            {
                isDirty = true;
                _secondPage = value;
            }
        }

        public bool isDirty;

        private EliteTierIndex _eliteTierIndex;

        public EliteTierIndex eliteTierIndex {
            get { return _eliteTierIndex; }
            set {
                _eliteTierIndex = value;
                isDirty = true;
            }
        }

        public CombatDirector.EliteTierDef eliteTier => eliteTiers[(int)eliteTierIndex];

        public float instanceLevel
        {
            get
            {
                if (!Run.instance) return 0;
                return Run.instance.ambientLevel + (Run.instance.stageClearCount > 5 ? (Run.instance.stageClearCount-5) * 2 : 0);
            }
        }

        private int lastInstanceLevel;

        public float credits;

        private RateModifier _rateModifier;

        public RateModifier rateModifier
        {
            get { return _rateModifier; }
            set
            {
                _rateModifier = value;
                isDirty = true;
            }
        }

        public float burstCharge;

        public bool bursting;

        private const float burstLossRate = 100f / 15;

        public CharacterMaster spectateTarget;

        public float creditsPerSecond { 
            get
            {
                if (rateModifier != RateModifier.Locked) return (baseCreditGain + creditGainPerLevel * instanceLevel) * (rateModifier == RateModifier.TeleporterBoosted ? 1.6f : 1f);
                return (float)(-maxCredits / 3);
            }
        }

        public float maxBoostCharge
        {
            get
            {
                return (int)instanceLevel * 15 + 100;
            }
        }

        public int maxCredits
        {
            get
            {
                return (int)(baseWalletSize + walletGainPerLevel * (((int)instanceLevel)));
            }
        }

        public List<uint> spawnedCharacters;

        public static List<SpawnCard> spawnCardTemplates;

        public event Action OnBurstCharged;
        public event Action OnLevelUp;
        public event Action OnBurstStart;
        public event Action OnBurstFail;
        public event Action<float> OnPurchaseSuccess;
        public event Action OnPurchaseFail;

        void Start()
        {
            rng = new Random();
            eliteTiers = new CombatDirector.EliteTierDef[5]
            {
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
                    costMultiplier = Mathf.LerpUnclamped(1f, 6f, 0.5f),
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
            eliteTierIndex = EliteTierIndex.Normal;
            spawnedCharacters = new List<uint>();
            if (!serverSpawnMode) {
                Debug.Log("Starting new Director in Client Mode");
                TeleporterInteraction.onTeleporterBeginChargingGlobal += TeleportBoost;
                TeleporterInteraction.onTeleporterChargedGlobal += Lock;
                GlobalEventManager.onClientDamageNotified += ChargeOnDamage;
            } else {
                Debug.Log("Starting new Director in Server Mode");
            }
        }

        void Update()
        {
            if (Run.instance == null) return;
            if (bursting)
            {
                burstCharge -= burstLossRate * Time.deltaTime;
                if (burstCharge <= 0)
                {
                    burstCharge = 0;
                    bursting = false;
                }
            }
            if (lastInstanceLevel != (int)instanceLevel) OnLevelUp?.Invoke();
            lastInstanceLevel = (int)instanceLevel;

            if (serverSpawnMode) {
                credits = maxCredits;
            } else {
                credits += creditsPerSecond * Time.deltaTime;
                if (credits > maxCredits) credits = maxCredits;
                if (credits < 0) credits = 0;
            }
            
            
        }
        
        public (float, CharacterMaster) TrySpawn(int index, Vector3 position, Quaternion rotation, EliteTierIndex eliteIndexOverride = EliteTierIndex.Invalid)
        {
            // Modified code taken from DebugToolkit
            // Client mode director cannot and should not be able to spawn - it should send a message 
            // Returns -1 on purchase fail, credits spent otherwise
            if (spawnCardTemplates == null) spawnCardTemplates = new List<SpawnCard>();
            if (index >= spawnCardTemplates.Count || index < 0) return (-1, null);
            if (spawnCardTemplates[index] == null) return (-1, null);
            if (rateModifier == RateModifier.Locked && !serverSpawnMode) return (-1, null);
            SpawnCard card = spawnCardTemplates[index];
            CombatDirector.EliteTierDef selectedEliteTier = eliteIndexOverride == EliteTierIndex.Invalid ? eliteTier : eliteTiers[(int)eliteIndexOverride];
            float multiplier = selectedEliteTier.costMultiplier;
            float swarmsmult = (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Swarms)) ? 0.5f : 1f;
            float realcost = card.directorCreditCost * multiplier * swarmsmult;

            if (!serverSpawnMode) return (realcost > credits ? -1 : realcost, null);

            // Server-side spawning shenanigans 
            GameObject preinst = card.prefab;
            if (!preinst) return (-1, null);
            NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(card.nodeGraphType);
            if (nodeGraph == null) return (-1, null);
            GameObject bodyGameObject;
            if (snapToNearestNode && card.nodeGraphType == MapNodeGroup.GraphType.Ground) {
                NodeGraph.NodeIndex nodeIndex = nodeGraph.FindClosestNodeWithFlagConditions(position, card.hullSize, NodeFlags.None, NodeFlags.None, false);
                if (!nodeGraph.GetNodePosition(nodeIndex, out position)) Debug.Log("Error: Failed to find valid snapping node. Defaulting to direct spawn.");
                else position.y += HullDef.Find(card.hullSize).height;
            }
            bodyGameObject = UnityEngine.Object.Instantiate<GameObject>(preinst, position, rotation);
            CharacterMaster master = bodyGameObject.GetComponent<CharacterMaster>();
            NetworkServer.Spawn(bodyGameObject);
            master.SpawnBody(position, rotation);
            master.inventory.GiveItem(RoR2Content.Items.UseAmbientLevel);
            if (selectedEliteTier != null)
            {
                EliteDef eliteDef = GetRandomEliteDef(selectedEliteTier.eliteTypes);
                if (eliteDef != null)
                {
                    master.inventory.SetEquipmentIndex(eliteDef.eliteEquipmentDef.equipmentIndex);
                    master.inventory.GiveItem(RoR2Content.Items.BoostHp, Mathf.RoundToInt((eliteDef.healthBoostCoefficient - 1) * 10));
                    master.inventory.GiveItem(RoR2Content.Items.BoostDamage, Mathf.RoundToInt(eliteDef.damageBoostCoefficient - 1) * 10);
                }
            }
            if (swarmsmult != 1f) master.inventory.GiveItem(RoR2Content.Items.CutHp);
            if (monsterInv) master.inventory.AddItemsFrom(monsterInv);
            master.teamIndex = TeamIndex.Monster;
            master.GetBody().teamComponent.teamIndex = TeamIndex.Monster;
            DeathRewards killreward = master.GetBodyObject().GetComponent<DeathRewards>();
            if (killreward != null)
            {
                killreward.expReward = (uint)Mathf.Max(1f, realcost * 0.3f * Run.instance.compensatedDifficultyCoefficient);
                killreward.goldReward = (uint)Mathf.Max(1f, realcost * 0.7f * Run.instance.compensatedDifficultyCoefficient);
            }
            if (bursting) {
                master.GetBody().AddTimedBuff(RoR2Content.Buffs.PowerBuff, 0.15f * burstCharge);
                master.GetBody().AddTimedBuff(RoR2Content.Buffs.TeamWarCry, 0.15f * burstCharge);
            }
            return (realcost, master);
        }

        private EliteDef GetRandomEliteDef(EliteDef[] pool)
        {
            return pool[rng.Next(0, pool.Length)];
        }

        public static void UpdateMonsterSelection()
        {
            if (spawnCardTemplates == null) spawnCardTemplates = new List<SpawnCard>(12);
            spawnCardTemplates.Clear();
            if (Run.instance == null || ClassicStageInfo.instance == null || ClassicStageInfo.instance.monsterSelection == null || ClassicStageInfo.instance.monsterSelection.choices == null)
            {
                Debug.LogWarning("No list of spawncards available!");
                return;
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
        }

        public int GetTrueIndex(int i)
        {
            return i + (secondPage ? 6 : 0);
        }

        private void TeleportBoost(TeleporterInteraction tp)
        {
            rateModifier = RateModifier.TeleporterBoosted;
            isDirty = true;
        }

        private void Lock(TeleporterInteraction tp)
        {
            rateModifier = RateModifier.Locked;
            isDirty = true;
        }

        public bool ApplyFrenzy()
        {
            if (!serverSpawnMode) return !(burstCharge < 100 || bursting);
            bursting = true;
            burstCharge = 100;
            foreach (TeamComponent c in TeamComponent.GetTeamMembers(TeamIndex.Monster))
            {
                CharacterBody body = c.body;
                if (body && !body.HasBuff(RoR2Content.Buffs.TeamWarCry)) body.AddTimedBuff(RoR2Content.Buffs.TeamWarCry, 15f);
                if (body && !body.HasBuff(RoR2Content.Buffs.PowerBuff)) body.AddTimedBuff(RoR2Content.Buffs.PowerBuff, 15f);
            }
            return true;
        }
        public void DoBurstTrigger(bool value) {
            if (value) {
                bursting = true;
                OnBurstStart?.Invoke();
            } else OnBurstFail?.Invoke();
        }

        public void DoPurchaseTrigger(float value, CharacterMaster spawned = null) {
            if (value >= 0) {
                credits -= value;
                if (spawned) {
                    UnityAction HandleCharacterDeath = null;
                    HandleCharacterDeath = delegate () {
                        spawnedCharacters.Remove(spawned.netId.Value);
                        isDirty = true;
                        spawned.onBodyDeath.RemoveListener(HandleCharacterDeath);
                    };
                    spawnedCharacters.Add(spawned.netId.Value);
                    spawned.onBodyDeath.AddListener(HandleCharacterDeath);
                    Debug.LogFormat("Added spawned character with ID {0} (Now {1} instances)", spawned.netId.Value, spawnedCharacters.Count);
                } else {
                    Debug.Log("Purchase triggered with no spawn tied to it.");
                }
                OnPurchaseSuccess?.Invoke(value);
            } else OnPurchaseFail?.Invoke();
            isDirty = true;
        }
        private void ChargeOnDamage(DamageDealtMessage msg)
        {
            if (msg.attacker == null || serverSpawnMode) return;
            CharacterMaster attacker = msg.attacker.GetComponent<CharacterMaster>();
            Debug.LogFormat("Damage dealt by {0}", attacker.netId.Value);
            Debug.LogFormat("{0} && {1} && {2}", spawnedCharacters.Contains(attacker.netId.Value), burstCharge < 100, !bursting);
            if (spawnedCharacters.Contains(attacker.netId.Value) && burstCharge < 100 && !bursting)
            {
                burstCharge += msg.damage / maxBoostCharge * 100f;
                if (burstCharge >= 100)
                {
                    burstCharge = 100;
                    OnBurstCharged?.Invoke();
                }
            }
        }
    }
}