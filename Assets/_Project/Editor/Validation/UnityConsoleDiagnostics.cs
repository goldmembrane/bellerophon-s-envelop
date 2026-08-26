using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    internal static class UnityConsoleDiagnostics
    {
        private const int MaximumReportedEntries = 80;
        private const int ErrorModeMask =
            (1 << 0) |
            (1 << 1) |
            (1 << 4) |
            (1 << 6) |
            (1 << 8) |
            (1 << 11) |
            (1 << 13) |
            (1 << 17) |
            (1 << 20) |
            (1 << 21);

        public static void InspectCurrentErrors()
        {
            Debug.Log(BuildReport());
        }

        public static void AssertNoErrors()
        {
            var counts = CurrentCounts();
            if (counts.ErrorCount != 0)
            {
                throw new InvalidOperationException(
                    "Unity Console still contains errors.\n" + BuildReport());
            }

            Debug.Log(
                "UnityConsoleDiagnostics passed. Errors=0, Warnings=" +
                counts.WarningCount.ToString(CultureInfo.InvariantCulture) +
                ", Logs=" +
                counts.LogCount.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static string BuildReport()
        {
            var counts = CurrentCounts();
            var entries = CurrentEntries();
            var errorEntries = entries
                .Where(entry => (entry.Mode & ErrorModeMask) != 0)
                .TakeLast(MaximumReportedEntries)
                .ToArray();
            if (counts.ErrorCount > 0 && errorEntries.Length == 0)
            {
                errorEntries = entries
                    .TakeLast(Math.Min(MaximumReportedEntries, entries.Count))
                    .ToArray();
            }

            var report = new StringBuilder()
                .AppendLine("Unity Console Error Report")
                .AppendLine("ErrorCount=" + counts.ErrorCount.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine("WarningCount=" + counts.WarningCount.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine("LogCount=" + counts.LogCount.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine("VisibleEntryCount=" + entries.Count.ToString(
                    CultureInfo.InvariantCulture))
                .AppendLine("ReportedErrorEntries=" + errorEntries.Length.ToString(
                    CultureInfo.InvariantCulture));
            for (var index = 0; index < errorEntries.Length; index++)
            {
                report.AppendLine(
                    "ErrorEntry[" + index.ToString(CultureInfo.InvariantCulture) +
                    "] Mode=" + errorEntries[index].Mode.ToString(
                        CultureInfo.InvariantCulture));
                report.AppendLine(errorEntries[index].Message);
            }

            return report.ToString().TrimEnd();
        }

        private static ConsoleCounts CurrentCounts()
        {
            var logEntriesType = RequireType("UnityEditor.LogEntries");
            var method = logEntriesType.GetMethod(
                "GetCountsByType",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new InvalidOperationException(
                    "Unity console count API could not be resolved.");
            var arguments = new object[] { 0, 0, 0 };
            method.Invoke(null, arguments);
            return new ConsoleCounts(
                (int)arguments[0],
                (int)arguments[1],
                (int)arguments[2]);
        }

        private static IReadOnlyList<ConsoleEntry> CurrentEntries()
        {
            var logEntriesType = RequireType("UnityEditor.LogEntries");
            var logEntryType = RequireType("UnityEditor.LogEntry");
            var flags = BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            var getCount = logEntriesType.GetMethod("GetCount", flags) ??
                           throw new InvalidOperationException(
                               "Unity console entry count API could not be resolved.");
            var start = logEntriesType.GetMethod("StartGettingEntries", flags) ??
                        throw new InvalidOperationException(
                            "Unity console entry read start API could not be resolved.");
            var end = logEntriesType.GetMethod("EndGettingEntries", flags) ??
                      throw new InvalidOperationException(
                          "Unity console entry read end API could not be resolved.");
            var getEntry = logEntriesType
                .GetMethods(flags)
                .SingleOrDefault(method =>
                {
                    if (method.Name != "GetEntryInternal")
                    {
                        return false;
                    }

                    var parameters = method.GetParameters();
                    return parameters.Length == 2 &&
                           parameters[0].ParameterType == typeof(int) &&
                           parameters[1].ParameterType == logEntryType;
                }) ??
                throw new InvalidOperationException(
                    "Unity console entry read API could not be resolved.");

            var entries = new List<ConsoleEntry>();
            start.Invoke(null, null);
            try
            {
                var count = (int)getCount.Invoke(null, null);
                for (var index = 0; index < count; index++)
                {
                    var entry = Activator.CreateInstance(logEntryType, true);
                    var returned = getEntry.Invoke(null, new[] { (object)index, entry });
                    if (returned is bool succeeded && !succeeded)
                    {
                        continue;
                    }

                    var message = ReadStringMember(entry, "message");
                    if (string.IsNullOrWhiteSpace(message))
                    {
                        message = ReadStringMember(entry, "condition");
                    }

                    entries.Add(new ConsoleEntry(
                        ReadIntMember(entry, "mode"),
                        Normalize(message)));
                }
            }
            finally
            {
                end.Invoke(null, null);
            }

            return entries;
        }

        private static Type RequireType(string fullName)
        {
            return Type.GetType(fullName + ",UnityEditor.dll") ??
                   throw new InvalidOperationException(
                       fullName + " could not be resolved.");
        }

        private static string ReadStringMember(object instance, string name)
        {
            return ReadMember(instance, name)?.ToString() ?? string.Empty;
        }

        private static int ReadIntMember(object instance, string name)
        {
            var value = ReadMember(instance, name);
            return value == null
                ? 0
                : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static object ReadMember(object instance, string name)
        {
            var flags = BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic;
            var type = instance.GetType();
            var field = type.GetField(name, flags);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            return type.GetProperty(name, flags)?.GetValue(instance);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "<empty>";
            }

            var normalized = value.Replace("\r\n", "\n").Trim();
            return normalized.Length <= 8000
                ? normalized
                : normalized.Substring(0, 8000) + "\n<truncated>";
        }

        private readonly struct ConsoleCounts
        {
            public ConsoleCounts(int errorCount, int warningCount, int logCount)
            {
                ErrorCount = errorCount;
                WarningCount = warningCount;
                LogCount = logCount;
            }

            public int ErrorCount { get; }
            public int WarningCount { get; }
            public int LogCount { get; }
        }

        private readonly struct ConsoleEntry
        {
            public ConsoleEntry(int mode, string message)
            {
                Mode = mode;
                Message = message;
            }

            public int Mode { get; }
            public string Message { get; }
        }
    }
}
