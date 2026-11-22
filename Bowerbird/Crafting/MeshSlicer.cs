using System.Collections.Generic;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace Bowerbird.Crafting;

/// <summary>
/// Fast mesh/plane intersection for slicing the same mesh with many planes.
/// The mesh topology is flattened into arrays once; every slice is then a linear scan
/// over the faces without calls into native code. Quads that are planar within a tenth of
/// the tolerance are cut directly; warped quads are split into four triangles around their
/// centroid. Vertices lying exactly on the plane are treated as being below it
/// (symbolic perturbation), so the resulting polylines stay manifold even for tangent planes.
/// </summary>
public sealed class MeshSlicer
{
    private readonly Mesh _mesh;
    private readonly int _vertexCount;
    private readonly int _faceCount;
    private readonly double[] _x;
    private readonly double[] _y;
    private readonly double[] _z;
    private readonly int[] _faces; // 4 topology vertex indices per face, [2] == [3] for triangles
    private readonly bool[] _planar; // quad faces whose four vertices lie in one plane
    private readonly double _planarTolerance;

    public MeshSlicer(Mesh mesh, double tolerance)
    {
        _mesh = mesh;
        _planarTolerance = 0.1 * tolerance;

        // Weld mesh vertices at identical positions, like the mesh topology does, so that faces
        // sharing a location share their edges. This is done on the managed arrays instead of
        // querying Mesh.TopologyVertices, which is much slower for large meshes. ToPoint3dArray
        // keeps double precision when the mesh has it (e.g. meshes far from the origin); adding
        // 0.0 maps -0.0 to 0.0 so both get the same key.
        var vertices = mesh.Vertices.ToPoint3dArray();
        var weld = new Dictionary<Point3d, int>(vertices.Length);
        var vertexMap = new int[vertices.Length];
        var welded = new List<Point3d>(vertices.Length);

        for (var k = 0; k < vertices.Length; k++)
        {
            var p = vertices[k];
            var key = new Point3d(p.X + 0.0, p.Y + 0.0, p.Z + 0.0);

            if (!weld.TryGetValue(key, out var index))
            {
                index = welded.Count;
                weld.Add(key, index);
                welded.Add(p);
            }

            vertexMap[k] = index;
        }

        var meshFaces = mesh.Faces.ToIntArray(false);

        _vertexCount = welded.Count;
        _faceCount = meshFaces.Length / 4;

        // Slots [0, vertexCount) hold welded vertices, [vertexCount, vertexCount + faceCount) quad centroids.
        var count = _vertexCount + _faceCount;
        _x = new double[count];
        _y = new double[count];
        _z = new double[count];

        for (var i = 0; i < _vertexCount; i++)
        {
            var p = welded[i];
            _x[i] = p.X;
            _y[i] = p.Y;
            _z[i] = p.Z;
        }

        _faces = new int[meshFaces.Length];
        for (var i = 0; i < meshFaces.Length; i++)
            _faces[i] = vertexMap[meshFaces[i]];

        _planar = new bool[_faceCount];

        for (var f = 0; f < _faceCount; f++)
        {
            int a = _faces[4 * f], b = _faces[4 * f + 1], c = _faces[4 * f + 2], d = _faces[4 * f + 3];
            if (c == d)
                continue;

            _planar[f] = IsPlanar(a, b, c, d);

            var m = _vertexCount + f;
            _x[m] = (_x[a] + _x[b] + _x[c] + _x[d]) / 4.0;
            _y[m] = (_y[a] + _y[b] + _y[c] + _y[d]) / 4.0;
            _z[m] = (_z[a] + _z[b] + _z[c] + _z[d]) / 4.0;
        }
    }

    private bool IsPlanar(int a, int b, int c, int d)
    {
        var pa = new Point3d(_x[a], _y[a], _z[a]);
        var ac = new Point3d(_x[c], _y[c], _z[c]) - pa;
        var bd = new Point3d(_x[d], _y[d], _z[d]) - new Point3d(_x[b], _y[b], _z[b]);
        var normal = Vector3d.CrossProduct(ac, bd);
        var length = normal.Length;
        if (length == 0.0)
            return false;

        // Distance between the two diagonals. Cutting such a quad directly instead of as a
        // triangle fan moves the section by at most this distance.
        var ab = new Point3d(_x[b], _y[b], _z[b]) - pa;
        var distance = System.Math.Abs(ab * normal) / length;
        return distance <= _planarTolerance;
    }

    public Polyline[] Slice(Plane plane)
    {
        var state = new SliceState(this, plane);

        for (var f = 0; f < _faceCount; f++)
        {
            int a = _faces[4 * f], b = _faces[4 * f + 1], c = _faces[4 * f + 2], d = _faces[4 * f + 3];

            if (c == d)
            {
                state.Triangle(a, b, c);
                continue;
            }

            if (state.SameSide(a, b, c, d))
                continue;

            if (_planar[f] && state.PlanarQuad(a, b, c, d))
                continue;

            var m = _vertexCount + f;
            state.Triangle(a, b, m);
            state.Triangle(b, c, m);
            state.Triangle(c, d, m);
            state.Triangle(d, a, m);
        }

        // Non-manifold input (a crossing point with more than two neighbours) cannot be chained
        // unambiguously. Fall back to Rhino in that case.
        if (state.IsNonManifold)
            return Intersection.MeshPlane(_mesh, plane) ?? [];

        return state.Chain();
    }

    private sealed class SliceState
    {
        private readonly MeshSlicer _s;
        private readonly double[] _dist;
        private readonly bool[] _above;
        private readonly Dictionary<long, int> _lookup = new();
        private readonly List<Point3d> _points = new();
        private readonly List<int> _neighbours = new(); // 2 slots per point, -1 = empty
        private readonly List<bool> _forward = new(); // per slot: link follows the face orientation
        private readonly long _stride;

