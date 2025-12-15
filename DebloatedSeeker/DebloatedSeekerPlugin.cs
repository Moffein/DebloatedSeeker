using System;
using System.Security.Permissions;
using System.Security;
using BepInEx;
using R2API.Utils;
using RoR2;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2.Skills;
using R2API;
using DebloatedSeeker.Modules;
using RoR2.Projectile;
using System.Text.RegularExpressions;
using MonoMod.Cil;
using RoR2.HudOverlay;
using System.Collections.Generic;
using UnityEngine.Networking;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
namespace DebloatedSeeker
{
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.LanguageAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin("com.Moffein.DebloatedSeeker", "DebloatedSeeker", "1.0.0")]
    public class DebloatedSeekerPlugin : BaseUnityPlugin
    {
        internal void Awake()
        {
            PluginUtils.scepterLoaded = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.DestroyedClone.AncientScepter");
            new PluginContentPack().Initialize();
            LanguageTokens.Init();
            Buffs.Init();
            GameObject seekerObject = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/SeekerBody.prefab").WaitForCompletion();
            NerfBaseStats(seekerObject);
            DisablePassive(seekerObject);
            DebloatSpiritPunch();
            DebloatUnseenHand();
            DebloatSojourn();
            DebloatMeditate();

            DebloatSoulSpiral();
            DebloatReprieve();
            DebloatPalmBlast();

            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;
            RoR2Application.onLoad += OnLoadActions;
        }

        private void OnLoadActions()
        {
            PluginUtils.ScepterIndex = ItemCatalog.FindItemIndex("ITEM_ANCIENT_SCEPTER");
        }

        private string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            string local = orig(self, token);
            if (local != token)
            {
                switch (token)
                {
                    case "SEEKER_PRIMARY_DESCRIPTION":
                        local = local.Replace("350", "500");
                        break;
                    case "SEEKER_SECONDARY_DESCRIPTION":
                        local = Regex.Replace(local.Replace("600", "1200"), "3.*5", "5", RegexOptions.None);
                        break;
                    case "SEEKER_SPECIAL_ALT1_DESCRIPTION":
                        local = local.Replace("400", "1000").Replace("700", "2000");
                        break;
                    default:
                        break;
                }
            }
            return local;
        }

        private void NerfBaseStats(GameObject seekerObject)
        {
            CharacterBody body = seekerObject.GetComponent<CharacterBody>();
            body.armor = 0f;
            body.baseMaxHealth = 90f;
            body.levelMaxHealth = 27f;
            body.baseRegen = 1f;
            body.levelRegen = 0.2f;
        }

        private void DisablePassive(GameObject seekerObject)
        {

            SurvivorDef seekerDef = Addressables.LoadAssetAsync<SurvivorDef>("RoR2/DLC2/Seeker/Seeker.asset").WaitForCompletion();
            seekerDef.descriptionToken = LanguageTokens.LoadoutDescriptionToken;

            GenericSkill[] gs = seekerObject.GetComponents<GenericSkill>();
            GenericSkill passive = null;
            foreach (var skill in gs)
            {
                if (skill.skillName == "InnerStrength")
                {
                    skill.hideInCharacterSelect = true;
                    skill.hideInLoadoutSelect = true;
                    passive = skill;
                    break;
                }
            }

            //Disable vanilla Chakra mechanics
            On.RoR2.SeekerController.CmdIncrementChakraGate += SpawnChakraVFXOnly;

            //Disable petal UI
            On.RoR2.SeekerController.OnEnable += RemoveOverlay;
        }

        private void RemoveOverlay(On.RoR2.SeekerController.orig_OnEnable orig, SeekerController self)
        {
            orig(self);
            if (self.overlayController != null)
            {
                HudOverlayManager.RemoveOverlay(self.overlayController);
            }
        }

        //jank
        private void SpawnChakraVFXOnly(On.RoR2.SeekerController.orig_CmdIncrementChakraGate orig, SeekerController self)
        {
            if (NetworkServer.active && self.specialSkillSlot)
            {
                if (self.specialSkillSlot.baseSkill == self.meditateSkillDef)
                {
                    //Apply our own tranquility buff to people in the radius instead
                    List<HurtBox> hurtBoxesList = new List<HurtBox>();
                    List<CharacterBody> buffedBodies = new List<CharacterBody>();

                    SphereSearch sphereSearch = new SphereSearch();
                    sphereSearch.mask = LayerIndex.entityPrecise.mask;
                    sphereSearch.origin = self.gameObject.transform.position;
                    sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.Collide;
                    sphereSearch.radius = 25f;  //HARDCODED VALUE

                    bool hasScepter = false;
                    TeamMask teamMask = default(TeamMask);
                    if (self.characterBody)
                    {
                        if (self.characterBody.teamComponent)
                        {
                            teamMask.AddTeam(self.characterBody.teamComponent.teamIndex);
                        }

                        if (self.characterBody.inventory && self.characterBody.inventory.GetItemCountEffective(PluginUtils.ScepterIndex) > 0)
                        {
                            hasScepter = true;
                        }
                    }

                    sphereSearch.RefreshCandidates();
                    sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
                    sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(hurtBoxesList);
                    int i = 0;
                    int count = hurtBoxesList.Count;
                    while (i < count)
                    {
                        HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
                        if (healthComponent && healthComponent.body && !buffedBodies.Contains(healthComponent.body))
                        {
                            buffedBodies.Add(healthComponent.body);

                            healthComponent.body.AddTimedBuff(Buffs.Tranquility2, 10f);
                            if (hasScepter)
                            {
                                healthComponent.body.AddTimedBuff(Buffs.Tranquility2, 10f);
                            }
                        }
                        i++;
                    }
                    return;
                }
                else if (self.specialSkillSlot.baseSkill == self.palmBlastSkillDef)
                {
                    //Just spawn VFX
                    EffectManager.SpawnEffect(self.palmBlastSuccessEffectPrefab, new EffectData
                    {
                        origin = self.characterBody.corePosition
                    }, true);

                    bool hasScepter = false;
                    if (self.characterBody)
                    {
                        if (self.characterBody.inventory && self.characterBody.inventory.GetItemCountEffective(PluginUtils.ScepterIndex) > 0)
                        {
                            hasScepter = true;
                        }

                        self.characterBody.AddTimedBuff(Buffs.Tranquility2, 10f);
                        if (hasScepter)
                        {
                            self.characterBody.AddTimedBuff(Buffs.Tranquility2, 10f);
                        }
                    }

                    return;
                }
            }
        }

        private void DebloatSpiritPunch()
        {
            //Modify Stats
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.SpiritPunch.asset", "damageCoefficient", "2.5");
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.SpiritPunch.asset", "comboDamageCoefficient", "5");
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.SpiritPunch.asset", "dmgBuffIncrease", "0");

            //Remove Keyword Jonk
            SkillDef skillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Seeker/SeekerBodySpiritPunchCrosshair.asset").WaitForCompletion();
            PluginUtils.RemoveTranquilityKeyword(skillDef);
        }

        private void DebloatUnseenHand()
        {
            //Modify Stats
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.UnseenHand.asset", "fistDamageCoefficient", "12");

            //Modify Projectile
            GameObject handProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/UnseenHandMovingProjectile.prefab").WaitForCompletion().InstantiateClone("DebloatedSeekerUnseenHandMovingProjectile", true);
            PluginContentPack.projectilePrefabs.Add(handProjectile);
            UnseenHandHealingProjectile u = handProjectile.GetComponent<UnseenHandHealingProjectile>();
            u.chakraIncrease = 0f;
            u.fractionOfDamage = 0.035f;
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.UnseenHand.asset", "fistProjectilePrefab", handProjectile);

            //Remove Keyword Jonk
            SkillDef skillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Seeker/SeekerBodyUnseenHand.asset").WaitForCompletion();
            PluginUtils.RemoveTranquilityKeyword(skillDef);
        }

        private void DebloatSojourn()
        {
            //Set baseline to 5 chakra stacks
            IL.RoR2.SojournVehicle.UpdateBlastRadius += SojournVehicle_UpdateBlastRadius;

            //Remove Keyword Jonk
            SkillDef skillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Seeker/SeekerBodySojourn.asset").WaitForCompletion();
            PluginUtils.RemoveTranquilityKeyword(skillDef);
        }

        private void SojournVehicle_UpdateBlastRadius(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, x => x.MatchCallvirt<CharacterBody>("GetBuffCount")))
            {
                c.EmitDelegate<Func<int, int>>(orig => 5);
            }
            else
            {
                Debug.LogError("DebloatedSeeker: SojournVehicle_UpdateBlastRadius IL hook failed.");
            }
        }

        //WHY ARE THERE 4 KEYWORDS
        //WHY CAN SHE REVIVE HERSELF
        //WHY DOES IT HAVE A 70M BLAST RADIUS
        //WHAT IS THIS SKILL EVEN
        private void DebloatMeditate()
        {
            SkillDef skillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Seeker/SeekerBodyMeditate.asset").WaitForCompletion();

            //Nuke the keywords
            skillDef.keywordTokens = new string[]
            {
                LanguageTokens.Tranquility2Token,
                //"SEEKER_SPECIAL_MEDITATE_KEYWORD" //This doesn't need to be a keyword
            };

            //Redo skill description
            skillDef.skillDescriptionToken = LanguageTokens.MeditateToken;

            //Modify stats
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.Meditate.asset", "damageCoefficient", "20");
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.Meditate.asset", "percentPerGate", "0");
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.Meditate.asset", "blastProcCoefficient", "1"); //WHY IS THIS 0.8
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.Meditate.asset", "blastRadiusScaling", "0");   //WHY DOES THIS MULTIPLY RADIUS BY 7X IN VANILLA?????
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.Meditate.asset", "blastRadius", "25"); //WHY IS THIS 70 IN VANILLA

            //WHY IS THE BASE HEALING ACTUALLY A HARDCODED VALUE????
            //PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.Meditate.asset", "healingExplosionAmount", "0.32"); //0.32 vanilla at max chakra

            //Apply new tranquility buff, handled in CmdIncrementChakraGate hook above.

            //RiskyTweaks changes for Meditate
            On.RoR2.SeekerController.CmdTriggerHealPulse += SeekerController_CmdTriggerHealPulse;
            IL.EntityStates.Seeker.Meditate.Update += Meditate_Update;

            //Scepter stuff
            On.EntityStates.Seeker.Meditate.OnEnter += MeditateScepterStats;


            if (PluginUtils.scepterLoaded)
            {
                SkillDef meditateScepter = ScriptableObject.CreateInstance<SkillDef>();
                meditateScepter.skillNameToken = "ANCIENTSCEPTER_SEEKER_MEDITATENAME";  //Steal this from ScepterMod
                meditateScepter.skillDescriptionToken = LanguageTokens.MeditateScepterToken;
                meditateScepter.icon = null;    //Gets set in RegisterScepterSkill, using ScepterMod's icon

                meditateScepter.beginSkillCooldownOnSkillEnd = skillDef.beginSkillCooldownOnSkillEnd;
                meditateScepter.baseMaxStock = skillDef.baseMaxStock;
                meditateScepter.activationState = skillDef.activationState; //Same state, just check for scepter
                meditateScepter.activationStateMachineName = skillDef.activationStateMachineName;
                meditateScepter.attackSpeedBuffsRestockSpeed = skillDef.attackSpeedBuffsRestockSpeed;
                meditateScepter.attackSpeedBuffsRestockSpeed_Multiplier = skillDef.attackSpeedBuffsRestockSpeed_Multiplier;
                meditateScepter.autoHandleLuminousShot = skillDef.autoHandleLuminousShot;
                meditateScepter.cancelSprintingOnActivation = skillDef.cancelSprintingOnActivation;
                meditateScepter.canceledFromSprinting = skillDef.canceledFromSprinting;
                meditateScepter.dontAllowPastMaxStocks = skillDef.dontAllowPastMaxStocks;
                meditateScepter.forceSprintDuringState = skillDef.forceSprintDuringState;
                meditateScepter.fullRestockOnAssign = skillDef.forceSprintDuringState;
                meditateScepter.hideCooldown = skillDef.hideCooldown;
                meditateScepter.hideStockCount = skillDef.hideStockCount;
                meditateScepter.interruptPriority = skillDef.interruptPriority;
                meditateScepter.isCombatSkill = skillDef.isCombatSkill;
                meditateScepter.isCooldownBlockedUntilManuallyReset = skillDef.isCooldownBlockedUntilManuallyReset;
                meditateScepter.keywordTokens = skillDef.keywordTokens;
                meditateScepter.mustKeyPress = skillDef.mustKeyPress;
                meditateScepter.rechargeStock = skillDef.rechargeStock;
                meditateScepter.requiredStock = skillDef.requiredStock;
                meditateScepter.resetCooldownTimerOnUse = skillDef.resetCooldownTimerOnUse;
                meditateScepter.skillName = skillDef.skillName + "DebloatedScepter";
                meditateScepter.stockToConsume = skillDef.stockToConsume;
                meditateScepter.suppressSkillActivation = skillDef.suppressSkillActivation;
                meditateScepter.triggeredByPressRelease = skillDef.triggeredByPressRelease;
                PluginContentPack.skillDefs.Add(meditateScepter);
                PluginUtils.RegisterScepterSkill(meditateScepter, skillDef);
            }
        }

        private void Meditate_Update(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryGotoNext(MoveType.After, x => x.MatchLdcR4(0.04f)))
            {
                c.EmitDelegate<Func<float, float>>(x => 0.25f);
            }
            else
            {
                Debug.LogError("DebloatedSeeker: Meditate base healing IL Hook failed.");
            }

            if (c.TryGotoNext(x => x.MatchCallvirt<BlastAttack>("Fire")))
            {
                c.EmitDelegate<Func<BlastAttack, BlastAttack>>(blast =>
                {
                    blast.damageType.damageType |= DamageType.Stun1s;
                    return blast;
                });
            }
            else
            {
                Debug.LogError("DebloatedSeeker: Meditate Stun IL Hook failed.");
            }
        }
        private void SeekerController_CmdTriggerHealPulse(On.RoR2.SeekerController.orig_CmdTriggerHealPulse orig, SeekerController self, float value, Vector3 corePosition, float blastRadius, float fxScale)
        {
            //orig(self, value, corePosition, blastRadius, fxScale);

            if (!NetworkServer.active) return;

            TeamIndex friendlyTeam = (self.characterBody && self.characterBody.teamComponent) ? self.characterBody.teamComponent.teamIndex : TeamIndex.None;
            List<HurtBox> hurtBoxesList = new List<HurtBox>();
            List<HealthComponent> healedTargets = new List<HealthComponent>();

            //Overwrite vanilla heal pulse logic
            SphereSearch sphereSearch = new SphereSearch();
            sphereSearch.mask = LayerIndex.entityPrecise.mask;
            sphereSearch.origin = corePosition;
            sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.Collide;
            sphereSearch.radius = blastRadius;
            TeamMask teamMask = default(TeamMask);
            teamMask.AddTeam(friendlyTeam);
            sphereSearch.RefreshCandidates();
            sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
            sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(hurtBoxesList);
            int i = 0;
            int count = hurtBoxesList.Count;
            while (i < count)
            {
                HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
                if (!healedTargets.Contains(healthComponent))
                {
                    healedTargets.Add(healthComponent);

                    if (self.characterBody && self.characterBody.healthComponent == healthComponent)
                    {
                        healthComponent.Heal(value, default);
                    }
                    else
                    {
                        healthComponent.Heal(value * 2f, default);
                    }
                }
                i++;
            }
            EffectManager.SpawnEffect(self.healingExplosionPrefab, new EffectData
            {
                origin = corePosition,
                rotation = Quaternion.identity,
                scale = fxScale
            }, true);

            //Cleanse Projectiles
            List<ProjectileController> instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (ProjectileController pc in instancesList)
            {
                if (pc.cannotBeDeleted || pc.teamFilter.teamIndex == friendlyTeam || (pc.transform.position - self.transform.position).sqrMagnitude >= blastRadius * blastRadius) continue;
                toDestroy.Add(pc.gameObject);
            }

            GameObject[] toDestroy2 = toDestroy.ToArray();
            for (int j = 0; j < toDestroy2.Length; j++)
            {
                EffectManager.SimpleEffect(cleanseEffect, toDestroy2[j].transform.position, toDestroy2[j].transform.rotation, true);
                UnityEngine.Object.Destroy(toDestroy2[j]);
            }
        }
        private static GameObject cleanseEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/SpiritPunchMuzzleFlashVFX.prefab").WaitForCompletion();

        private void MeditateScepterStats(On.EntityStates.Seeker.Meditate.orig_OnEnter orig, EntityStates.Seeker.Meditate self)
        {
            orig(self);
            if (self.characterBody && self.characterBody.inventory && self.characterBody.inventory.GetItemCountEffective(PluginUtils.ScepterIndex) > 0)
            {
                self.damageCoefficient *= 1.5f;
                self.blastRadius *= 1.5f;
                self.healingExplosionAmount *= 1.5f;
            }
        }

        private void DebloatSoulSpiral()
        {
            //Modify Projectile
            GameObject projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/SoulSpiralProjectile.prefab").WaitForCompletion().InstantiateClone("DebloatedSeekerSoulSpiralProjectile", true);
            PluginContentPack.projectilePrefabs.Add(projectilePrefab);
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.SoulSpiral.asset", "projectilePrefab", projectilePrefab);

            ProjectileOverlapLimitHits polh = projectilePrefab.GetComponent<ProjectileOverlapLimitHits>();
            polh.hitLimit = 15; //11 vanilla

            SoulSpiralProjectile ssp = projectilePrefab.GetComponent<SoulSpiralProjectile>();
            ssp.hitStackPerBuff = 0;

            UnseenHandHealingProjectile uhhp = projectilePrefab.GetComponent<UnseenHandHealingProjectile>();
            uhhp.barrierPercent = 0.06f;    //0.03 vanilla
            uhhp.chakraIncrease = 0f;   //0.01 vanilla

            //Remove Keyword Jonk
            SkillDef skillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Seeker/SeekerBodySoulSpiral.asset").WaitForCompletion();
            PluginUtils.RemoveTranquilityKeyword(skillDef);
        }

        private void DebloatReprieve()
        {
            //Modify Projectile
            GameObject projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/SeekerCyclonePrefab.prefab").WaitForCompletion().InstantiateClone("DebloatedSeekerCycloneProjectile", true);
            PluginContentPack.projectilePrefabs.Add(projectilePrefab);
            SeekerCycloneController scc = projectilePrefab.GetComponent<SeekerCycloneController>();
            scc.baseLifetime = 7f;
            scc.durationBonusPerStack = 0f;

            GameObject reprieveVehicle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/SojournVehicle/ReprieveVehicle.prefab").WaitForCompletion(); //wont even bother cloning this
            ReprieveVehicle rv = reprieveVehicle.GetComponent<ReprieveVehicle>();
            rv.cyclonePrefab = projectilePrefab;

            //Remove Keyword Jonk
            SkillDef skillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Seeker/SeekerBodyReprieve.asset").WaitForCompletion();
            skillDef.keywordTokens = new string[0]; //Cyclone does not need to be a keyword
            //PluginUtils.RemoveTranquilityKeyword(skillDef);
        }

        private void DebloatPalmBlast()
        {
            //Modify Projectile
            GameObject projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/PalmBlastProjectile.prefab").WaitForCompletion().InstantiateClone("DebloatedSeekerPalmBlastProjectile", true);
            PluginContentPack.projectilePrefabs.Add(projectilePrefab);
            {
                PalmBlastProjectileController pbc = projectilePrefab.GetComponent<PalmBlastProjectileController>();
                pbc.projectileSize = 1.5f; //Would be 2f by vanilla logic
                pbc.bonusProjectileSizePerStack = 0f;
                pbc.damageCoefficient = 10f;
                pbc.bonusDamageCoefficientPerStack = 0f;
                pbc.bonusAllyHealingPercentPerStack = 0f;
                pbc.allyHealingPercent = 0.25f;
                pbc.barrierPercent = 0.15f;
            }


            GameObject projectilePrefab2 = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/PalmBlastChargedProjectile.prefab").WaitForCompletion().InstantiateClone("DebloatedSeekerPalmBlastChargedProjectile", true);
            PluginContentPack.projectilePrefabs.Add(projectilePrefab2);
            {
                PalmBlastProjectileController pbc = projectilePrefab2.GetComponent<PalmBlastProjectileController>();
                pbc.projectileSize = 2.5f;
                pbc.bonusProjectileSizePerStack = 0f;
                pbc.damageCoefficient = 20f;
                pbc.bonusDamageCoefficientPerStack = 0f;
                pbc.bonusAllyHealingPercentPerStack = 0f;
                pbc.allyHealingPercent = 0.5f;
                pbc.barrierPercent = 0.3f;
            }

            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.PalmBlastFire.asset", "projectilePrefab", projectilePrefab);
            PluginUtils.SetAddressableEntityStateField("RoR2/DLC2/Seeker/EntityStates.Seeker.PalmBlastFire.asset", "chargedProjectilePrefab", projectilePrefab2);

            SkillDef skillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Seeker/SeekerPalmBlast.asset").WaitForCompletion();

            //Nuke the keywords
            skillDef.keywordTokens = new string[]
            {
                LanguageTokens.Tranquility2Token,
                //"KEYWORD_MULTIHIT" //WHY IS THIS A KEYWORD
            };

            //Scepter
            if (PluginUtils.scepterLoaded)
            {
                //WHY IS THE NAMESPACE WRONG
                On.PalmBlastProjectileController.Init += PalmBlastProjectileController_Init;

                SkillDef scepter = ScriptableObject.CreateInstance<SkillDef>();
                scepter.skillNameToken = "ANCIENTSCEPTER_SEEKER_PALMBLASTNAME";  //Steal this from ScepterMod
                scepter.skillDescriptionToken = LanguageTokens.PalmBlastScepterToken;
                scepter.icon = null;    //Gets set in RegisterScepterSkill, using ScepterMod's icon

                scepter.beginSkillCooldownOnSkillEnd = skillDef.beginSkillCooldownOnSkillEnd;
                scepter.baseMaxStock = skillDef.baseMaxStock;
                scepter.activationState = skillDef.activationState; //Same state, just check for scepter
                scepter.activationStateMachineName = skillDef.activationStateMachineName;
                scepter.attackSpeedBuffsRestockSpeed = skillDef.attackSpeedBuffsRestockSpeed;
                scepter.attackSpeedBuffsRestockSpeed_Multiplier = skillDef.attackSpeedBuffsRestockSpeed_Multiplier;
                scepter.autoHandleLuminousShot = skillDef.autoHandleLuminousShot;
                scepter.cancelSprintingOnActivation = skillDef.cancelSprintingOnActivation;
                scepter.canceledFromSprinting = skillDef.canceledFromSprinting;
                scepter.dontAllowPastMaxStocks = skillDef.dontAllowPastMaxStocks;
                scepter.forceSprintDuringState = skillDef.forceSprintDuringState;
                scepter.fullRestockOnAssign = skillDef.forceSprintDuringState;
                scepter.hideCooldown = skillDef.hideCooldown;
                scepter.hideStockCount = skillDef.hideStockCount;
                scepter.interruptPriority = skillDef.interruptPriority;
                scepter.isCombatSkill = skillDef.isCombatSkill;
                scepter.isCooldownBlockedUntilManuallyReset = skillDef.isCooldownBlockedUntilManuallyReset;
                scepter.keywordTokens = skillDef.keywordTokens;
                scepter.mustKeyPress = skillDef.mustKeyPress;
                scepter.rechargeStock = skillDef.rechargeStock;
                scepter.requiredStock = skillDef.requiredStock;
                scepter.resetCooldownTimerOnUse = skillDef.resetCooldownTimerOnUse;
                scepter.skillName = skillDef.skillName + "DebloatedScepter";
                scepter.stockToConsume = skillDef.stockToConsume;
                scepter.suppressSkillActivation = skillDef.suppressSkillActivation;
                scepter.triggeredByPressRelease = skillDef.triggeredByPressRelease;
                PluginContentPack.skillDefs.Add(scepter);
                PluginUtils.RegisterScepterSkill(scepter, skillDef);
            }
        }

        private void PalmBlastProjectileController_Init(On.PalmBlastProjectileController.orig_Init orig, PalmBlastProjectileController self, CharacterBody body)
        {
            orig(self, body);
            if (body && body.inventory && body.inventory.GetItemCountEffective(PluginUtils.ScepterIndex) > 0)
            {
                self.damageCoefficient *= 1.5f;
                self.healingToApply *= 1.5f;
                self.barrierPercent *= 1.5f;
            }
        }
    }
}
