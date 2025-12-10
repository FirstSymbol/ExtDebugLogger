<span style="font-family:monospace;">

# ExtDebugLogger
## Installation
1. Install all dependencies plugins:
    - [ExtInspectorTools](https://github.com/FirstSymbol/ExtInspectorTools);
2. Download package
    - From package manager - https://github.com/FirstSymbol/ExtDebugLogger.git
    - From file - download and drop in project folder.
## How to use
1. Configure colors:
   - Create default csharp class and implement interface [IKeepDefaultLoggerTags](./Scripts/Interfaces/IKeepDefaultLoggerTags.cs) or create field
   ```csharp
     [access modifier] static Dictionary<[any enum], UnityEngine.Color> [any name]
     ```
   and mark this field with attribute **[ExtDebbugLoggerTag]**
   - Create simple SerializableObject and implement interface [IKeepSerializableLoggerTags](./Scripts/Interfaces/IKeepSerializableLoggerTags.cs) and mark this field with attribute **[ExtDebbugLoggerTag]**
2. Execute static Logger methods with 2 parameters: string, enum. See the examples below.

## Examples
### Create SerializableObject config parameters
#### Code
```csharp
[CreateAssetMenu(fileName = "ColorTags Config", menuName = "Configs/ColorTags Config", order = 0)]
public class ColorTagsConfig : ScriptableObject, IKeepSeriaizableLoggerTags<ExampleTags>
{
  [field: SerializeField, ExtDebugLoggerTags]
  public SerializableDictionary<ExampleTags, Color> ColorDictionary { get; private set;}
}
```
#### Inspector
<img width="587" height="284" alt="InspectorView" src="https://github.com/user-attachments/assets/b29fac6f-6c0f-4235-ad4e-58117d50d853" /><br>
### Create static csharp config parameters
#### Code
```csharp
public class ExtLoggerTags : IKeepDefaultLoggerTags<ExampleTags2>
{
  [field: ExtDebugLoggerTags]
  public Dictionary<ExampleTags2, Color> ColorDictionary { get; private set; } = new()
  {
    { ExampleTags2.Example1, Color.brown },
    { ExampleTags2.Example2, Color.bisque }, 
  };
}
```
### Calling methods
#### Code
```csharp
private void Awake()
{
  Logger.Log("SerializableObject log1", ExampleTags.TestTag1);
  Logger.Log("SerializableObject log2", ExampleTags.TestTag2);
  Logger.Log("Static field log", ExampleTags2.Example1);
  Logger.Log("Static field log", ExampleTags2.Example2);
  Logger.Log("Default log"); // Default log
  Logger.Warn("Default log"); // Warning log
  Logger.Error("Default log"); // Error log
}
```
#### Editor
<img width="513" height="432" alt="Unity_X71tObR2Bo" src="https://github.com/user-attachments/assets/f29af623-5855-435c-ae00-35e743146e94" /><br>
