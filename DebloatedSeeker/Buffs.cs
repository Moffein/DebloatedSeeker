using DebloatedSeeker.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DebloatedSeeker
{
    public class Buffs
    {
        public static BuffDef Tranquility2;

        internal static void Init()
        {
            CreateTranquility2();
        }

        private static void CreateTranquility2()
        {
            if (Tranquility2) return;

            BuffDef orig = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC2/Seeker/RevitalizeBuff/bdRevitalizeBuff.asset").WaitForCompletion();

            BuffDef bd = ScriptableObject.CreateInstance<BuffDef>();
            bd.name = "DebloatedSeekerTranquility";
            bd.canStack = true;
            bd.isCooldown = false;
            bd.isDebuff = false;
            bd.buffColor = orig.buffColor;
            bd.iconSprite = orig.iconSprite;
            PluginContentPack.buffDefs.Add(bd);

            (bd as UnityEngine.Object).name = bd.name;
            Tranquility2 = bd;

            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += CharacterBody_AddTimedBuff_BuffDef_float;

            //A bit unfitting but whatever
            /*IL.RoR2.CharacterBody.OnClientBuffsChanged += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(
                     x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "WarCryBuff")
                    ))
                {
                    c.Index += 2;
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<bool, CharacterBody, bool>>((hasWarCry, self) =>
                    {
                        return hasWarCry || self.HasBuff(Tranquility2);
                    });
                }
                else
                {
                    UnityEngine.Debug.LogError("DebloatedSeeker: OnClientBuffsChanged IL Hook failed");
                }
            };*/

            //Maybe this looks more aurelionite-like?
            IL.RoR2.CharacterBody.UpdateAllTemporaryVisualEffects += (il) =>
            {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(
                     x => x.MatchLdsfld(typeof(RoR2Content.Buffs), "Immune")
                    ))
                {
                    c.Index += 2;
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Func<bool, CharacterModel, bool>>((hasBuff, self) =>
                    {
                        return hasBuff || (self.body.HasBuff(Tranquility2));
                    });
                }
                else
                {
                    Debug.LogError("DebloatedSeeker: Failed to set up Tranquility2 Overlay.");
                }
            };
        }

        private static void CharacterBody_AddTimedBuff_BuffDef_float(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
        {
            orig(self, buffDef, duration);
            if (buffDef == Tranquility2)
            {
                foreach (var b in self.timedBuffs)
                {
                    if (b.buffIndex == buffDef.buffIndex && b.timer < duration)
                    {
                        b.timer = duration;
                    }
                }
            }
        }

        private static void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(Tranquility2);
            if (buffCount > 0)
            {
                args.attackSpeedMultAdd += 0.3f * buffCount;
                args.moveSpeedMultAdd += 0.3f * buffCount;
                args.regenMultAdd += 0.3f * buffCount;
            }
        }
    }
}
