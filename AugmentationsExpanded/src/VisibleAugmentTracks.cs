using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded.VisibleAugments;


public sealed class TrackRoller : MonoBehaviour
{
    private sealed class Data
    {
        public Vector3 A0, A1, A2, B;            // mesh = A * src + B (rows of A)
        public Vector3 HipPos; public Quaternion HipRot;
        public int Count; public float LinksPerLoop;
        public Vector2[] Path; public float[] SideX;
        public Vector3[] Wheels;                 // (y, z, radius)
        public byte[] Kind, Side; public short[] Index;   // kind: 0 static, 1 tread link, 2 wheel
        public Vector3[] LocalPos, LocalNormal;
        // derived
        public Vector2[] Seg; public float[] SegLen, Cum; public float Total;
    }

    private static readonly Dictionary<string, Data> ByPiece = new Dictionary<string, Data>();
    private static readonly FieldInfo FrameIndexField = typeof(Creature3dAnimatorController).GetField("_currentFrameIndex", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo FramesField = typeof(Creature3dAnimatorController).GetField("_currentAnimationFrames", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static bool Has(string piece) => piece != null && ByPiece.ContainsKey(piece);

    internal static void Load(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            using (var r = new BinaryReader(File.OpenRead(path)))
            {
                if (new string(r.ReadChars(4)) != "AEVT") throw new InvalidDataException("bad header");
                int version = r.ReadInt32(); if (version != 1) throw new InvalidDataException("unsupported version " + version);
                int pieces = r.ReadInt32();
                for (int p = 0; p < pieces; p++)
                {
                    string name = r.ReadString();
                    var d = new Data();
                    d.A0 = V3(r); d.A1 = V3(r); d.A2 = V3(r); d.B = V3(r);
                    d.HipPos = V3(r); d.HipRot = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                    d.Count = r.ReadInt32(); d.LinksPerLoop = r.ReadSingle();
                    int np = r.ReadInt32(); d.Path = new Vector2[np];
                    for (int i = 0; i < np; i++) d.Path[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
                    d.SideX = new[] { r.ReadSingle(), r.ReadSingle() };
                    int nw = r.ReadInt32(); d.Wheels = new Vector3[nw];
                    for (int i = 0; i < nw; i++) d.Wheels[i] = V3(r);
                    int n = r.ReadInt32();
                    d.Kind = r.ReadBytes(n); d.Side = r.ReadBytes(n); d.Index = new short[n];
                    for (int i = 0; i < n; i++) d.Index[i] = r.ReadInt16();
                    d.LocalPos = new Vector3[n]; for (int i = 0; i < n; i++) d.LocalPos[i] = V3(r);
                    d.LocalNormal = new Vector3[n]; for (int i = 0; i < n; i++) d.LocalNormal[i] = V3(r);
                    d.Seg = new Vector2[np]; d.SegLen = new float[np]; d.Cum = new float[np + 1];
                    for (int i = 0; i < np; i++)
                    {
                        d.Seg[i] = d.Path[(i + 1) % np] - d.Path[i]; d.SegLen[i] = d.Seg[i].magnitude; d.Cum[i + 1] = d.Cum[i] + d.SegLen[i];
                    }
                    d.Total = d.Cum[np];
                    ByPiece[name] = d;
                }
            }
            Debug.Log("[" + ModMain.Id + "] Loaded rolling track data for " + ByPiece.Count + " piece(s).");
        }
        catch (Exception ex) { Debug.LogError("[" + ModMain.Id + "] aeva_tracks.bytes could not be read: " + ex.Message); }
    }

    private static Vector3 V3(BinaryReader r) => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

    private Data _d;
    private Mesh _mesh;
    private Vector3[] _v, _n;
    private Creature3dAnimatorController _anim;
    private Transform _root;
    private Vector3 _attachPos; private Quaternion _attachRot;
    private double _phase;
    private int _lastFrame = -1; private AnimationType _lastType = AnimationType.None;
    private double _shownPhase = double.NaN;

    internal void Init(string piece, Creature3dView view)
    {
        if (!ByPiece.TryGetValue(piece, out _d)) { enabled = false; return; }
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount != _d.Kind.Length) { enabled = false; return; }
        _mesh = mf.mesh;                                  // per-instance copy: other clones keep the shared template
        _v = _mesh.vertices; _n = _mesh.normals;
        _anim = view != null ? view.AnimatorController : null;
        _root = transform.parent != null ? transform.parent.parent : null;   // Hip bone -> the body mesh renderer
        _attachPos = transform.localPosition; _attachRot = transform.localRotation;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (_d == null) return;
        AnimationType type = _anim != null ? _anim.CurrentAnimationType : AnimationType.Idle;

        if (type == AnimationType.Walk && _anim != null && FrameIndexField != null && FramesField != null)
        {
            int frame = (int)FrameIndexField.GetValue(_anim);
            int len = (FramesField.GetValue(_anim) as Array)?.Length ?? 0;
            if (len > 0 && _lastType == AnimationType.Walk && frame != _lastFrame)
            {
                int steps = ((frame - _lastFrame) % len + len) % len;
                _phase += steps * (double)_d.LinksPerLoop / (_d.Count * len);
            }
            _lastFrame = frame;
        }
        _lastType = type;

        if (type != AnimationType.Death && _root != null)
        {
            transform.position = _root.TransformPoint(_d.HipPos);
            transform.rotation = _root.rotation * _d.HipRot;
        }
        else
        {
            transform.localPosition = _attachPos; transform.localRotation = _attachRot;
        }

        if (_phase != _shownPhase) { Pose((float)(_phase % 1000.0)); _shownPhase = _phase; }
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }

    private Vector3 Map(Vector3 s) => new Vector3(Vector3.Dot(_d.A0, s), Vector3.Dot(_d.A1, s), Vector3.Dot(_d.A2, s)) + _d.B;
    private Vector3 MapN(Vector3 s) => new Vector3(Vector3.Dot(_d.A0, s), Vector3.Dot(_d.A1, s), Vector3.Dot(_d.A2, s)).normalized;

    private void Pose(float phase)
    {
        Data d = _d;
        for (int i = 0; i < d.Kind.Length; i++)
        {
            Vector3 l = d.LocalPos[i], q = d.LocalNormal[i];
            float x = d.SideX[d.Side[i]];
            if (d.Kind[i] == 1)
            {
                float s = d.Index[i] / (float)d.Count + phase; s -= Mathf.Floor(s);
                float dd = s * d.Total; int k = 0;
                while (k < d.Path.Length - 1 && d.Cum[k + 1] <= dd) k++;
                Vector2 yz = d.Path[k] + d.Seg[k] * ((dd - d.Cum[k]) / d.SegLen[k]);
                Vector2 tt = d.Seg[k] / d.SegLen[k];
                var t = new Vector3(0, tt.x, tt.y); var nr = new Vector3(0, tt.y, -tt.x); var ax = new Vector3(1, 0, 0);
                _v[i] = Map(new Vector3(x, yz.x, yz.y) + ax * l.x + nr * l.y + t * l.z);
                _n[i] = MapN(ax * q.x + nr * q.y + t * q.z);
            }
            else if (d.Kind[i] == 2)
            {
                Vector3 w = d.Wheels[d.Index[i]];
                float a = phase * d.Total / w.z, c = Mathf.Cos(a), sn = Mathf.Sin(a);
                var rl = new Vector3(l.x, c * l.y - sn * l.z, sn * l.y + c * l.z);
                var rq = new Vector3(q.x, c * q.y - sn * q.z, sn * q.y + c * q.z);
                _v[i] = Map(new Vector3(x, w.x, w.y) + rl);
                _n[i] = MapN(rq);
            }
        }
        _mesh.vertices = _v; _mesh.normals = _n;
    }
}
