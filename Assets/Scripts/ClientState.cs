using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using RoR2;

namespace ProvidirectorGame {
    public class SpawnCardDisplayData: MessageBase {
        public string iconName;
        public string bodyName;
        public Color bodyColor;
        public float baseMaxHealth;
        public float baseDamage;
        public int price;
        public override void Serialize(NetworkWriter writer) {
            writer.Write(iconName);
            writer.Write(bodyName);
            writer.Write(bodyColor);
            writer.Write(baseMaxHealth);
            writer.Write(baseDamage);
            writer.Write(price);
        }

        public override void Deserialize(NetworkReader reader) {
            iconName = reader.ReadString();
            bodyName = reader.ReadString();
            bodyColor = reader.ReadColor();
            baseMaxHealth = reader.ReadSingle();
            baseDamage = reader.ReadSingle();
            price = reader.ReadInt32();
        }
    }

    public enum SpawnFailReason {
        NoCredits = 0,
        SpawnLimitReached = 1,
        ServerSpawnFail = 2
    }
    public class ClientState : DirectorState
    {
        public enum RateModifier
        {
            None = 0,
            TeleporterBoosted = 1,
            Locked = 2
        }
        public bool isDirty;
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
        private int lastInstanceLevel;
        private EliteTierIndex _eliteTierIndex;
        public EliteTierIndex eliteTierIndex {
            get { return _eliteTierIndex; }
            set {
                _eliteTierIndex = value;
                isDirty = true;
            }
        }
        public CombatDirector.EliteTierDef selectedEliteTier => eliteTiers[(int)eliteTierIndex];
        public float creditsPerSecond { 
            get
            {
                if (rateModifier != RateModifier.Locked) return (baseCreditGain + creditGainPerLevel * instanceLevel) * (rateModifier == RateModifier.TeleporterBoosted ? 1.6f : 1f) * (Run.instance.participatingPlayerCount + 1) / 2f;
                return -maxCredits / 3.5f;
            }
        }
        public float maxBoostCharge
        {
            get
            {
                return (int)instanceLevel * 50 + 200;
            }
        }
        protected List<CharacterMaster> spawnedCharacters;
        public static List<SpawnCardDisplayData> spawnableCharacters = new List<SpawnCardDisplayData>();
        private bool _secondPage;
        public bool secondPage
        {
            get { return _secondPage; }
            set
            {
                isDirty = true;
                if (spawnableCharacters.Count > 6) {
                    _secondPage = value;
                    OnPageChange?.Invoke();
                }
                else _secondPage = false;
            }
        }
        public bool canBurst => !(burstCharge < 100 || bursting);

        private GameObject _spectateTarget;
        public GameObject spectateTarget {
            get { return _spectateTarget; }
            set {
                _spectateTarget = value;
                isDirty = true;
            }
        }

        private const float focusCooldownLength = 5f;
        public float focusCooldownTimer = 0f;
        private float cachedCredits = 0f;
        private CharacterMaster _focusTarget;
        public CharacterMaster focusTarget {
            get { return _focusTarget; }
            set {
                _focusTarget = value;
                isDirty = true;
            }
        }
        
        public event Action OnBurstCharged;
        public event Action OnLevelUp;
        public event Action OnBurstStart;
        public event Action OnBurstFail;
        public event Action<float> OnPurchaseSuccess;
        public event Action<SpawnFailReason> OnPurchaseFail;
        public event Action OnFocusTarget;
        public event Action OnPageChange;
        public event Action<int> OnCachedLimitReached;
        
        void Start() {
            eliteTierIndex = EliteTierIndex.Normal;
            spawnedCharacters = new List<CharacterMaster>();
            eliteTierIndex = EliteTierIndex.Normal;
            TeleporterInteraction.onTeleporterBeginChargingGlobal += TeleportBoost;
            TeleporterInteraction.onTeleporterChargedGlobal += Lock;
            GlobalEventManager.onClientDamageNotified += ChargeOnDamage;
        }

        void OnDestroy() {
            TeleporterInteraction.onTeleporterBeginChargingGlobal -= TeleportBoost;
            TeleporterInteraction.onTeleporterChargedGlobal -= Lock;
            GlobalEventManager.onClientDamageNotified -= ChargeOnDamage;
        }

        public bool ActivateFocus(CharacterMaster c) {
            if (focusCooldownTimer <= 0f) {
                focusTarget = c;
                focusCooldownTimer = 5f;
                OnFocusTarget?.Invoke();
                return true;
            }
            return false;
        }

