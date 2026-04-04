using GraphShape;
using GraphShape.Algorithms.Layout;
using Koreny.Models;
using QuikGraph;

namespace Koreny.Services;

/// <summary>Computes individual positions via GraphShape Sugiyama (layered) layout.</summary>
public sealed class FamilyGraphLayoutService
{
    public const double NODE_WIDTH = 160.0;
    public const double NODE_HEIGHT = 56.0;
    public const double H_GAP = 24.0;
    public const double V_GAP = 80.0;

    /// <summary>Maps each individual id to top-left (X, Y) of its node box.</summary>
    public Dictionary<string, (double X, double Y)> ComputeLayout(GedcomDocument document)
    {
        var result = new Dictionary<string, (double X, double Y)>(StringComparer.Ordinal);
        if (document.Individuals.Count == 0)
        {
            return result;
        }

        var graph = new BidirectionalGraph<string, Edge<string>>(allowParallelEdges: false);
        foreach (var ind in document.Individuals)
        {
            graph.AddVertex(ind.Id);
        }

        foreach (var fam in document.Families)
        {
            foreach (var cid in fam.ChildrenIds)
            {
                if (!string.IsNullOrEmpty(fam.HusbandId) && graph.ContainsVertex(fam.HusbandId) && graph.ContainsVertex(cid))
                {
                    graph.AddEdge(new Edge<string>(fam.HusbandId, cid));
                }

                if (!string.IsNullOrEmpty(fam.WifeId) && graph.ContainsVertex(fam.WifeId) && graph.ContainsVertex(cid))
                {
                    graph.AddEdge(new Edge<string>(fam.WifeId, cid));
                }
            }
        }

        var sizes = new Dictionary<string, Size>(StringComparer.Ordinal);
        foreach (var ind in document.Individuals)
        {
            sizes[ind.Id] = new Size(NODE_WIDTH, NODE_HEIGHT);
        }

        // Sugiyama expects at least one edge; otherwise the internal layer assignment is empty.
        if (graph.EdgeCount == 0)
        {
            var x = 0.0;
            foreach (var id in graph.Vertices.OrderBy(v => v, StringComparer.Ordinal))
            {
                result[id] = (x, 0);
                x += NODE_WIDTH + H_GAP;
            }

            return result;
        }

        var parameters = new SugiyamaLayoutParameters
        {
            Direction = LayoutDirection.TopToBottom,
            LayerGap = V_GAP,
            SliceGap = H_GAP,
        };

        // Sizes overload: layer assignment uses vertex bounds; graph-only ctor would default sizes and skew gaps.
        var algorithm = new SugiyamaLayoutAlgorithm<string, Edge<string>, BidirectionalGraph<string, Edge<string>>>(
            graph,
            sizes,
            parameters);

        algorithm.Compute();

        foreach (var kv in algorithm.VerticesPositions)
        {
            var p = kv.Value;
            result[kv.Key] = (p.X, p.Y);
        }

        return result;
    }
}
