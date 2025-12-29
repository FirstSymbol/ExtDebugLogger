using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExtDebugLogger.Attributes;
using ExtDebugLogger.Interfaces;
using ExtInspectorTools;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ExtDebugLogger
{
    public static class TagsAssembler
    {
        private static readonly Type DefaultDictType = typeof(Dictionary<,>);
        private static readonly Type SerializableDictType = typeof(SerializableDictionary<,>);

        public static IReadOnlyDictionary<Enum, Color> ColorTags { get; private set; } = new Dictionary<Enum, Color>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void InitializeRuntime() => Initialize();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Initialize();
        }
#endif

        private static void Initialize()
        {
            var defaultTags      = CollectTags(DefaultDictType, typeof(IKeepDefaultLoggerTags<>));
            var serializableTags = CollectTags(SerializableDictType, typeof(IKeepSeriaizableLoggerTags<>));

            var combined = new Dictionary<Enum, Color>(defaultTags);
            foreach (var kvp in serializableTags)
                combined[kvp.Key] = kvp.Value; // SerializableDictionary overwrites in case of conflict

            ColorTags = combined;

            Logger.Log($"{ColorTags.Count} colored tags successfully loaded " +
                      $"(default & static: {defaultTags.Count}, serializable: {serializableTags.Count}).", LogTag.ExtDebugLogger);
        }

        private static Dictionary<Enum, Color> CollectTags(Type openDictType, Type openInterfaceType)
        {
            var result = new Dictionary<Enum, Color>();

            var providers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                .Any(field => field.GetCustomAttributes(typeof(ExtDebugLoggerTagsAttribute), inherit: false).Any()))
                .ToArray();

            foreach (var provider in providers)
            {
                // Static fields
                CollectFromFields(provider, BindingFlags.Static, null, openDictType, result);

                // ScriptableObjects
                if (typeof(ScriptableObject).IsAssignableFrom(provider))
                {
                    foreach (var instance in LoadAllScriptableObjects(provider))
                        CollectFromFields(provider, BindingFlags.Instance, instance, openDictType, result);
                }
                // Default csharp fields in classes with an empty constructor
                else if (provider.GetConstructor(Type.EmptyTypes) != null)
                {
                    var instance = Activator.CreateInstance(provider);
                    CollectFromFields(provider, BindingFlags.Instance, instance, openDictType, result);
                }
            }

            return result;
        }

        private static void CollectFromFields(Type type, BindingFlags flags, object instance, Type openDictType, Dictionary<Enum, Color> result)
        {
            var fields = type.GetFields(flags | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.GetCustomAttribute<ExtDebugLoggerTagsAttribute>() != null &&
                            IsValidDictionaryType(f.FieldType, openDictType));

            foreach (var field in fields)
            {
                var dict = field.GetValue(instance);
                if (dict != null)
                    MergeTags(dict, result);
            }
        }

        private static bool IsValidDictionaryType(Type fieldType, Type openDictType)
        {
            if (!fieldType.IsGenericType) return false;
            if (fieldType.GetGenericTypeDefinition() != openDictType) return false;

            var args = fieldType.GetGenericArguments();
            return args.Length == 2 && args[0].IsEnum && args[1] == typeof(Color);
        }

        private static void MergeTags(object dictionary, Dictionary<Enum, Color> target)
        {
            if (dictionary is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item == null) continue;

                    var itemType = item.GetType();
                    if (!itemType.IsGenericType) continue;
                    if (itemType.GetGenericTypeDefinition() != typeof(KeyValuePair<,>)) continue;

                    var keyProp = itemType.GetProperty("Key");
                    var valueProp = itemType.GetProperty("Value");

                    if (keyProp == null || valueProp == null) continue;

                    var key = keyProp.GetValue(item);
                    var value = valueProp.GetValue(item);

                    if (key is Enum enumKey && value is Color color)
                    {
                        target[enumKey] = color;
                    }
                }
            }
        }

        private static IEnumerable<ScriptableObject> LoadAllScriptableObjects(Type type)
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"t:{type.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && asset.GetType() == type)
                    yield return asset;
            }
#else
            foreach (var obj in Resources.FindObjectsOfTypeAll(type))
            {
                if (obj is ScriptableObject so)
                    yield return so;
            }
#endif
        }
    }
}