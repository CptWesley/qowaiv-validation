#if NET5_0_OR_GREATER
#if DEBUG

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Qowaiv.Validation.Abstractions;

internal static class ModuleInit
{
    private static readonly object lck = new();
    private static readonly HashSet<Assembly> instrumented = new();
    private static readonly Harmony harmony = new("qowaiv-message-patcher");
    private static readonly HarmonyMethod postfix = new(SymbolExtensions.GetMethodInfo<IValidationMessage>((__instance) => InjectStacktrace(__instance)));

    [ModuleInitializer]
    [Impure]
    public static void Init()
    {
        AppDomain.CurrentDomain.AssemblyLoad += (s, e) => TryInstrument(e.LoadedAssembly);

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            TryInstrument(asm);
        }
    }

    [Impure]
    private static void TryInstrument(Assembly assembly)
    {
        lock (lck)
        {
            if (instrumented.Contains(assembly))
            {
                return;
            }

            instrumented.Add(assembly);
        }

        Instrument(assembly);
    }

    [Impure]
    private static void Instrument(Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(type => typeof(IValidationMessage).IsAssignableFrom(type))
            .Where(type => !type.IsInterface)
            .Where(type => !type.IsAbstract)
            .Where(type => type.IsClass);

        foreach (var type in types)
        {
            Instrument(type);
        }
    }

    [Impure]
    private static void Instrument(Type type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            Instrument(constructor);
        }
    }

    [Impure]
    private static void Instrument(ConstructorInfo constructor)
    {
        harmony.Patch(constructor, null, postfix);
    }

    private static void InjectStacktrace(IValidationMessage __instance)
    {
        if (__instance.Severity <= ValidationSeverity.None)
        {
            return;
        }

        var trace = new StackTrace(2, true);
        __instance.RegisterTrace(trace);
    }
}

#endif

public static class MessageDebuggingExtensions
{
    private static readonly ConditionalWeakTable<IValidationMessage, StackTrace> table = new ConditionalWeakTable<IValidationMessage, StackTrace>();

    internal static void RegisterTrace(this IValidationMessage msg, StackTrace trace)
    {
        table.AddOrUpdate(msg, trace);
    }

    public static bool TryGetStackTrace(this IValidationMessage msg, [NotNullWhen(true)] out StackTrace? trace)
    {
        return table.TryGetValue(msg, out trace);
    }

    public static StackTrace? GetStackTrace(this IValidationMessage msg)
    {
        TryGetStackTrace(msg, out StackTrace? trace);
        return trace;
    }
}

#endif
