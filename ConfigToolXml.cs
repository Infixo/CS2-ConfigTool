using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;

namespace ConfigTool;

public static class ResizerXml
{
    private static readonly string _settingsFileName = "ResizerSettings.xml";
    private static readonly string _settingsFile = Path.Combine(ColossalFramework.IO.DataLocation.localApplicationData, _settingsFileName);

    private static ResizerSettings _settings = null;
    public static ResizerSettings Settings { get { return _settings; } }

    /// <summary>
    /// Loads default props and prefab names from a file in a user or mod directory.
    /// Settings are set to null id there is any problem during loading.
    /// </summary>
    public static void LoadSettings()
    {
        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ResizerSettings));
            FileStream fs = new FileStream(_settingsFile, FileMode.Open);
            _settings = (ResizerSettings)serializer.Deserialize(fs);
            Debug.Log($"Resizer settings: === default props ===");
            Debug.Log(String.Join("|", Settings?.DefaultProps));
            Debug.Log($"Resizer settings: === prefabs ===");
            foreach (PrefabToResize prefab in Settings?.PrefabsToResize)
                Debug.Log(prefab);
        }
        catch (Exception e)
        {
            Debug.Log($"Resizer settings: exception {e.Message}");
            _settings = null;
        }
    }
}

[XmlRoot("ResizerSettings")]
public class ResizerSettings
{
    [XmlArray("DefaultProps")]
    [XmlArrayItem(typeof(string), ElementName = "Prop")]
    public string[] DefaultProps;

    [XmlArray("PrefabsToResize")]
    [XmlArrayItem(ElementName = "Prefab")]
    public PrefabToResize[] PrefabsToResize;

    public bool CheckPropName(string propName)
    {
        foreach (string propPart in DefaultProps)
            if (propName.ToLower().Contains(propPart.ToLower()))
                return true;
        return false;
    }

    public PrefabToResize FindPrefab(string name)
    {
        return PrefabsToResize.FirstOrDefault(prefab => name.ToLower() == prefab.Name.ToLower());
    }

    public bool CheckPrefabName(string name)
    {
        //return PrefabsToResize.FirstOrDefault( prefab => name.ToLower().Contains(prefab.Name.ToLower()) ) != null;
        return PrefabsToResize.FirstOrDefault( prefab => name.ToLower() == prefab.Name.ToLower() ) != null;
    }

    public Vector3 GetScale(string name)
    {
        //return PrefabsToResize.First( prefab => name.ToLower().Contains(prefab.Name.ToLower()) ).Scale;
        return PrefabsToResize.First( prefab => name.ToLower() == prefab.Name.ToLower() ).Scale;
    }
    public bool CheckIfAllProps(string name)
    {
        //return PrefabsToResize.First( prefab => name.ToLower().Contains(prefab.Name.ToLower()) ).Scale;
        return PrefabsToResize.First(prefab => name.ToLower() == prefab.Name.ToLower()).ResizeAllProps;
    }
}

public class PrefabToResize
{
    [XmlAttribute(AttributeName = "name", DataType = "string")]
    public string Name = "PrefabName";
    [XmlAttribute(AttributeName = "x", DataType = "float")]
    public float X = 1f;
    [XmlAttribute(AttributeName = "y", DataType = "float")]
    public float Y = 1f;
    [XmlAttribute(AttributeName = "z", DataType = "float")]
    public float Z = 1f;
    [XmlAttribute(AttributeName = "props", DataType = "string")]
    public string Props = "default"; // only "all" matters
    public Vector3 Scale { get { return new Vector3(X, Y, Z); } }
    public bool ResizeAllProps { get { return Props == "all"; } }
    public override string ToString()
    {
        return $"Prefab: {Name} Scale: {Scale.ToString("F2")} Props: {Props}";
    }
}

// namespace
