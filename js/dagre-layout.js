window.KorenyDagre = {};
window.KorenyDagre.computeLayout = function(nodesJson, edgesJson, defaultWidth, defaultHeight) {
    var g = new dagre.graphlib.Graph();
    g.setGraph({
        rankdir: 'TB',
        nodesep: 60,
        ranksep: 80,
        edgesep: 20,
        ranker: 'tight-tree'
    });
    g.setDefaultEdgeLabel(function() { return {}; });

    var nodes = JSON.parse(nodesJson);
    nodes.forEach(function(n) {
        var w = n.width !== undefined && n.width !== null ? n.width : defaultWidth;
        var h = n.height !== undefined && n.height !== null ? n.height : defaultHeight;
        g.setNode(n.id, { label: n.id, width: w, height: h });
    });

    var edges = JSON.parse(edgesJson);
    edges.forEach(function(e) {
        g.setEdge(e.from, e.to);
    });

    dagre.layout(g);

    var result = {
        nodes: [],
        edges: []
    };

    g.nodes().forEach(function(id) {
        var n = g.node(id);
        var w = n.width;
        var h = n.height;
        result.nodes.push({ id: id, x: n.x - w / 2, y: n.y - h / 2 });
    });

    g.edges().forEach(function(e) {
        var edge = g.edge(e);
        result.edges.push({
            from: e.v,
            to: e.w,
            points: edge.points
        });
    });

    return JSON.stringify(result);
};
