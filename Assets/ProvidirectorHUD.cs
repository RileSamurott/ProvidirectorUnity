using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using RoR2;
using RoR2.UI;
namespace ProvidirectorGame
{
    public class ProvidirectorHUD : MonoBehaviour
    {
        public MonsterIcon[] monstericons;
        public TextMeshProUGUI notificationtext;
        public MoneyBarController moneyBar;
        public BurstBarController burstBar;
        public HealthBar spectateHealthBar;
        public TextMeshProUGUI spectateNameText;
        public Color defaultColor;
        public Color teleporterColor;
        public Color lockedColor;
        private const float notifDuration = 3f;
        private float notificationTimer;
        private DirectorState _directorState = null;
        public DirectorState directorState {
            get { return _directorState; }
            set {
                if (directorState != null && value != null) {
                    directorState.OnLevelUp -= HandleLevelUp;
                    directorState.OnBurstCharged -= HandleBurstCharged;
                    directorState.OnBurstStart -= HandleBurstStart;
                    directorState.OnPurchaseFail -= NotifyInsufficientCash;
                    directorState.OnBurstFail -= NotifyBurstNotReady;
                }
                if (value != null) {
                    _directorState = value;
                    directorState.OnLevelUp += HandleLevelUp;
                    directorState.OnBurstCharged += HandleBurstCharged;
                    directorState.OnBurstStart += HandleBurstStart;
                    directorState.OnPurchaseFail += NotifyInsufficientCash;
                    directorState.OnBurstFail += NotifyBurstNotReady;
                }
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            if (monstericons == null) monstericons = new MonsterIcon[6];
            Array.Resize(ref monstericons, 6);
        }
        
        void HandleLevelUp()
        {
            StartCoroutine(moneyBar.FlashBar());
        }

        void HandleBurstCharged()
        {
            burstBar.SetIsMax(true);
        }

        void HandleBurstStart()
        {
            StartCoroutine(burstBar.PulseUntilDepleted());
        }

        void Notify(string s)
        {
            if (notificationtext == null) return;
            notificationtext.text = s;
            notificationTimer = notifDuration;
            notificationtext.enabled = true;
        }
        
        void NotifyInsufficientCash()
        {
            Notify("Not enough money!");
        }

        void NotifyBurstNotReady()
        {
            Notify("Burst not ready!");
        }

        // Update is called once per frame
        void Update()
        {
            if (directorState == null)
            {
                foreach (MonsterIcon m in monstericons)
                {
                    m.SetMonsterInfo(null, null);
                }
                moneyBar.value = 0;
                moneyBar.maxValue = 0;
                burstBar.value = 0;
                Notify("(Debug) No hooked state!");
                return;
            }
            DirectorState state = directorState;
            moneyBar.value = state.credits;
            moneyBar.maxValue = state.maxCredits;
            burstBar.value = state.burstCharge;
            notificationTimer -= Time.deltaTime;
            if (notificationTimer < 0) notificationTimer = 0;
            notificationtext.enabled = notificationtext.enabled && notificationTimer > 0;
            if (!state.isDirty) return;
            state.isDirty = false;
            
            switch (state.rateModifier)
            {
                case DirectorState.RateModifier.TeleporterBoosted:
                    moneyBar.baseColor = teleporterColor;
                    break;
                case DirectorState.RateModifier.Locked:
                    moneyBar.baseColor = lockedColor;
                    break;
                default:
                    moneyBar.baseColor = defaultColor;
                    break;
            }
            if (DirectorState.spawnCardTemplates == null) return;
            for (int i = 0; i < 6; i++)
            {
                int k = state.GetTrueIndex(i);
                if (k > DirectorState.spawnCardTemplates.Count - 1)
                {
                    monstericons[i].SetMonsterInfo(null, null);
                    continue;
                }
                monstericons[i].SetMonsterInfo(DirectorState.spawnCardTemplates[k], directorState);
            }
        }
    }
}
