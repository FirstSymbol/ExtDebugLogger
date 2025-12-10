using System.Collections.Generic;
using ExtDebugLogger.Attributes;
using ExtDebugLogger.Interfaces;
using UnityEngine;

namespace ExtDebugLogger.Configs
{
  public class DefaultExtDebuggerTags : IKeepDefaultLoggerTags<LogTag>
  {
    [field: ExtDebugLoggerTags]
    public Dictionary<LogTag, Color> ColorDictionary { get; private set; } = new()
    {
      { LogTag.Default, new Color(1f, 1f, 1f, 1f) },
      { LogTag.ExtDebugLogger, new Color(1f, 0.4980392f, 0.3137255f, 1f) }
    };
  }
}