using System;
using System.Configuration;

namespace Boss.Scm.CustomsReportHost
{
    public static class DebugHelper
    {
        public static bool IsDebug => ConfigurationManager.AppSettings["IsDebug"]?.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}