using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Axiom.Settings;
using Styx.Common;

namespace Axiom.Helpers
{
    class Log
    {
        public static void WriteLog(string text)
        {
            Logging.Write(text);
        }
        public static void WriteLog(string text, Color Color)
        {
            Logging.Write(Color, text);
        }
        public static void WriteLog(LogLevel level, string text)
        {
            if (Styx.Helpers.GlobalSettings.Instance.LogLevel >= level)
                Logging.Write(text);
        }
        public static void WriteLog(LogLevel level, string text, Color Color)
        {
            if (Styx.Helpers.GlobalSettings.Instance.LogLevel >= level)
                Logging.Write(Color, text);
        }
        public static void WriteQuiet(string text)
        {
            Logging.WriteQuiet(text);
        }
        public static void WriteQuiet(string text, Color Color)
        {
            Logging.WriteQuiet(Color, text);
        }
        public static void WriteQuiet(LogLevel level, string text)
        {
            if (Styx.Helpers.GlobalSettings.Instance.LogLevel >= level)
                Logging.WriteQuiet(text);
        }
        public static void WriteQuite(LogLevel level, string text, Color Color)
        {
            if (Styx.Helpers.GlobalSettings.Instance.LogLevel >= level)
                Logging.WriteQuiet(Color, text);
        }
        public static void WritetoFile(string text)
        {
            Logging.WriteToFileSync(LogLevel.Normal, text);
        }
        public static void WritetoFile(LogLevel level, string text)
        {
            if (Styx.Helpers.GlobalSettings.Instance.LogLevel >= level)
                Logging.WriteToFileSync(level, text);
        }
    }
}
