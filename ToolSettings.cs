using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace ConfigTool;

[XmlRoot("ToolSettings")]
public class ToolSettingsXml
{
    [XmlArray("ValidComponents")]
    [XmlArrayItem(typeof(string), ElementName = "Component")]
    public string[] ValidComponents;

    public bool IsComponentValid(string nameToCheck)
    {
        if (ValidComponents is null || ValidComponents.Length == 0) return false;
        return Array.IndexOf(ValidComponents, nameToCheck) != -1;
        //foreach (string propPart in ValidComponents)
        //if (propName == propPart)
        //return true;
        //return false;
    }

    [XmlElement("Prefab")]
    public List<PrefabXml> Prefabs { get; set; }

    public bool TryGetPrefab(string name, out PrefabXml prefab)
    {
        prefab = default(PrefabXml);
        foreach (var item in Prefabs)
            if (item.Name == name)
            {
                prefab = item;
                return true;
            }
        return false;
    }
}

public class PrefabXml
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("Component")]
    public List<ComponentXml> Components { get; set; }

    public override string ToString()
    {
        return $"PrefabXml: {Name}";
    }

    public void DumpToLog()
    {
        Plugin.Log(ToString());
        foreach (ComponentXml component in Components)
            component.DumpToLog();
    }

    internal bool TryGetComponent(string name, out ComponentXml component)
    {
        component = default(ComponentXml);
        foreach (var item in Components)
            if (item.Name == name)
            {
                component = item;
                return true;
            }
        return false;
    }
}

public class ComponentXml
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    // Different types of elements can be defined here using XmlElement attributes
    [XmlElement("Field")]
    public List<FieldXml> Fields { get; set; }

    public override string ToString()
    {
        return $"ComponentXml: {Name}";
    }

    public void DumpToLog()
    {
        Plugin.Log(ToString());
        foreach (FieldXml field in Fields)
            Plugin.Log(field.ToString());
    }

    internal bool TryGetField(string name, out FieldXml field)
    {
        field = default(FieldXml);
        foreach (var item in Fields)
            if (item.Name == name)
            {
                field = item;
                return true;
            }
        return false;
    }
}

public class FieldXml
{
    public override string ToString()
    {
        string res = $"{Name}=";
        if (ValueInt.HasValue)
            res += $" {ValueInt} (int)";
        if (ValueFloat.HasValue)
            res += $" {ValueFloat} (float)";
        if (!string.IsNullOrEmpty(Value))
            res += $" {Value}";
        return res;
    }

    [XmlAttribute("name")]
    public string Name { get; set; }

    // STRING is the default value
    // use string.IsNullOrEmpty() to check if specified

    [XmlAttribute(AttributeName = "value", DataType = "string")]
    public string Value { get; set; }

    // INTEGER

    [XmlIgnore]
    public bool ValueIntSpecified { get; set; } // Use a bool field to determine if the value is present

    [XmlIgnore]
    public int? ValueInt { get; set; } // Nullable in case it is not defined

    [XmlAttribute("valueInt")]
    public int XmlValueInt
    {
        get { return ValueInt.GetValueOrDefault(); }
        set { ValueInt = value; ValueIntSpecified = true; }
    }

    // FLOAT
    
    [XmlIgnore]
    public bool ValueFloatSpecified { get; set; } // Use a bool field to determine if the value is present

    [XmlIgnore]
    public float? ValueFloat { get; set; } // Nullable in case it is not defined

    [XmlAttribute(AttributeName = "valueFloat", DataType = "float")]
    public float XmlValueFloat
    {
        get { return ValueFloat.GetValueOrDefault(); }
        set { ValueFloat = value; ValueFloatSpecified = true; }
    }
}


public static class ConfigToolXml
{
    private static readonly string _settingsFileName = "ToolSettings.xml";
    private static readonly string _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private static readonly string _settingsFile = Path.Combine(_assemblyPath, _settingsFileName); // TODO: change to a CO framework method

    private static readonly string _dumpFileName = "DumpSettings.xml";
    private static readonly string _dumpFile = Path.Combine(_assemblyPath, _dumpFileName); // TODO: change to a CO framework method

    private static ToolSettingsXml _settings = null;
    public static ToolSettingsXml Settings { get { return _settings; } }

    /// <summary>
    /// Loads prefab config data from a file in the mod directory.
    /// Settings are set to null id there is any problem during loading.
    /// </summary>
    public static void LoadSettings()
    {
        //try
        //{
            XmlSerializer serializer = new XmlSerializer(typeof(ToolSettingsXml));
            using (FileStream fs = new FileStream(_settingsFile, FileMode.Open))
            {
                _settings = (ToolSettingsXml)serializer.Deserialize(fs);
            }
            // Verify and output deserialized data
            //Plugin.Log($"NULL: {Settings is null}");
            if (Settings.ValidComponents.Length == 0)
            {
                Plugin.Log("Warning! No valid components are defined.");
            }
            else
            {
                Plugin.Log($"VALID COMPONENTS {Settings.ValidComponents.Length}");
                foreach (string compName in Settings.ValidComponents)
                    Plugin.Log(compName);
            }
            Plugin.Log("PREFAB CONFIG DATA");
            foreach (PrefabXml prefab in Settings?.Prefabs)
                prefab.DumpToLog();
        //}
        //catch (Exception e)
        //{
            //Plugin.Log($"ERROR: Cannot load settings, exception {e.Message}");
            //_settings = null;
        //}
    }

    public static void SaveSettings()
    {
        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ToolSettingsXml));
            using (FileStream fs = new FileStream(_dumpFile, FileMode.Create))
            {
                serializer.Serialize(fs, Settings);
            }
            Plugin.Log($"Settings dumped to file {_dumpFile}");
        }
        catch (Exception e)
        {
            Plugin.Log($"ERROR: Cannot dump settings, exception {e.Message}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Deserialize XML file into objects
        ToolSettingsXml toolSettings;
        XmlSerializer serializer = new XmlSerializer(typeof(ToolSettingsXml));
        using (FileStream fs = new FileStream("C:\\Repos\\Learning\\XmlReader\\ToolSettings.xml", FileMode.Open))
        {
            toolSettings = (ToolSettingsXml)serializer.Deserialize(fs);
        }

        // Output deserialized data

        foreach (var compName in toolSettings.ValidComponents)
            Console.WriteLine(compName);
        Console.WriteLine();

        foreach (var prefab in toolSettings.Prefabs)
        {
            Console.WriteLine($"Prefab Name: {prefab.Name}");
            /*foreach (var field in prefab.Component.Fields)
            {
                Console.WriteLine($"  Field Name: {field.Name}");

                // Output different types of field values based on the present attributes
                if (field.ValueInt.HasValue)
                    Console.WriteLine($"    Value (Int): {field.ValueInt}");
                else if (field.ValueFloat.HasValue)
                    Console.WriteLine($"    Value (Float): {field.ValueFloat}");
                else if (!string.IsNullOrEmpty(field.ValueString))
                    Console.WriteLine($"    Value (String): {field.ValueString}");
            }*/
            Console.WriteLine();
        }
    }
}