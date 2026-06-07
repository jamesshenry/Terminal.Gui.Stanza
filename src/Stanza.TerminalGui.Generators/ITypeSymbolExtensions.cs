using Microsoft.CodeAnalysis;

namespace Stanza.TerminalGui.Generators;

public static class ITypeSymbolExtensions
{
    extension(ITypeSymbol typeSymbol)
    {
        public IPropertySymbol? FindPropertyInHierarchy(string? name)
        {
            var type = typeSymbol;
            while (type != null)
            {
                var prop = type.GetMembers(name!).OfType<IPropertySymbol>().FirstOrDefault();
                if (prop != null)
                    return prop;
                type = type.BaseType;
            }
            return null;
        }

        public bool IsSubtypeOf(string fullyQualifiedBaseName)
        {
            var current = typeSymbol.BaseType;
            while (current != null)
            {
                if (current.ToDisplayString() == fullyQualifiedBaseName)
                    return true;
                current = current.BaseType;
            }
            return false;
        }
    }
}
