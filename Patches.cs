using Game;
using HarmonyLib;

namespace ConfigTool;

[HarmonyPatch]
class Patches
{

    /* Example how to add extra info to the Developer UI Info
    [HarmonyPatch(typeof(Game.UI.InGame.DeveloperInfoUISystem), "UpdateExtractorCompanyInfo")]
    [HarmonyPostfix]
    public static void UpdateExtractorCompanyInfo_Postfix(Entity entity, Entity prefab, InfoList info, EntityQuery _____query_746694603_5)
    {
        // private EntityQuery __query_746694603_5;
        //Plugin.Log("UpdateExtractorCompanyInfo");
        ExtractorParameterData singleton = _____query_746694603_5.GetSingleton<ExtractorParameterData>();
        info.Add(new InfoList.Item($"ExtPar: {singleton.m_FertilityConsumption} {singleton.m_ForestConsumption} {singleton.m_OreConsumption} {singleton.m_OilConsumption}"));
    }
    */
}
