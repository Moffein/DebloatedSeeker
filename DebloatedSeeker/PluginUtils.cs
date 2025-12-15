using RoR2.Skills;
using RoR2;
using UnityEngine.AddressableAssets;
using System.Linq;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace DebloatedSeeker
{
    internal static class PluginUtils
    {
        internal static bool scepterLoaded;
        internal static ItemIndex ScepterIndex = ItemIndex.None;

        internal static void SetAddressableEntityStateField(string fullEntityStatePath, string fieldName, string value)
        {
            Addressables.LoadAssetAsync<EntityStateConfiguration>(fullEntityStatePath).Completed += handle => SetAddressableEntityStateField_String_Completed(handle, fieldName, value);
        }

        private static void SetAddressableEntityStateField_String_Completed(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<EntityStateConfiguration> handle, string fieldName, string value)
        {
            EntityStateConfiguration esc = handle.Result;
            for (int i = 0; i < esc.serializedFieldsCollection.serializedFields.Length; i++)
            {
                if (esc.serializedFieldsCollection.serializedFields[i].fieldName == fieldName)
                {
                    esc.serializedFieldsCollection.serializedFields[i].fieldValue.stringValue = value;
                    return;
                }
            }
            Debug.LogError("DebloatedSeeker: " + esc + " does not have field " + fieldName);
        }

        internal static void SetAddressableEntityStateField(string fullEntityStatePath, string fieldName, UnityEngine.Object newObject)
        {
            Addressables.LoadAssetAsync<EntityStateConfiguration>(fullEntityStatePath).Completed += handle => SetAddressableEntityStateField_Object_Completed(handle, fieldName, newObject);
        }

        private static void SetAddressableEntityStateField_Object_Completed(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<EntityStateConfiguration> handle, string fieldName, Object newObject)
        {
            EntityStateConfiguration esc = handle.Result;
            for (int i = 0; i < esc.serializedFieldsCollection.serializedFields.Length; i++)
            {
                if (esc.serializedFieldsCollection.serializedFields[i].fieldName == fieldName)
                {
                    esc.serializedFieldsCollection.serializedFields[i].fieldValue.objectValue = newObject;
                    return;
                }
            }
            Debug.LogError("DebloatedSeeker: " + esc + " does not have field " + fieldName);
        }

        internal static void RemoveTranquilityKeyword(SkillDef skillDef)
        {
            if (!skillDef) return;
            skillDef.keywordTokens = skillDef.keywordTokens.Where(keyword => !keyword.Contains("TRANQUILITY_DESCRIPTION")).ToArray();
        }

        internal static void RegisterScepterSkill(SkillDef skill, SkillDef orig)
        {
            if (scepterLoaded) RegisterScepterSkillInternal(skill, orig);
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void RegisterScepterSkillInternal(SkillDef skill, SkillDef orig)
        {
            if (skill.skillNameToken == "ANCIENTSCEPTER_SEEKER_MEDITATENAME")
            {
                skill.icon = AncientScepter.Assets.SpriteAssets.SeekerMeditate2;
            }
            else if (skill.skillNameToken == "ANCIENTSCEPTER_SEEKER_PALMBLASTNAME")
            {
                skill.icon = AncientScepter.Assets.SpriteAssets.SeekerPalmBlast2;
            }
            AncientScepter.AncientScepterItem.instance.RegisterScepterSkill(skill, "SeekerBody", orig);
        }
    }
}
