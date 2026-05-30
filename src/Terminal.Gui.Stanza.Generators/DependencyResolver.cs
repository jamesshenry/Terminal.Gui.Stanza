using System.Collections.Generic;
using System.Linq;
using Terminal.Gui.Stanza.Abstractions.IR;

namespace Terminal.Gui.Stanza.Generators;

internal class DependencyResolver
{
    /// <summary>
    /// Topologically sort views by layout dependencies.
    /// If viewB.Y = Pos.Bottom(viewA), then viewA must be instantiated before viewB.
    /// </summary>
    public List<string> ResolveOrder(IEnumerable<LayoutConstraint> constraints, IEnumerable<string> allViewNames)
    {
        // Build adjacency list: viewName → List of views that depend on it
        var graph = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();

        var viewNamesList = allViewNames.ToList();
        foreach (var view in viewNamesList)
        {
            graph[view] = new List<string>();
            inDegree[view] = 0;
        }

        foreach (var constraint in constraints)
        {
            // Constraint: targetView depends on referencedView
            if (graph.ContainsKey(constraint.ReferencedView) && graph.ContainsKey(constraint.SourceView))
            {
                graph[constraint.ReferencedView].Add(constraint.SourceView);
                inDegree[constraint.SourceView]++;
            }
        }

        // Kahn's algorithm (topological sort)
        var queue = new Queue<string>();
        foreach (var view in viewNamesList)
        {
            if (inDegree[view] == 0)
                queue.Enqueue(view);
        }

        var result = new List<string>();
        while (queue.Count > 0)
        {
            var view = queue.Dequeue();
            result.Add(view);

            foreach (var dependent in graph[view])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                    queue.Enqueue(dependent);
            }
        }

        if (result.Count != viewNamesList.Count)
        {
            // Circular dependency detected
            // For MVP, we just return what we have or handle it elsewhere
        }

        return result;
    }
}
