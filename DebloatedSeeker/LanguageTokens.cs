using R2API;
using System;
using System.Collections.Generic;
using System.Text;

namespace DebloatedSeeker
{
    public static class LanguageTokens
    {
        public const string LoadoutDescriptionToken = "SEEKER_DESCRIPTION_DEBLOATED";

        public const string Tranquility2Token = "SEEKER_TRANQUILITY_DEBLOATED";
        public const string MeditateToken = "SEEKER_SPECIAL_DESCRIPTION_DEBLOATED";
        public const string MeditateScepterToken = "SEEKER_SPECIAL_DESCRIPTION_DEBLOATED_SCEPTER";

        public const string PalmBlastToken = "SEEKER_SPECIAL_ALT1_DESCRIPTION_DEBLOATED";
        public const string PalmBlastScepterToken = "SEEKER_SPECIALL_ALT1_DESCRIPTION_DEBLOATED_SCEPTER";

        internal static void Init()
        {
            LanguageAPI.Add(LanguageTokens.LoadoutDescriptionToken, "As a meditative mid-range brawler, Seeker utilizes powerful healing to sustain herself and her team.<style=cSub>\r\n\r\n< ! > Every third Spirit Punch in a row fires a high-damaging explosive burst of Soul. Flow between targets while holding the primary to deal maximum damage.\r\n\r\n< ! > Seeker\u2019s Unseen Hand heals you for every enemy hit. Prioritize enemy groups to maximize healing.\r\n\r\n< ! > Use Sojourn to escape dangerous situations, rush to teammates, or enter the fray. Use Sojourn again to end your flight to deal damage to enemies. While in Sojourn, your health quickly depletes, so use it wisely.\r\n\r\n< ! > Meditate places you in a vulnerable state, but provides a powerful buff upon completion. Positioning and timing are key to empowering you and your teammates.\r\n</style>\r\n");

            LanguageAPI.Add(LanguageTokens.Tranquility2Token, "<style=cKeywordName>Tranquility</style><style=cSub>Increase movement speed, attack speed, and health regeneration by <style=cIsDamage>30%</style>.");
            //FOR TRANSLATORS: YOUR LANGUAGE HERE
            //LanguageAPI.Add(LanguageTokens.Tranquility2Token, "", "jp");
            //LanguageAPI.Add(LanguageTokens.Tranquility2Token, "", "ko");
            //LanguageAPI.Add(LanguageTokens.Tranquility2Token, "", "pt-br");
            //LanguageAPI.Add(LanguageTokens.Tranquility2Token, "", "ru");
            //LanguageAPI.Add(LanguageTokens.Tranquility2Token, "", "de");
            //etc

            LanguageAPI.Add(LanguageTokens.MeditateToken, "<style=cIsUtility>Focus your mind</style> to blast enemies for <style=cIsDamage>2000% damage</style>, <style=cIsHealing>heal</style> nearby allies, and grant <style=cIsUtility>Tranquility</style> for <style=cIsUtility>10 seconds</style>.");
            //FOR TRANSLATORS: YOUR LANGUAGE HERE
            //LanguageAPI.Add(LanguageTokens.MeditateToken, "", "jp");
            //LanguageAPI.Add(LanguageTokens.MeditateToken, "", "ko");
            //LanguageAPI.Add(LanguageTokens.MeditateToken, "", "pt-br");
            //LanguageAPI.Add(LanguageTokens.MeditateToken, "", "ru");
            //LanguageAPI.Add(LanguageTokens.MeditateToken, "", "de");
            //etc

            LanguageAPI.Add(LanguageTokens.MeditateScepterToken, "<style=cIsUtility>Focus your mind</style> to blast enemies for <style=cIsDamage>3000% damage</style>, <style=cIsHealing>heal</style> nearby allies, and grant <style=cIsUtility>2 stacks of Tranquility</style> for <style=cIsUtility>10 seconds</style>.");

            LanguageAPI.Add(LanguageTokens.PalmBlastScepterToken, "<style=cIsUtility>Charge</style> a <style=cIsDamage>piercing</style>, spectral hand that deals <style=cIsDamage>1500%-3000% damage</style> and <style=cIsHealing>heals</style> allies. <style=cIsDamage>Multihit</style> three characters to gain <style=cIsUtility>2 stacks of Tranquility</style> and <style=cIsHealing>barrier</style>.");
        }
    }
}
