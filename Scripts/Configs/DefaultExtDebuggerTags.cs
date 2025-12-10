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
      { LogTag.Default, Color.white },
      { LogTag.ExtDebugLogger, Color.coral }
    };
  }
}