        void Update()
        {
            if (Run.instance == null) return;
            if (focusCooldownTimer > 0) focusCooldownTimer -= Time.deltaTime;
            else focusCooldownTimer = 0;
            if (bursting)
            {
                burstCharge -= burstLossRate * Time.deltaTime;
                if (burstCharge <= 0)
                {
                    burstCharge = 0;
                    bursting = false;
                }
            } else if (rateModifier == RateModifier.Locked && burstCharge > 0) {
                burstCharge -= 20 * Time.deltaTime;
                if (burstCharge <= 0) burstCharge = 0;
            }
            if (lastInstanceLevel != (int)instanceLevel) OnLevelUp?.Invoke();
            lastInstanceLevel = (int)instanceLevel;
            credits += creditsPerSecond * Time.deltaTime;
            if (credits > maxCredits) {
                cachedCredits += credits - maxCredits;
                credits = maxCredits;
                if (cachedCredits >= maxCredits / 2) {
                    OnCachedLimitReached?.Invoke((int)cachedCredits);
                    cachedCredits = 0;
                }
            }
            if (credits < 0) credits = 0;
        }
        public int GetTrueIndex(int i)
        {
            return i + (secondPage ? 6 : 0);
        }
        public bool IsAbleToSpawn(int index, Vector3 position, Quaternion rotation, out int adjustedCost, EliteTierIndex eliteIndexOverride = EliteTierIndex.Normal)
        {
            adjustedCost = -1;
            if (spawnableCharacters == null) spawnableCharacters = new List<SpawnCardDisplayData>();
            if (index >= spawnableCharacters.Count || index < 0) return false;
            if (spawnableCharacters[index] == null) return false;
            if (rateModifier == RateModifier.Locked) return false;
            if (eliteIndexOverride == EliteTierIndex.Invalid) return false;
            SpawnCardDisplayData data = spawnableCharacters[index];
            CombatDirector.EliteTierDef selectedEliteTier = eliteTiers[(int)eliteIndexOverride];
            float multiplier = selectedEliteTier.costMultiplier;
            float swarmsmult = (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Swarms)) ? 0.5f : 1f;
            float realcost = data.price * multiplier * swarmsmult;
            if (credits >= realcost && spawnedCharacters.Count < directorSelfSpawnCap) {
                adjustedCost = (int) realcost;
                credits -= realcost;
                return true;
            } else if (spawnedCharacters.Count >= directorSelfSpawnCap) {
                OnPurchaseFail?.Invoke(SpawnFailReason.SpawnLimitReached);
            } else OnPurchaseFail?.Invoke(SpawnFailReason.NoCredits);
            return false;
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
        public void DoBurstTrigger(bool value) {
            if (value) {
                bursting = true;
                OnBurstStart?.Invoke();
            } else OnBurstFail?.Invoke();
        }
        public void DoPurchaseTrigger(float value, CharacterMaster spawned = null) {
            if (value >= 0) {
                if (spawned) {
                    UnityAction HandleCharacterDeath = null;
                    HandleCharacterDeath = delegate () {
                        if (spawned == null) {
                            Debug.Log("Object was destroyed immediately and is now null, cleansing all null references.");
                            spawnedCharacters.RemoveAll(delegate (CharacterMaster master) { return master == null; });
                        } else spawnedCharacters.Remove(spawned);
                        spawned?.onBodyDeath.RemoveListener(HandleCharacterDeath);
                        Debug.LogFormat("Death: There are now {0} characters.", spawnedCharacters.Count);
                    };
                    spawnedCharacters.Add(spawned);
                    Debug.LogFormat("Spawn: There are now {0} characters.", spawnedCharacters.Count);
                    spawned.onBodyDeath.AddListener(HandleCharacterDeath);
                }
                OnPurchaseSuccess?.Invoke(value);
            }
        }
        private void ChargeOnDamage(DamageDealtMessage msg)
        {
            if (msg == null || msg.attacker == null || rateModifier == RateModifier.Locked) return;
            CharacterBody attackerBody = msg.attacker.GetComponent<CharacterBody>();
            if (!attackerBody) {
                Debug.LogError("Failed to find attacker Body from DDM.");
                return;
            }
            CharacterMaster attacker = attackerBody.master;
            if (!attacker) {
                Debug.LogError("Failed to find attacker Master from DDM.");
                return;
            }
            if (spawnedCharacters.Contains(attacker) && burstCharge < 100 && !bursting)
            {
                burstCharge += msg.damage / maxBoostCharge * 100f;
                if (burstCharge >= 100)
                {
                    burstCharge = 100;
                    OnBurstCharged?.Invoke();
                }
            }
        }
        public static SpawnCardDisplayData ExtractDisplayData(SpawnCard card) {
            CharacterMaster cmprefab = card.prefab.GetComponent<CharacterMaster>();
            GameObject bodyprefab = cmprefab.bodyPrefab;
            CharacterBody b = bodyprefab.GetComponent<CharacterBody>();
            return new SpawnCardDisplayData() {
                iconName = cmprefab.name.Replace("Master", "Icon"),
                bodyName = Util.GetBestBodyName(bodyprefab),
                bodyColor = b.bodyColor,
                baseMaxHealth = b.baseMaxHealth,
                baseDamage = b.baseDamage,
                price = (int)card.directorCreditCost
            };
        }
        public static int UpdateMonsterSelection() {
            if (spawnableCharacters == null) spawnableCharacters = new List<SpawnCardDisplayData>();
            spawnableCharacters.Clear();
            foreach (SpawnCard card in GetNewMonsterSelectionInternal()) if (card) spawnableCharacters.Add(ExtractDisplayData(card));
            return spawnableCharacters.Count;
        }
    }
}
