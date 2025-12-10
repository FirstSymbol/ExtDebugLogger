using System;
using UnityEngine;
using ExtInspectorTools;

namespace ExtDebugLogger.Interfaces
{
  public interface IKeepSeriaizableLoggerTags<T> where T : Enum
  {
    public SerializableDictionary<T, Color> ColorDictionary { get; }
  }
}