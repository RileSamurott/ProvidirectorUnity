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
        private TextMeshProUGUI keytext;
        public TextMeshProUGUI costtext;
        public TooltipProvider ttp;

        public void SetMonsterInfo(SpawnCard s, DirectorState directorState)
        {
            //Debug.Log("Setting monster info for SpawnCard");
            if (s == null || directorState == null)
            {
                icon.sprite = null;
                costtext.text = "";
                ttp.enabled = false;
                return;
            }
            var etd = directorState.eliteTier;
            int level = (int)directorState.instanceLevel;
            float multiplier = etd.costMultiplier * ((RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Swarms)) ? 0.5f : 1f);
            costtext.text = "$" + Math.Floor(s.directorCreditCost*multiplier).ToString();
            ttp.enabled = true;
            CharacterMaster cmprefab = s.prefab.GetComponent<CharacterMaster>();
            string iconName = cmprefab.name.Replace("Master", "Icon");
            if (!iconDB.ContainsKey(iconName))
            {
                if (!iconDB.ContainsKey("DefaultIcon")) Debug.LogErrorFormat("Unable to find fallback icon!");
                else icon.sprite = iconDB["DefaultIcon"];
            }
            else icon.sprite = iconDB[iconName];
            GameObject bodyPrefab = cmprefab.bodyPrefab;
            CharacterBody b = bodyPrefab.GetComponent<CharacterBody>();
            if (b)
            {
                ttp.titleToken = String.Format("Lv. {0} {1}", level, Util.GetBestBodyName(bodyPrefab));
                ttp.bodyToken = String.Format("Cost: ${0}\n{1} HP\n{2} Base Damage", Math.Floor(s.directorCreditCost * multiplier), (b.baseMaxHealth + Mathf.Round(b.baseMaxHealth * 0.3f) * (level - 1)) * multiplier, (b.baseDamage * (1 + 0.2f * (level - 1))) * multiplier);
                ttp.titleColor = (b.bodyColor != Color.clear ? b.bodyColor : Color.gray);
            }
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
