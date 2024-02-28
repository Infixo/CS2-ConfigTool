using System;
using System.Reflection;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Entities;
using HarmonyLib;

namespace ConfigTool;

[HarmonyPatch(typeof(Game.Prefabs.PrefabSystem), "AddPrefab")]
public static class PrefabSystem_AddPrefab_Patches
{
    public static void DumpFields(PrefabBase prefab, ComponentBase component)
    {
        string className = component.GetType().Name;
        Plugin.Log($"{prefab.name}.{component.name}.CLASS: {className}");

        object obj = (object)component;
        Type type = obj.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            // field components: System.Collections.Generic.List`1[Game.Prefabs.ComponentBase]
            if (field.Name != "isDirty" && field.Name != "active")
            {
                object value = field.GetValue(obj);
                Plugin.Log($"{prefab.name}.{component.name}.{field.Name}: {value}");
            }
        }
    }

    /// <summary>
    /// Configures a specific component withing a specific prefab according to config data.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="prefabConfig"></param>
    /// <param name="comp"></param>
    /// <param name="compConfig"></param>
    private static void ConfigureComponent(PrefabBase prefab, PrefabXml prefabConfig, ComponentBase component, ComponentXml compConfig)
    {
        Type compType = component.GetType();
        Plugin.Log($"Configuring component {prefab.name}.{compType.Name}");
        foreach (FieldXml fieldConfig in compConfig.Fields)
        {
            // Get the FieldInfo object for the field with the given name
            FieldInfo field = compType.GetField(fieldConfig.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                Plugin.Log($"... {field.Name} ({field.FieldType}) => {fieldConfig}");
                if (field.FieldType == typeof(float))
                {
                    field.SetValue(component, fieldConfig.ValueFloat);
                }
                else
                {
                    field.SetValue(component, fieldConfig.ValueInt);
                }
            }
            else
            {
                Plugin.Log($"Warning! Field {fieldConfig.Name} not found in component {compType.Name}.");
            }
        }
        DumpFields(prefab, component); // debug
    }

    /// <summary>
    /// Configures a specific prefab according to the config data.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="prefabConfig"></param>
    private static void ConfigurePrefab(PrefabBase prefab, PrefabXml prefabConfig)
    {
        Plugin.Log($"Configuring prefab {prefab.name}");
        // iterate through components and see which ones need to be changed
        foreach (ComponentBase component in prefab.components)
        {
            string compName = component.GetType().Name;
            if (ConfigToolXml.Settings.IsComponentValid(compName) && prefabConfig.TryGetComponent(compName, out ComponentXml compConfig))
            {
                Plugin.Log($"{prefab.name}.{compName}: valid");
                ConfigureComponent(prefab, prefabConfig, component, compConfig);
            }
            else
                Plugin.Log($"{prefab.name}.{compName}: SKIP");
        }
    }

    [HarmonyPrefix]
    public static bool PrefabConfig_Prefix(object __instance, PrefabBase prefab)
    {
        if (ConfigToolXml.Settings.TryGetPrefab(prefab.name, out PrefabXml prefabConfig))
        {
            ConfigurePrefab(prefab, prefabConfig);
        }
        return true;
    }
    
    [HarmonyPrefix]
    public static bool DemandPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        return true; // DISABLED
        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "DemandPrefab")
        {
            DemandPrefab p = (DemandPrefab)prefab;
            Plugin.Log($"{prefab.name}: resRatio {p.m_FreeResidentialProportion} " +
                $"happiness min {p.m_MinimumHappiness} neu {p.m_NeutralHappiness} eff {p.m_HappinessEffect} " +
                $"homeless neu {p.m_NeutralHomelessness} eff {p.m_HomelessEffect} " +
                $"unemployment neu {p.m_NeutralUnemployment} eff {p.m_UnemploymentEffect}");
            Plugin.Log($"{prefab.name}: comRatio {p.m_FreeCommercialProportion} indRatio {p.m_FreeIndustrialProportion} " +
                $"baseDemand com {p.m_CommercialBaseDemand} ind {p.m_IndustrialBaseDemand} ext {p.m_ExtractorBaseDemand}");
        }
        return true;
    }

    [HarmonyPrefix]
    public static bool EconomyPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "EconomyPrefab")
        {
            EconomyPrefab p = (EconomyPrefab)prefab;
            p.m_ExtractorCompanyExportMultiplier = 0.65f; // default: 0.85f, this change effectively increases Extractor production by 31%
            Plugin.Log($"Modded {prefab.name}: ExtExpMult {p.m_ExtractorCompanyExportMultiplier}");
        }
        return true;
    }

    /* Infixo: this does not have any effect which is super weird
    [HarmonyPrefix]
    public static bool ExtractorParameterPrefab_Prefix(object __instance, PrefabBase prefab)
    {
        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "ExtractorParameterPrefab")
        {
            // This tweaks effectively lower the usage rate of natural resources to 50% of the original
            ExtractorParameterPrefab p = (ExtractorParameterPrefab)prefab;
            p.m_ForestConsumption = 0.5f; // 1f, Wood is used approx. 3x faster
            p.m_FertilityConsumption = 0.05f; // 0.1f, Fetile land is used approx. 4x faster
            p.m_OreConsumption = 1000000f; // 500000f, Ore is used approx. 4x faster
            p.m_OilConsumption = 200000f; // 100000f, Oil is used 4x faster
            Plugin.Log($"Modded {prefab.name}: forest {p.m_ForestConsumption} fertility {p.m_FertilityConsumption} ore {p.m_OreConsumption} oil {p.m_OilConsumption}");
        }
        return true;
    }
    */

    private static readonly Dictionary<string, float> MaxWorkersPerCellDict = new Dictionary<string, float>
    {
        {"Industrial_ForestryExtractor",  0.04f}, // 0.02
        {"Industrial_GrainExtractor",     0.06f}, // 0.032
        {"Industrial_OreExtractor",       0.10f}, // 0.04
        {"Industrial_OilExtractor",       0.15f}, // 0.04
        {"Industrial_CoalMine",           0.15f}, // 0.1
        {"Industrial_StoneQuarry",        0.12f}, // 0.08
        {"Industrial_VegetableExtractor", 0.08f}, // 0.032
        {"Industrial_LivestockExtractor", 0.10f}, // 0.04
        {"Industrial_CottonExtractor",    0.10f}, // 0.04
    };
    
    // NOT USED
    private static readonly Dictionary<string, int> OutputAmountDict = new Dictionary<string, int>
    {
        // default values are 30 for all
        // Infixo: this doesn't increase production because it is countered by increased WPU so the profitability stays the same
        {"Industrial_ForestryExtractor",  30},
        {"Industrial_GrainExtractor",     60},
        {"Industrial_OreExtractor",       40},
        {"Industrial_OilExtractor",       50},
        {"Industrial_CoalMine",           30},
        {"Industrial_StoneQuarry",        50},
        {"Industrial_VegetableExtractor", 90},
        {"Industrial_LivestockExtractor", 90},
        {"Industrial_CottonExtractor",    70},
    };
    
    private static readonly Dictionary<string, WorkplaceComplexity> ComplexityDict = new Dictionary<string, WorkplaceComplexity>
    {
        // Infixo stats before 9,28,11,4 => after 8,21,14,9
        // Manual => Simple
        {"Industrial_GrainExtractor",     WorkplaceComplexity.Simple },
        {"Industrial_OreExtractor",       WorkplaceComplexity.Simple },
        {"Industrial_VegetableExtractor", WorkplaceComplexity.Simple },
        {"Industrial_LivestockExtractor", WorkplaceComplexity.Simple },
        {"Industrial_CottonExtractor",    WorkplaceComplexity.Simple },
        {"Industrial_SawMill",            WorkplaceComplexity.Simple },
        // Simple => Manual
        {"Commercial_FoodStore",   WorkplaceComplexity.Manual },
        {"Commercial_Restaurant",  WorkplaceComplexity.Manual },
        //{"Commercial_GasStation",  WorkplaceComplexity.Manual },
        {"Commercial_Bar",         WorkplaceComplexity.Manual },
        {"Commercial_ConvenienceFoodStore",     WorkplaceComplexity.Manual },
        {"Commercial_FashionStore",             WorkplaceComplexity.Manual },
        //{"Industrial_TextileFromCottonFactory", WorkplaceComplexity.Manual },
        // Simple => Complex
        //{"Commercial_VehicleStore",       WorkplaceComplexity.Complex },
        {"Industrial_MetalSmelter",       WorkplaceComplexity.Complex },
        //{"Industrial_OilExtractor",       WorkplaceComplexity.Complex },
        //{"Commercial_ChemicalStore",      WorkplaceComplexity.Complex },
        {"Industrial_MachineryFactory",   WorkplaceComplexity.Complex },
        {"Industrial_BeverageFromGrainFactory",      WorkplaceComplexity.Complex },
        {"Industrial_BeverageFromVegetablesFactory", WorkplaceComplexity.Complex },
        {"Industrial_FurnitureFactory",   WorkplaceComplexity.Complex },
        // Complex => Hitech
        {"Industrial_ElectronicsFactory", WorkplaceComplexity.Hitech },
        {"Industrial_PlasticsFactory",    WorkplaceComplexity.Hitech },
        //{"Industrial_OilRefinery",        WorkplaceComplexity.Hitech },
        //{"Commercial_DrugStore",          WorkplaceComplexity.Hitech },
        {"Industrial_ChemicalFactory",    WorkplaceComplexity.Hitech },
        //{"Industrial_VehicleFactory",     WorkplaceComplexity.Hitech },
        {"Office_Bank",                   WorkplaceComplexity.Hitech },
        {"Office_MediaCompany",           WorkplaceComplexity.Hitech },
    };
    
    [HarmonyPrefix]
    public static bool Companies_Prefix(PrefabBase prefab)
    {
        return true; // DISABLE
        if (prefab.GetType().Name == "CompanyPrefab")
        {
            // Component ProcessingCompany => m_MaxWorkersPerCell
            if (prefab.Has<ExtractorCompany>())
            {
                ProcessingCompany comp = prefab.GetComponent<ProcessingCompany>();
                if (MaxWorkersPerCellDict.ContainsKey(prefab.name))
                {
                    comp.process.m_MaxWorkersPerCell = MaxWorkersPerCellDict[prefab.name];
                    Plugin.Log($"Modded {prefab.name}: wpc {comp.process.m_MaxWorkersPerCell}");
                }
            }

            // Component Workplace => WorkplaceComplexity, m_Complexity
            if (prefab.Has<Workplace>())
            {
                Workplace comp = prefab.GetComponent<Workplace>();
                if (ComplexityDict.ContainsKey(prefab.name))
                {
                    comp.m_Complexity = ComplexityDict[prefab.name];
                    Plugin.Log($"Modded {prefab.name}: comp {comp.m_Complexity}");
                }
            }
        }
        return true;
    }

    
    [HarmonyPrefix]
    public static bool Buildings_Prefix(object __instance, PrefabBase prefab)
    {
        return true; // DISABLED
        //if (prefab.Has<BuildingProperties>())
        //{
            //Plugin.Log($"{prefab.name}: {prefab.GetType().Name}");
        //}

        // types: BuildingPrefab, RenderPrefab, StaticObjectPrefab, EconomyPrefab, ZonePrefab, etc.
        if (prefab.GetType().Name == "BuildingPrefab" && prefab.Has<BuildingProperties>())
        {
            string ShowResources(Game.Economy.ResourceInEditor[] resArr)
            {
                Game.Economy.Resource res = Game.Economy.EconomyUtils.GetResources(resArr);
                return Game.Economy.EconomyUtils.GetNames(res);
            }
            //BuildingPrefab p = (BuildingPrefab)prefab;
            BuildingProperties bp = prefab.GetComponent<BuildingProperties>();
            Plugin.Log($"{prefab.name}: spc {bp.m_SpaceMultiplier} res {bp.m_ResidentialProperties} " +
                $"com {ShowResources(bp.m_AllowedSold)} " +
                $"ind {ShowResources(bp.m_AllowedManufactured)} " +
                $"war {ShowResources(bp.m_AllowedStored)}");
            //foreach (var component in prefab.components)
                //Plugin.Log($"{prefab.name}: {component.GetType().Name}");
        }
        
        return true;
    }
    
}

