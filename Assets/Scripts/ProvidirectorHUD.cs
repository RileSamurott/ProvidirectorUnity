using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RoR2;
using RoR2.UI;
namespace ProvidirectorGame
{
    public class ProvidirectorHUD : MonoBehaviour
    {
        public MonsterIcon[] monsterIcons;
        public RectTransform monsterIconCluster;
        public TextMeshProUGUI notificationText;

        public MoneyBarController moneyBar;
        public BurstBarController burstBar;
        public FocusBarController focusBar;
        public HealthBar spectateHealthBar;
        public TextMeshProUGUI spectateNameText;
        public TextMeshProUGUI pageText;
        public Image tier1Backlight;
        public Image tier2Backlight;
        public Color defaultColor;
        public Color teleporterColor;
        public Color lockedColor;
        private const float notifDuration = 3f;
        private float notificationTimer;
        private float mIconLerpProgress = 1f;

        private ClientState _clientState = null;
        public ClientState clientState {
            get { return _clientState; }
            set {
                if (clientState != null && value != null) {
                    clientState.OnLevelUp -= HandleLevelUp;
                    clientState.OnPurchaseFail -= NotifySpawnFail;
                    clientState.OnBurstFail -= NotifyBurstNotReady;
                    clientState.OnPageChange -= HandlePageChange;
                }
                if (value != null) {
                    _clientState = value;
                    clientState.OnLevelUp += HandleLevelUp;
                    clientState.OnPurchaseFail += NotifySpawnFail;
                    clientState.OnBurstFail += NotifyBurstNotReady;
                    clientState.OnPageChange += HandlePageChange;
                }
            }
        }

        private GameObject _spectateTarget;
        public GameObject spectateTarget {
            get { return _spectateTarget; }
            set {
                _spectateTarget = value;
                if (spectateTarget == null) {
                    spectateHealthBar.enabled = false;
                    spectateNameText.text = "---";
                }
                else {
                    spectateHealthBar.enabled = true;
                    spectateHealthBar.source = spectateTarget.GetComponent<CharacterBody>().healthComponent;
                    spectateNameText.text = Util.GetBestBodyName(spectateTarget);
                }
            }
        }

        private CharacterMaster _focusTarget;
        public CharacterMaster focusTarget {
            get { return _focusTarget; }
            set {
                _focusTarget = value;
                if (focusTarget != null && focusTarget?.GetBodyObject() != null) focusBar.targetText.text = Util.GetBestBodyName(focusTarget.GetBodyObject());
                else focusBar.targetText.text = "No Target";
            }
        }

        // Start is called before the first frame update
        
        void Start()
        {
            if (monsterIcons == null) monsterIcons = new MonsterIcon[6];
            Array.Resize(ref monsterIcons, 6);
        }

        void HandleLevelUp()
        {
            StartCoroutine(moneyBar.FlashBar());
            if (DirectorState.instanceLevel >= 99) burstBar.rank = "MAX";
            else if (DirectorState.instanceLevel >= 40) burstBar.rank = "2";
            else burstBar.rank = "1";
        }

        void HandlePageChange()
        {
            mIconLerpProgress = 0f;
        }

        void Notify(string s)
        {
            if (s == null || s.Length == 0) {
                notificationText.text = "";
                notificationTimer = 0;
                notificationText.enabled = false;
                return;
            }
            notificationText.text = s;
            notificationTimer = notifDuration;
            notificationText.enabled = true;
        }    
        void NotifySpawnFail(SpawnFailReason s)
        {
            switch (s) {
                case SpawnFailReason.NoCredits:
                    Notify("Not enough money!");
                    break;
                case SpawnFailReason.SpawnLimitReached:
                    Notify("Spawn limit reached!");
                    break;
                case SpawnFailReason.ServerSpawnFail:
                    Notify("Failed to summon character!");
                    break;
                default:
                    Notify("Failed to summon character!");
                    break;
            }
        }
        void NotifyBurstNotReady()
        {
            Notify("Burst not ready!");
        }
        // Update is called once per frame
        void Update()
        {
            if (clientState == null)
            {
                foreach (MonsterIcon m in monsterIcons)
                {
                    m.SetMonsterInfo(null, null);
                }
                moneyBar.value = 0;
                moneyBar.maxValue = 0;
                burstBar.value = 0;
                return;
            }
            ClientState state = clientState;
            moneyBar.value = state.credits;
            moneyBar.maxValue = state.maxCredits;
            burstBar.value = state.burstCharge;
            focusBar.value = state.focusCooldownTimer;
            notificationTimer -= Time.deltaTime;
            if (notificationTimer < 0) notificationTimer = 0;
            notificationText.enabled = notificationText.enabled && notificationTimer > 0;
            for (int i = 0; i < 6; i++) monsterIcons[i].UpdatePurchaseableHUD(state);
            if (mIconLerpProgress < 1) {
                mIconLerpProgress += 4*Time.deltaTime;
                if (mIconLerpProgress > 1) mIconLerpProgress = 1;
                Vector3 pos = monsterIconCluster.anchoredPosition;
                float adjusted = 1 - (float)Math.Pow(mIconLerpProgress - 1, 4);
                monsterIconCluster.anchoredPosition = new Vector3(pos.x, 100 + adjusted * 50, pos.z);
            }

            if (!state.isDirty) return;
            state.isDirty = false;
            tier1Backlight.enabled = state.eliteTierIndex == EliteTierIndex.Tier1 || state.eliteTierIndex == EliteTierIndex.Honor1;
            tier2Backlight.enabled = state.eliteTierIndex == EliteTierIndex.Tier2;
            pageText.text = "Page " + (state.secondPage ? "2" : "1");
            switch (state.rateModifier)
            {
                case ClientState.RateModifier.TeleporterBoosted:
                    moneyBar.baseColor = teleporterColor;
                    break;
                case ClientState.RateModifier.Locked:
                    moneyBar.baseColor = lockedColor;
                    break;
                default:
                    moneyBar.baseColor = defaultColor;
                    break;
            }
            spectateTarget = state.spectateTarget;
            focusTarget = state.focusTarget;
            if (ClientState.spawnableCharacters == null) return;
            for (int i = 0; i < 6; i++)
            {
                int k = state.GetTrueIndex(i);
                if (k >= ClientState.spawnableCharacters.Count)
                {
                    monsterIcons[i].SetMonsterInfo(null, null);
                    continue;
                }
                monsterIcons[i].SetMonsterInfo(ClientState.spawnableCharacters[k], state);
            }
        }
    }
}
