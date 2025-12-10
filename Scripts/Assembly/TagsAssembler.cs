#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExtDebugLogger.Interfaces;
using ExtInspectorTools;
using UnityEngine;

namespace ExtDebugLogger
{
  public static partial class TagsAssembler
  {
    static Type _defaultType = typeof(Dictionary<,>);
    static Type _serializableType = typeof(SerializableDictionary<,>);
    private static Dictionary<Enum, Color> _defaultFields;
    private static Dictionary<Enum, Color> _serializableFields;
    public static Dictionary<Enum, Color> ColorTags { get; private set; }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorRun()
    {
      Initialize();
    }
#endif
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Runtime()
    {
      Initialize();
    }

    internal static void Initialize()
    {
      _defaultFields = FindFields(_defaultType);
      _serializableFields = FindFields(_serializableType);
      ValidateDicts();
      ColorTags = _defaultFields.Union(_serializableFields).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
      
      Logger.Log($"{ColorTags.Count} tags successfully loaded.", LogTag.ExtDebugLogger);
    }

    private static void ValidateDicts()
    {
      _defaultFields ??= new Dictionary<Enum, Color>();
      _serializableFields ??= new Dictionary<Enum, Color>();
    }

    private static Dictionary<Enum, Color> FindFields(Type genericDef)
    {
      Type openInterface = genericDef == typeof(Dictionary<,>) ? typeof(IKeepDefaultLoggerTags<>) : typeof(IKeepSeriaizableLoggerTags<>);

      var providerTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a => a.GetTypes())
        .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openInterface))
        .ToList();

      var staticEntries = providerTypes
        .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
          .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == genericDef &&
                      f.FieldType.GetGenericArguments() is { Length: 2 } args && args[0].IsEnum && args[1] == typeof(Color) &&
                      f.GetCustomAttributes(typeof(Attributes.ExtDebugLoggerTags), true).Any())
          .Select(f => ExtractEntries(f.GetValue(null), genericDef))
          .Concat(t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == genericDef &&
                        p.PropertyType.GetGenericArguments() is { Length: 2 } args && args[0].IsEnum && args[1] == typeof(Color) &&
                        p.GetCustomAttributes(typeof(Attributes.ExtDebugLoggerTags), true).Any())
            .Select(p => ExtractEntries(p.GetValue(null), genericDef))))
        .SelectMany(e => e);

      var instanceEntries = providerTypes
        .SelectMany(t =>
        {
          object[] instances;
          if (typeof(ScriptableObject).IsAssignableFrom(t))
          {
            instances = Resources.FindObjectsOfTypeAll(t);
          }
          else
          {
            var ctor = t.GetConstructor(Type.EmptyTypes);
            instances = ctor != null ? new object[] { Activator.CreateInstance(t) } : new object[0];
          }
          return instances.Select(instance => (t, instance));
        })
        .SelectMany(ti =>
        {
          var t = ti.t;
          var instance = ti.instance;
          return t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == genericDef &&
                        f.FieldType.GetGenericArguments() is { Length: 2 } args && args[0].IsEnum && args[1] == typeof(Color) &&
                        f.GetCustomAttributes(typeof(Attributes.ExtDebugLoggerTags), true).Any())
            .Select(f => ExtractEntries(f.GetValue(instance), genericDef))
            .Concat(t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
              .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == genericDef &&
                          p.PropertyType.GetGenericArguments() is { Length: 2 } args && args[0].IsEnum && args[1] == typeof(Color) &&
                          p.GetCustomAttributes(typeof(Attributes.ExtDebugLoggerTags), true).Any())
              .Select(p => ExtractEntries(p.GetValue(instance), genericDef)));
        })
        .SelectMany(e => e);

      return staticEntries.Concat(instanceEntries)
        .ToDictionary(de => (Enum)de.Key, de => (Color)de.Value);
    }

    private static IEnumerable<KeyValuePair<object, object>> ExtractEntries(object dictObj, Type genericDef)
    {
      if (dictObj == null) yield break;

      var dictType = dictObj.GetType();
      if (!dictType.IsGenericType || dictType.GetGenericTypeDefinition() != genericDef) yield break;

      var args = dictType.GetGenericArguments();
      if (args.Length != 2 || !args[0].IsEnum || args[1] != typeof(Color)) yield break;

      var getEnumeratorMethod = dictType.GetMethod("GetEnumerator");
      if (getEnumeratorMethod == null) yield break;

      var enumerator = getEnumeratorMethod.Invoke(dictObj, null);
      var enumType = enumerator.GetType();

      var moveNext = enumType.GetMethod("MoveNext");
      var current = enumType.GetProperty("Current");
      if (moveNext == null || current == null) yield break;

      while ((bool)moveNext.Invoke(enumerator, null))
      {
        var kvp = current.GetValue(enumerator);
        var kvpType = kvp.GetType();

        var keyProp = kvpType.GetProperty("Key");
        var valueProp = kvpType.GetProperty("Value");
        if (keyProp == null || valueProp == null) continue;

        var key = keyProp.GetValue(kvp);
        var value = valueProp.GetValue(kvp);

        yield return new KeyValuePair<object, object>(key, value);
      }

      if (enumerator is IDisposable disp) disp.Dispose();
    }
    
  }
}