/*
[HarmonyPatch]
public static class ProcessingCompany_Patches
{
    [HarmonyPatch(typeof(Game.Prefabs.ProcessingCompany), "Initialize")]
    [HarmonyPrefix]
    public static bool ProcessingCompany_Initialize_Prefix(object __instance, EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<ExtractorCompanyData>(entity))
        {
            //Plugin.Log($"ProcessingCompany_Initialize_Prefix: {entity} has ExtractorCompanyData");
            if (entityManager.HasComponent<IndustrialProcessData>(entity))
            {
                IndustrialProcessData ipd = entityManager.GetComponentData<IndustrialProcessData>(entity);
                Plugin.Log($"ProcessingCompany_Initialize_Prefix: {entity} has IndustrialProcessData, wpc {ipd.m_MaxWorkersPerCell}, out {ipd.m_Output.m_Amount}");
            }
            else
                Plugin.Log($"ProcessingCompany_Initialize_Prefix: {entity} NO IndustrialProcessData");
        }
        //if (entityManager.HasComponent<ExtractorCompany>(entity))
        //{
        //Plugin.Log($"ProcessingCompany_Initialize_Prefix: {entity} has ExtractorCompany");
        //}
        return true;
    }

    [HarmonyPatch(typeof(Game.Prefabs.ProcessingCompany), "Initialize")]
    [HarmonyPostfix]
    public static void ProcessingCompany_Initialize_Postfix(object __instance, EntityManager entityManager, Entity entity)
    {
        if (entityManager.HasComponent<ExtractorCompanyData>(entity))
        {
            //Plugin.Log($"ProcessingCompany_Initialize_Postfix: {entity} has ExtractorCompanyData");
            if (entityManager.HasComponent<IndustrialProcessData>(entity))
            {
                IndustrialProcessData ipd = entityManager.GetComponentData<IndustrialProcessData>(entity);
                Plugin.Log($"ProcessingCompany_Initialize_Postfix: {entity} has IndustrialProcessData, wpc {ipd.m_MaxWorkersPerCell}, out {ipd.m_Output.m_Amount}");
            }
            else
                Plugin.Log($"ProcessingCompany_Initialize_Postfix: {entity} NO IndustrialProcessData");
        }
    }

}
*/

