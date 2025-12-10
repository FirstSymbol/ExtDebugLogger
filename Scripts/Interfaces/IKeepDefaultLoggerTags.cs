using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExtDebugLogger.Interfaces
{
  public interface IKeepDefaultLoggerTags<T>  where T : Enum
  {
    public static Dictionary<T, Color> ColorDictionary { get; }
  }
}