using System.Runtime.CompilerServices;
using VerifyTests;

namespace Terminal.Gui.Stanza.Generators.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
