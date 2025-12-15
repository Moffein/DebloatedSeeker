using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DebloatedSeeker.Modules
{
    public class PluginContentPack : IContentPackProvider
    {
        public static ContentPack content = new ContentPack();
        public static List<GameObject> projectilePrefabs = new List<GameObject>();
        public static List<BuffDef> buffDefs = new List<BuffDef>();
        public static List<SkillDef> skillDefs = new List<SkillDef>();

        public string identifier => "DebloatedSeeker.content";

        internal void Initialize()
        {
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(this);
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(content, args.output);
            yield break;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            content.buffDefs.Add(buffDefs.ToArray());
            content.projectilePrefabs.Add(projectilePrefabs.ToArray());
            content.skillDefs.Add(skillDefs.ToArray());
            yield break;
        }
    }
}
