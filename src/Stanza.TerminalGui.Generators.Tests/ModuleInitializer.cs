using System.Runtime.CompilerServices;
using VerifyTests;

namespace Stanza.TerminalGui.Generators.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
