using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using RoR2;
using RoR2.Audio;
using RoR2.Navigation;
namespace ProvidirectorGame {
    public class ServerState : DirectorState
    {
        public static List<SpawnCard> spawnCardTemplates = new List<SpawnCard>();

        public static Dictionary<int, BuffDef> burstBuffRequirements = new Dictionary<int, BuffDef>();
        
        void Update()
        {
            if (Run.instance == null) return;
            if (bursting)
            {
                burstCharge -= burstLossRate * Time.deltaTime;
                if (burstCharge <= 0)
                {
                    burstCharge = 100;
                    bursting = false;
                }
            }
        }
        public bool TrySpawn(int index, Vector3 position, Quaternion rotation, EliteTierIndex eliteTier, bool snappingOverride, out CharacterMaster spawned, out int trueCost)
        {
            spawned = null;
            trueCost = -1;
            if (spawnCardTemplates == null) spawnCardTemplates = new List<SpawnCard>();
            if (index >= spawnCardTemplates.Count || index < 0) return false;
            if (spawnCardTemplates[index] == null) return false;
            if (eliteTier == EliteTierIndex.Invalid) return false;
            SpawnCard card = spawnCardTemplates[index];
            CombatDirector.EliteTierDef selectedEliteTier = eliteTiers[(int)eliteTier];
            float multiplier = selectedEliteTier.costMultiplier;
            float swarmsmult = (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Swarms)) ? 0.5f : 1f;
            float realcost = card.directorCreditCost * multiplier * swarmsmult;
            if (maxCredits < realcost) return false;
            GameObject preinst = card.prefab;
            NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(card.nodeGraphType);
            if (!preinst || nodeGraph == null) return false;
            GameObject bodyGameObject;
            if (snappingOverride && card.nodeGraphType == MapNodeGroup.GraphType.Ground) {
                NodeGraph.NodeIndex nodeIndex = nodeGraph.FindClosestNodeWithFlagConditions(position, card.hullSize, NodeFlags.None, NodeFlags.None, false);
                if (!nodeGraph.GetNodePosition(nodeIndex, out position)) Debug.LogError("Error: Failed to find valid snapping node. Defaulting to direct spawn.");
                else {
                    RaycastHit hitInfo = default(RaycastHit);
                    Ray ray = new Ray(position + Vector3.up * 2.5f, Vector3.down);
                    float maxDistance = 4f;
                    float bodyRadius = 1f;
                    var a = preinst.GetComponent<CapsuleCollider>();
                    if (a) bodyRadius = a.radius;
                    else {
                        var b = preinst.GetComponent<SphereCollider>();
                        if (b) bodyRadius = b.radius;
                    }
                    if (Physics.SphereCast(ray, bodyRadius, out hitInfo, maxDistance, LayerIndex.world.mask)) position.y = ray.origin.y - hitInfo.distance;
                    position.y += Util.GetBodyPrefabFootOffset(preinst);

                }
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
            if (bursting) AddBurstBuffs(master.GetBody(), 0.15f * burstCharge);
            spawned = master;
            trueCost = (int)realcost;
            return true;
        }

        public bool ActivateBurst()
        {
            bursting = true;
            foreach (TeamComponent c in TeamComponent.GetTeamMembers(TeamIndex.Monster))
            {
                AddBurstBuffs(c.body);
                c.body.AddTimedBuff(RoR2Content.Buffs.Energized, 4f);
            }
            return true;
        }

        public int UpdateMonsterSelection() {
            spawnCardTemplates = GetNewMonsterSelectionInternal();
            return spawnCardTemplates.Count;
        }

        private void AddBurstBuffs(CharacterBody body, float duration = 15f) {
            if ((int)instanceLevel >= 99) body.AddTimedBuff(RoR2Content.Buffs.ArmorBoost, duration);
            if  ((int)instanceLevel >= 40) body.AddTimedBuff(RoR2Content.Buffs.PowerBuff, duration);
            body.AddTimedBuff(RoR2Content.Buffs.TeamWarCry, duration);
            
        }
    }
}