[HarmonyPatch]
public static class ConfigTool_Patches
{
    // Part 1: This is called 1035 times
    /*
    [HarmonyPatch(typeof(Game.Prefabs.AssetCollection), "AddPrefabsTo")]
    [HarmonyPostfix]
    public static void AddPrefabsTo_Postfix()
    {
        Plugin.Log("**************************** Game.Prefabs.AssetCollection.AddPrefabsTo");
    }
    */

    // Part 2: This is called 1 time
    [HarmonyPatch(typeof(Game.SceneFlow.GameManager), "LoadPrefabs")]
    [HarmonyPostfix]
    public static void LoadPrefabs_Postfix()
    {
        Plugin.Log("**************************** Game.SceneFlow.GameManager.LoadPrefabs");
        ConfigToolXml.SaveSettings();
    }

    // Part 3: This is called 1 time
    [HarmonyPatch(typeof(Game.Prefabs.PrefabInitializeSystem), "OnUpdate")]
    [HarmonyPostfix]
    public static void OnUpdate_Postfix()
    {
        Plugin.Log("**************************** Game.Prefabs.PrefabInitializeSystem.OnUpdate");
    }
}
/*
[HarmonyPatch]
public static class PrefabInitializeSystem_Patches
{
    [HarmonyPatch(typeof(Game.Prefabs.PrefabInitializeSystem), "InitializePrefab")]
    [HarmonyPrefix]
    public static bool InitializePrefab_Prefix(object __instance, Entity entity, PrefabBase prefab, List<ComponentBase> components)
    {
        if (prefab.GetType().Name == "CompanyPrefab")
        {
            Plugin.Log($"InitializePrefab_Prefix: {prefab.name} {entity}");
        }
        return true;
    }

    [HarmonyPatch(typeof(Game.Prefabs.PrefabInitializeSystem), "LateInitializePrefab")]
    [HarmonyPrefix]
    public static bool LateInitializePrefab_Prefix(object __instance, Entity entity, PrefabBase prefab)
    {
        if (prefab.GetType().Name == "CompanyPrefab")
        {
            Plugin.Log($"LateInitializePrefab_Prefix: {prefab.name} {entity}");
        }
        return true;
    }

}
*/