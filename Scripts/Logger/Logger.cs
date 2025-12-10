using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExtDebugLogger.Extensions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ExtDebugLogger
{
    public class Logger
    {
        private static readonly Enum[] _tagsToExclude = { };

        private static Dictionary<Enum, Color> _tagColors => TagsAssembler.ColorTags;

        private static bool IsColoredLogs => true;
        
        [Conditional("DEV"), Conditional("UNITY_EDITOR")]
        public static void Log(string text, Enum tag = null)
        {
            tag = ValidateEnum(tag);
            if (_tagsToExclude.Contains(tag)) return;

            if (_tagColors.TryGetValue(tag, out Color color) && IsColoredLogs)
            {
                Debug.Log($"{color.ColorizeWithBrackets(tag)} {text}");
            }
            else
            {
                Debug.LogFormat("[{0}] {1}", tag, text);
            }
        }

        [Conditional("DEV"), Conditional("UNITY_EDITOR")]
        public static void Warn(string text, Enum tag = null)
        {
            tag = ValidateEnum(tag);
            if (_tagColors.TryGetValue(tag, out Color color) && IsColoredLogs)
            {
                Debug.LogWarning($"[Warn]{color.ColorizeWithBrackets(tag)} {text}");
            }
            else
            {
                Debug.LogWarningFormat("[Warn][{0}] {1}", tag, text);
            }
        }

        public static void Error(string text, Enum tag = null)
        {
            tag = ValidateEnum(tag);
            if (_tagColors.TryGetValue(tag, out Color color) && IsColoredLogs)
            {
                Debug.LogError($"[Exception]{color.ColorizeWithBrackets(tag)} {text}");
            }
            else
            {
                Debug.LogErrorFormat("[Exception][{0}] {1}", tag, text);
            }
        }

        private static Enum ValidateEnum(Enum tag)
        {
            if (tag == null) tag = LogTag.Default;
            return tag;
        }
    }
}