        public bool IsNonManifold { get; private set; }

        public SliceState(MeshSlicer slicer, Plane plane)
        {
            _s = slicer;

            var count = slicer._x.Length;
            _stride = count;
            _dist = new double[count];
            _above = new bool[count];

            var n = plane.ZAxis;
            var o = plane.Origin;

            for (var i = 0; i < count; i++)
            {
                var d = (slicer._x[i] - o.X) * n.X + (slicer._y[i] - o.Y) * n.Y + (slicer._z[i] - o.Z) * n.Z;
                _dist[i] = d;
                _above[i] = d > 0.0;
            }
        }

        public bool SameSide(int a, int b, int c, int d)
        {
            var s = _above[a];
            return _above[b] == s && _above[c] == s && _above[d] == s;
        }

        /// <summary>Cuts a planar quad directly. Returns false if it is crossed more than twice (non-convex).</summary>
        public bool PlanarQuad(int a, int b, int c, int d)
        {
            bool sa = _above[a], sb = _above[b], sc = _above[c], sd = _above[d];
            var crossings = (sa != sb ? 1 : 0) + (sb != sc ? 1 : 0) + (sc != sd ? 1 : 0) + (sd != sa ? 1 : 0);
            if (crossings != 2)
                return false;

            int enter = -1, leave = -1;
            Edge(a, b, ref enter, ref leave);
            Edge(b, c, ref enter, ref leave);
            Edge(c, d, ref enter, ref leave);
            Edge(d, a, ref enter, ref leave);
            Segment(leave, enter);
            return true;
        }

        public void Triangle(int a, int b, int c)
        {
            bool sa = _above[a], sb = _above[b], sc = _above[c];
            if (sa == sb && sb == sc)
                return;

            int enter = -1, leave = -1;
            Edge(a, b, ref enter, ref leave);
            Edge(b, c, ref enter, ref leave);
            Edge(c, a, ref enter, ref leave);
            Segment(leave, enter);
        }

        /// <summary>
        /// Classifies the face edge u→v (in face order) as entering (below→above) or leaving the
        /// half-space above the plane. Directing every segment from the leaving to the entering
        /// crossing orients the section loops consistently with the face orientation.
        /// </summary>
        private void Edge(int u, int v, ref int enter, ref int leave)
        {
            var su = _above[u];
            if (su == _above[v])
                return;

            if (su)
                leave = Crossing(u, v);
            else
                enter = Crossing(u, v);
        }

        private int Crossing(int a, int b)
        {
            // Crossings are keyed by edge, even if the plane passes exactly through a vertex.
            // Every crossed edge belongs to exactly two faces of a manifold mesh, so every point
            // gets exactly two neighbours. Zero-length segments are removed when chaining.
            if (a > b)
                (a, b) = (b, a);

            var t = _dist[a] / (_dist[a] - _dist[b]);
            return Point(a * _stride + b, a, b, t);
        }

        private int Point(long key, int a, int b, double t)
        {
            if (_lookup.TryGetValue(key, out var id))
                return id;

            var x = _s._x;
            var y = _s._y;
            var z = _s._z;

            id = _points.Count;
            _points.Add(new Point3d(x[a] + t * (x[b] - x[a]), y[a] + t * (y[b] - y[a]), z[a] + t * (z[b] - z[a])));
            _neighbours.Add(-1);
            _neighbours.Add(-1);
            _forward.Add(false);
            _forward.Add(false);
            _lookup[key] = id;
            return id;
        }

        private void Segment(int p, int q)
        {
            if (p == q)
                return;

            Link(p, q, true);
            Link(q, p, false);
        }

        private void Link(int p, int q, bool forward)
        {
            int slot;
            if (_neighbours[2 * p] < 0)
                slot = 2 * p;
            else if (_neighbours[2 * p + 1] < 0)
                slot = 2 * p + 1;
            else
            {
                IsNonManifold = true;
                return;
            }

            _neighbours[slot] = q;
            _forward[slot] = forward;
        }

        public Polyline[] Chain()
        {
            var used = new bool[_points.Count];
            var result = new List<Polyline>();

            // Open chains first (start at their ends), then the remaining closed loops.
            for (var i = 0; i < _points.Count; i++)
            {
                if (!used[i] && _neighbours[2 * i + 1] < 0)
                    Walk(i, used, result);
            }

            for (var i = 0; i < _points.Count; i++)
            {
                if (!used[i])
                    Walk(i, used, result);
            }

            return result.ToArray();
        }

        private void Walk(int start, bool[] used, List<Polyline> result)
        {
            var polyline = new Polyline();
            var previous = -1;
            var current = start;
            var orientation = 0;

            while (current >= 0 && !used[current])
            {
                used[current] = true;

                var point = _points[current];
                if (polyline.Count == 0 || polyline[polyline.Count - 1] != point)
                    polyline.Add(point);

                var slot = 2 * current;
                if (_neighbours[slot] == previous)
                    slot++;

                var next = _neighbours[slot];
                if (next >= 0)
                    orientation += _forward[slot] ? 1 : -1;

                previous = current;
                current = next;
            }

            if (current == start)
            {
                if (polyline.Count > 1 && polyline[polyline.Count - 1] == polyline[0])
                    polyline.RemoveAt(polyline.Count - 1);

                // A closed loop needs three distinct points; fewer means the plane only grazes the mesh.
                if (polyline.Count < 3)
                    return;

                polyline.Add(polyline[0]);
            }

            if (polyline.Count < 2)
                return;

            // The walk direction is arbitrary; follow the orientation of the majority of the faces.
            if (orientation < 0)
                polyline.Reverse();

            result.Add(polyline);
        }
    }
}
