using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoR2;
using RoR2.UI;
using System;

namespace ProvidirectorGame
{
    public class MonsterIcon : MonoBehaviour
    {
        // Start is called before the first frame update
        public static List<AssetBundle> iconBundles;
        private static Dictionary<string, Sprite> iconDB;
        public Image icon;
        public Image noPurchaseOverlay;
        public TextMeshProUGUI costtext;
        public TooltipProvider ttp;
        private static Color purchaseOKColor = new Color(0.9150f, 0.8625f, 0);
        private static Color purchaseImpossible = new Color(0.396f, 0.379f, 0.173f);
        public int purchaseCost = -1;
        public void SetMonsterInfo(SpawnCardDisplayData data, ClientState clientState)
        {
            if (data == null || clientState == null)
            {
                icon.sprite = null;
                costtext.text = "";
                ttp.enabled = false;
                purchaseCost = -1;
                return;
            }
            CombatDirector.EliteTierDef etd = clientState.selectedEliteTier;
            int level = (int)DirectorState.instanceLevel;
            float multiplier = etd.costMultiplier * ((RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Swarms)) ? 0.5f : 1f);
            purchaseCost = (int)Math.Floor(data.price * multiplier);
            costtext.text = "$" + purchaseCost.ToString();
            ttp.enabled = true;
            if (!iconDB.ContainsKey(data.iconName))
            {
                if (!iconDB.ContainsKey("DefaultIcon")) icon.sprite = null; // Unable to find default
                else icon.sprite = iconDB["DefaultIcon"];
            }
            else icon.sprite = iconDB[data.iconName];
            ttp.titleToken = String.Format("{0} (${1})", data.bodyName, purchaseCost);
            ttp.bodyToken = String.Format("{0} HP\n{1} Base Damage", (data.baseMaxHealth + Mathf.Round(data.baseMaxHealth * 0.3f) * (level - 1)) * multiplier, (data.baseDamage * (1 + 0.2f * (level - 1))) * multiplier);
            ttp.titleColor = (data.bodyColor != Color.clear ? data.bodyColor : Color.gray);
        }

        public void UpdatePurchaseableHUD(ClientState state) {
            if (purchaseCost < 0 || state == null || state.credits < (float)purchaseCost || state.rateModifier == ClientState.RateModifier.Locked) {
                noPurchaseOverlay.enabled = true;
                costtext.color = purchaseImpossible;
                return;
            } 
            noPurchaseOverlay.enabled = false;
            costtext.color = purchaseOKColor;
        }

        public static void AddIconsFromBundle(AssetBundle bundle)
        {
            if (iconDB == null) iconDB = new Dictionary<string, Sprite>();
            if (iconBundles == null) iconBundles = new List<AssetBundle>();
            if (bundle == null) return;
            iconBundles.Add(bundle);
            string[] assetNames = bundle.GetAllAssetNames();
            foreach (string s in assetNames)
            {
                UnityEngine.Object[] assets = bundle.LoadAssetWithSubAssets(s, typeof(Sprite));
                foreach (UnityEngine.Object spr in assets)
                {
                    if (spr != null)
                    {
                        iconDB[spr.name] = (Sprite)spr;
                        Debug.LogFormat("Added icon {0}", spr.name);
                    }
                }
            }
        }
    }
}
