using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace AugmentationsExpanded.VisibleAugments;

public static class ModMain
{
    public const string Id = "Augmentations_Expanded.VisibleAugments";
    private const string HarmonyId = "Augmentations_Expanded.VisibleAugments";
    private const BindingFlags Any = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static string _modPath;
    private static bool _loaded, _patchedVa;
    private static bool _failed, _reportedInactive;
    public static bool VisibleAugmentsEnabled { get; private set; } // True only when VA is active and this built-in bridge owns the integration
    private static readonly Dictionary<string, GameObject> Templates = new Dictionary<string, GameObject>();
    private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
    internal static readonly Dictionary<string, SkinData> Skins = new Dictionary<string, SkinData>();
    private static readonly List<GroupSpec> Specs = new List<GroupSpec>();
    private static readonly Dictionary<string, List<Candidate>> Dispatch = new Dictionary<string, List<Candidate>>(); // Dispatcher piece name -> candidate (augment ids, real piece name, texture) in priority order
    private static readonly Dictionary<string, List<string>> SuppressByPiece = new Dictionary<string, List<string>>(); // Piece name -> bones whose worn equipment is hidden while that piece is shown
    private static readonly HashSet<Creature3dView> SuppressedViews = new HashSet<Creature3dView>();
    private static readonly Dictionary<string, List<string>> HideByPiece = new Dictionary<string, List<string>>(); // Piece name -> body-mesh regions (e.g. "rightarm") whose triangles are dropped while that piece is shown
    private static readonly FieldInfo BodyMeshFilterField = typeof(Creature3dView).GetField("_meshFilter", Any);
    private static Creature3dView _restoring;
    private static CreatureData _ctx; // Creature VA is currently spawning pieces for
    private static readonly List<object> Injected = new List<object>();
    private static GameObject _holder;
    private static Mesh _emptyMesh;

    private static readonly FieldInfo EquippedItemsField = typeof(Creature3dView).GetField("_equippedItems", Any);
    private static readonly FieldInfo MaterialsField = typeof(Creature3dView).GetField("_materials", Any);
    private static readonly FieldInfo DynamicRenderersField = typeof(Creature3dView).GetField("_dynamicRenderers", Any);

    // Visible Augments internals (resolved by reflection)
    private static Type _vaPlugin, _groupType, _prefabType;
    private static IList _vaGroups;
    private static IDictionary _vaAugIdToGroup, _vaPrefabs, _vaGeneratedSkins;
    private static readonly Dictionary<int, long> SkinFlags = new Dictionary<int, long>();
    private static readonly VisibleAugmentClipping Clipping = new VisibleAugmentClipping();
    private static bool? _appliedClipping;
    private static CreatureUIIcon _previewIcon;
    private static Mercenary _previewMercenary;

    private static void Log(string m) => Debug.Log("[" + Id + "] " + m);
    private static void Err(string m) => Debug.LogError("[" + Id + "] " + m);

    private sealed class PrefabSpec
    {
        public string Name, TextureName;
        public bool HideLeggings, HideBoots, HideArmor, HideHelmet;
        public List<string> HideIfWeapon = new List<string>(), ShowOnlyIfWeapon = new List<string>();
    }
    private sealed class GroupSpec
    {
        public List<string> AugmentIds, UvMale, UvFemale, MasksMale, MasksFemale, Suppress, HideRegions;
        public string Texture, Slot;
        public bool SuppressHair, SuppressHelmet, HideLeggings, HideBoots, HideArmor, HideHelmet;
        public List<PrefabSpec> Prefabs = new List<PrefabSpec>();
    }
    private sealed class Candidate { public HashSet<string> AugmentIds; public string Piece; public Texture Texture; }

    [Hook(ModHookType.AfterConfigsLoaded)]
    public static void OnConfigsLoaded(IModContext context)
    {
        Ensure(context, "configs loaded");
    }

    // If Visible Augments initialised after us, (re)inject at later points so mod load order doesn't break anything
    [Hook(ModHookType.AfterBootstrap)] public static void OnBootstrap(IModContext context) => Ensure(context, "bootstrap");
    [Hook(ModHookType.MainMenuStarted)] public static void OnMainMenu(IModContext context) => Ensure(context, "main menu");
    [Hook(ModHookType.AfterSaveLoaded)] public static void OnSaveLoaded(IModContext context) => Ensure(context, "save loaded");

    internal static bool ShouldEnable(IEnumerable<string> activeMods)
    {
        if (activeMods == null) return false;
        var names = new HashSet<string>(activeMods, StringComparer.Ordinal);
        return names.Contains("VisibleAugments") && !names.Contains("AEVisibleAugments");
    }

    private static void Ensure(IModContext context, string when)
    {
        try
        {
            if (_failed) return;
            var mods = context?.State?.Get<UserMods>();
            VisibleAugmentsEnabled = ShouldEnable(mods?.Values.Select(m => m.UniqueModName));
            if (!VisibleAugmentsEnabled)
            {
                if (!_reportedInactive)
                {
                    Log("Built-in visuals inactive: Visible Augments is disabled/absent, or the standalone AEVisibleAugments bridge is active.");
                    _reportedInactive = true;
                }
                return;
            }
            if (string.IsNullOrEmpty(context.ModContentPath)) return;
            _modPath = Path.Combine(context.ModContentPath, "Assets", "VisibleAugments");
            if (!ResolveVa()) return;
            TryHookVisibleAugments();
            Activate(when);
        }
        catch (Exception ex) { DisableAfterFailure(ex); }
    }

    private static void Activate(string when)
    {
        if (!VisibleAugmentsEnabled || _failed || !_patchedVa || _vaGroups == null || _vaGroups.Count == 0) return;
        LoadOwnContent();
        if (Injected.Count == 0 || !Injected.All(g => _vaGroups.Contains(g))) Inject(when);
        RefreshClippingPolicy();
    }

    private static void DisableAfterFailure(Exception ex)
    {
        _failed = true;
        VisibleAugmentsEnabled = false;
        try { new Harmony(HarmonyId).UnpatchAll(HarmonyId); }
        catch (Exception cleanup) { Err("Could not fully remove optional patches: " + cleanup.Message); }
        _patchedVa = false;
        try { Clipping.Restore(); }
        catch (Exception cleanup) { Err("Could not restore VA clothing rules: " + cleanup.Message); }
        try { RemoveInjectedGroups(); }
        catch (Exception cleanup) { Err("Could not fully remove optional groups: " + cleanup.Message); }
        Err("Optional visuals disabled for this session; AE gameplay remains active: " + ex);
    }

    private static void LoadOwnContent()
    {
        if (_loaded) return;
        JSONNode root = JSON.Parse(File.ReadAllText(Path.Combine(_modPath, "augments.json")));
        if (root == null || !root.IsArray) throw new InvalidDataException("augments.json is not a JSON array");

        _holder = new GameObject(Id + "_Templates");
        _holder.SetActive(false); // Templates never render cause clones are activated under the creature view
        UnityEngine.Object.DontDestroyOnLoad(_holder);
        _emptyMesh = new Mesh { name = "mesh.empty" };

        using (var r = new BinaryReader(File.OpenRead(Path.Combine(_modPath, "Models", "aeva_meshes.bytes"))))
        {
            if (new string(r.ReadChars(4)) != "AEVA") throw new InvalidDataException("aeva_meshes.bytes: bad header");
            int version = r.ReadInt32();
            if (version < 1 || version > 3) throw new InvalidDataException("aeva_meshes.bytes: unsupported version " + version);
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string name = r.ReadString(), bone = r.ReadString();
                int n = r.ReadInt32();
                Vector3[] v = ReadV3(r, n), nr = ReadV3(r, n);
                var uv = new Vector2[n];
                for (int k = 0; k < n; k++) uv[k] = new Vector2(r.ReadSingle(), r.ReadSingle());
                int ti = r.ReadInt32();
                var tri = new int[ti];
                for (int k = 0; k < ti; k++) tri[k] = r.ReadInt32();

                if (version == 2)
                {
                    string secondary = r.ReadString(); // v2: optional 2-bone blend (attach bone + secondary)
                    if (!string.IsNullOrEmpty(secondary))
                    {
                        Vector3[] a = ReadV3(r, n), an = ReadV3(r, n); var t = new float[n];
                        for (int k = 0; k < n; k++) t[k] = r.ReadSingle();
                        var w0 = t.Select(x => 1 - x).ToArray();
                        Skins[name] = new SkinData { Bones = new[] { bone, secondary }, Idx = new[] { new int[n], Enumerable.Repeat(1, n).ToArray() },
                                                     W = new[] { w0, t }, A = new[] { v, a }, An = new[] { nr, an } };
                    }
                }
                else if (version >= 3)
                {
                    int K = r.ReadInt32(); // v3: up to K bone influences per vertex
                    if (K > 0)
                    {
                        int nb = r.ReadInt32();
                        var sd = new SkinData { Bones = new string[nb], Idx = new int[K][], W = new float[K][], A = new Vector3[K][], An = new Vector3[K][] };
                        for (int b = 0; b < nb; b++) sd.Bones[b] = r.ReadString();
                        for (int k = 0; k < K; k++)
                        {
                            sd.Idx[k] = new int[n]; for (int q = 0; q < n; q++) sd.Idx[k][q] = r.ReadInt32();
                            sd.W[k] = new float[n]; for (int q = 0; q < n; q++) sd.W[k][q] = r.ReadSingle();
                            sd.A[k] = ReadV3(r, n); sd.An[k] = ReadV3(r, n);
                        }
                        Skins[name] = sd;
                    }
                }
                Templates[name] = MakeTemplate(name, bone, MakeMesh(name, v, nr, uv, tri));
            }
        }

        foreach (JSONNode g in root.AsArray) Specs.Add(ParseGroup(g));
        foreach (GroupSpec g in Specs)
        {
            if (!string.IsNullOrEmpty(g.Texture)) LoadTexture(g.Texture);
            foreach (PrefabSpec p in g.Prefabs) if (!string.IsNullOrEmpty(p.TextureName)) LoadTexture(p.TextureName);
        }
        string hidePath = Path.Combine(_modPath, "aeva_hide.json");
        if (File.Exists(hidePath)) MeshHider.Load(JSON.Parse(File.ReadAllText(hidePath)));
        _loaded = true;
        Log($"Loaded {Templates.Count} piece templates ({Skins.Count} skinned), {Textures.Count} textures, {Specs.Count} augment entries.");
    }

    private static Vector3[] ReadV3(BinaryReader r, int n)
    {
        var a = new Vector3[n];
        for (int k = 0; k < n; k++) a[k] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        return a;
    }

    private static Mesh MakeMesh(string name, Vector3[] v, Vector3[] nr, Vector2[] uv, int[] tri)
    {
        var mesh = new Mesh { name = "mesh." + name };
        if (v.Length > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = v; mesh.normals = nr; mesh.uv = uv; mesh.triangles = tri;
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
        return mesh;
    }

    private static GameObject MakeTemplate(string name, string bone, Mesh mesh)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_holder.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        var ib = go.AddComponent<ItemBone>();
        ib.TargetBoneId = bone;
        ib.Scale = Vector3.one * 0.1f;
        return go;
    }

    private static void LoadTexture(string name)
    {
        if (Textures.ContainsKey(name)) return;
        string path = Path.Combine(_modPath, "Textures", name + ".png");
        if (!File.Exists(path)) { Err("Texture not found: " + path); return; }
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = name };
        t.LoadImage(File.ReadAllBytes(path), true);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Point;
        UnityEngine.Object.DontDestroyOnLoad(t);
        Textures[name] = t;
    }

    private static List<string> Strings(JSONNode a)
    {
        var l = new List<string>();
        if (a == null) return l;
        if (a.IsArray) { foreach (JSONNode x in a.AsArray) if (!string.IsNullOrEmpty(x.Value)) l.Add(x.Value); }
        else if (!string.IsNullOrEmpty(a.Value)) l.Add(a.Value);
        return l;
    }

    private static GroupSpec ParseGroup(JSONNode n) // Mask stuff
    {
        // VA resolves relative mask names against its own folder, so give it our full mask paths.
        List<string> Masks(JSONNode a) => Strings(a).Select(m => Path.IsPathRooted(m) ? m : Path.Combine(_modPath, "Masks", m)).ToList();
        var g = new GroupSpec
        {
            AugmentIds = Strings(n["augmentIds"]), Texture = n["texture"].Value, Slot = n["slot"].Value,
            UvMale = Strings(n["uvRegionsMale"]), UvFemale = Strings(n["uvRegionsFemale"]),
            MasksMale = Masks(n["masksMale"]), MasksFemale = Masks(n["masksFemale"]), Suppress = Strings(n["suppressEquipmentBones"]),
            HideRegions = Strings(n["hideMeshRegions"]),
            SuppressHair = n["suppressHair"].AsBool, SuppressHelmet = n["suppressHelmet"].AsBool,
            HideLeggings = n["hideIfLeggingsEquipped"].AsBool, HideBoots = n["hideIfBootsEquipped"].AsBool,
            HideArmor = n["hideIfArmorEquipped"].AsBool, HideHelmet = n["hideIfHelmetEquipped"].AsBool,
        };
        // Transparent skin pixels still leave the original head in the UI pixelizer's geometry pass.
        // Remove that geometry as well, so only the replacement head contributes to the silhouette.
        if (g.Slot == "head" && !g.HideRegions.Contains("head")) g.HideRegions.Add("head");
        foreach (JSONNode p in n["prefabs"].AsArray)
        {
            if (p.IsString) { g.Prefabs.Add(new PrefabSpec { Name = p.Value }); continue; }
            g.Prefabs.Add(new PrefabSpec
            {
                Name = p["name"].Value, TextureName = p["texture"].Value,
                HideLeggings = p["hideIfLeggingsEquipped"].AsBool, HideBoots = p["hideIfBootsEquipped"].AsBool,
                HideArmor = p["hideIfArmorEquipped"].AsBool, HideHelmet = p["hideIfHelmetEquipped"].AsBool,
                HideIfWeapon = Strings(p["hideIfWeaponActive"]), ShowOnlyIfWeapon = Strings(p["showOnlyIfWeaponActive"]),
            });
        }
        return g;
    }

    private static bool ResolveVa()
    {
        if (_vaPlugin != null) return true;
        Assembly asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "VisibleAugments");
        if (asm == null) return false;
        _vaPlugin = asm.GetType("Plugin");
        if (_vaPlugin == null) { Err("Visible Augments found but its Plugin type is missing; it may have changed."); return false; }
        _groupType = _vaPlugin.GetNestedType("AugmentGroupDef", Any);
        _prefabType = _vaPlugin.GetNestedType("PrefabDef", Any);
        _vaGroups = _vaPlugin.GetField("_groups", Any)?.GetValue(null) as IList;
        _vaAugIdToGroup = _vaPlugin.GetField("_augIdToGroup", Any)?.GetValue(null) as IDictionary;
        _vaPrefabs = _vaPlugin.GetField("_prefabs", Any)?.GetValue(null) as IDictionary;
        if (_groupType == null || _prefabType == null || _vaGroups == null || _vaAugIdToGroup == null || _vaPrefabs == null)
        {
            _vaPlugin = null;
            throw new MissingMemberException("Visible Augments' internal tables are not compatible.");
        }
        ValidateVaContract();
        return true;
    }

    private static void ValidateVaContract()
    {
        void RequireFields(Type type, string fields)
        {
            foreach (string name in fields.Split(' '))
                if (type.GetField(name, Any) == null) throw new MissingFieldException(type.FullName, name);
        }
        RequireFields(_groupType, "Flag AugmentIds TextureName Texture UvRegionsMale UvRegionsFemale MasksMale MasksFemale SuppressHair SuppressHelmet HideIfLeggingsEquipped HideIfBootsEquipped HideIfArmorEquipped HideIfHelmetEquipped Prefabs");
        RequireFields(_prefabType, "Name TextureName Texture HideIfLeggingsEquipped HideIfBootsEquipped HideIfArmorEquipped HideIfHelmetEquipped HideIfWeaponActive ShowOnlyIfWeaponActive");
        RequireFields(_vaPlugin, "_generatedSkins");
        foreach (string name in new[] { "OnConfigsLoaded", "SpawnAugmentForView", "SpawnAugmentObjects", "ComputeEquipmentHash", "DestroyAugmentObjects", "UpdateSkinTexture" })
            if (_vaPlugin.GetMethod(name, Any) == null) throw new MissingMethodException(_vaPlugin.FullName, name);
        foreach (string name in new[] { "CreatureUIIconPatch", "CorpseDecoyUpdateVisualsPatch", "CorpseStorageRefreshVisualPatch" })
            if (_vaPlugin.GetNestedType(name, Any)?.GetMethod("Postfix", Any) == null) throw new MissingMethodException(name, "Postfix");
        RequireFields(typeof(Creature3dView), "_meshFilter _equippedItems _materials _dynamicRenderers");
        if (AccessTools.Method(typeof(Creature3dView), "RefreshItemsRender") == null) throw new MissingMethodException("Creature3dView", "RefreshItemsRender");
    }

    private static void TryHookVisibleAugments()
    {
        if (_patchedVa || !ResolveVa()) return;
        MethodInfo target = _vaPlugin.GetMethod("OnConfigsLoaded", Any);
        if (target == null) { Err("Visible Augments' OnConfigsLoaded not found."); return; }
        var harmony = new Harmony(HarmonyId);
        harmony.Patch(target, postfix: new HarmonyMethod(typeof(ModMain), nameof(AfterVaLoaded)));

        // Pieces get their set (dispatchers), skinning and equipment hiding as VA spawns them
        MethodInfo spawn = _vaPlugin.GetMethod("SpawnAugmentForView", Any);
        if (spawn != null) harmony.Patch(spawn, postfix: new HarmonyMethod(typeof(ModMain), nameof(AfterSpawn)));
        else Err("SpawnAugmentForView not found: set pieces and skinning will not work.");

        var ctxPrefix = new HarmonyMethod(typeof(ModMain), nameof(CtxFromArgs));
        var ctxClear = new HarmonyMethod(typeof(ModMain), nameof(CtxClear));
        void PatchCtx(MethodInfo m) { if (m != null) harmony.Patch(m, prefix: ctxPrefix, finalizer: ctxClear); else Log("A creature-context hook was not found; dispatch falls back to the first set."); }
        PatchCtx(_vaPlugin.GetMethod("SpawnAugmentObjects", Any));
        PatchCtx(_vaPlugin.GetNestedType("CreatureUIIconPatch", Any)?.GetMethod("Postfix", Any));
        PatchCtx(_vaPlugin.GetNestedType("CorpseDecoyUpdateVisualsPatch", Any)?.GetMethod("Postfix", Any));
        PatchCtx(_vaPlugin.GetNestedType("CorpseStorageRefreshVisualPatch", Any)?.GetMethod("Postfix", Any));
        harmony.Patch(_vaPlugin.GetNestedType("CreatureUIIconPatch", Any).GetMethod("Postfix", Any),
            postfix: new HarmonyMethod(typeof(ModMain), nameof(AfterPreview)));

        MethodInfo eqHash = _vaPlugin.GetMethod("ComputeEquipmentHash", Any);
        if (eqHash != null) harmony.Patch(eqHash, postfix: new HarmonyMethod(typeof(ModMain), nameof(AfterEquipmentHash)));

        harmony.Patch(AccessTools.Method(typeof(Creature3dView), "RefreshItemsRender"), postfix: new HarmonyMethod(typeof(ModMain), nameof(AfterRefreshItems)));
        MethodInfo destroy = _vaPlugin.GetMethod("DestroyAugmentObjects", Any);
        if (destroy != null) harmony.Patch(destroy, postfix: new HarmonyMethod(typeof(ModMain), nameof(AfterDestroyAugments)));

        // VA builds a creature's masked skin once and keeps it, so adding a second augment mid-session leaves the new body part unmasked. Drop the cached skin whenever the visible set changes
        MethodInfo updateSkin = _vaPlugin.GetMethod("UpdateSkinTexture", Any);
        _vaGeneratedSkins = _vaPlugin.GetField("_generatedSkins", Any)?.GetValue(null) as IDictionary;
        if (updateSkin != null && _vaGeneratedSkins != null)
            harmony.Patch(updateSkin, prefix: new HarmonyMethod(typeof(ModMain), nameof(BeforeUpdateSkin)));
        else Log("Skin refresh fix not applied (Visible Augments internals changed).");
        _patchedVa = true;
    }

    public static void CtxFromArgs(object[] __args)
    {
        _ctx = null;
        try
        {
            foreach (object a in __args)
            {
                switch (a)
                {
                    case Creature c: _ctx = c.CreatureData; return;
                    case Mercenary m: _ctx = m.CreatureData; return;
                    case CorpseStorage s: _ctx = s.CreatureData; return;
                    case CorpseDecoy d: _ctx = AccessTools.Field(typeof(CorpseDecoy), "_creatureData")?.GetValue(d) as CreatureData; return;
                }
            }
        }
        catch (Exception ex) { Err("Creature context failed: " + ex.Message); }
    }

    public static Exception CtxClear(Exception __exception) { _ctx = null; return __exception; }

    public static void AfterPreview(CreatureUIIcon __0, Mercenary __1)
    {
        _previewIcon = __0;
        _previewMercenary = __1;
    }

    internal static void RefreshClippingPolicy()
    {
        if (!VisibleAugmentsEnabled || !_loaded || _vaGroups == null || Injected.Count == 0) return;
        bool allowClipping = AugmentationsExpandedMod._vaClipping;
        if (_appliedClipping == allowClipping) return;
        try
        {
            Clipping.Apply(_vaGroups, PieceBone, allowClipping);
            _appliedClipping = allowClipping;
            // VA compares these hashes each update. Clearing them also refreshes pieces when
            // only a per-prefab clothing rule changes and the group's skin-mask flag is unchanged.
            (_vaPlugin.GetField("_equipmentHash", Any)?.GetValue(null) as IDictionary)?.Clear();
            if (_previewIcon != null && _previewIcon.isActiveAndEnabled && _previewMercenary != null)
                _previewIcon.Initialize(_previewMercenary);
        }
        catch (Exception ex) { Err("Could not refresh VA clothing visibility: " + ex); }
    }

    public static void AfterEquipmentHash(Creature creature, ref int __result)
    {
        try
        {
            var map = creature?.CreatureData?.AugmentationMap;
            if (map == null) return;
            int h = 17;
            foreach (var kv in map.OrderBy(k => k.Key, StringComparer.Ordinal)) h = h * 31 + (kv.Value?.GetHashCode() ?? 0);
            __result ^= h * 486187739;
        }
        catch (Exception ex) { Err("Augment change check failed: " + ex.Message); }
    }

    public static void BeforeUpdateSkin(Creature creature, long flags)
    {
        try
        {
            int id = creature.CreatureData.UniqueId;
            if (SkinFlags.TryGetValue(id, out long last) && last != flags && _vaGeneratedSkins != null && _vaGeneratedSkins.Contains(id))
            {
                if (_vaGeneratedSkins[id] is Texture2D old && old != null) UnityEngine.Object.Destroy(old);
                _vaGeneratedSkins.Remove(id);
            }
            SkinFlags[id] = flags;
        }
        catch (Exception ex) { Err("Skin refresh failed: " + ex.Message); }
    }

    public static void AfterSpawn(Creature3dView view, GameObject prefab, GameObject __result)
    {
        try
        {
            if (__result == null || prefab == null) return;
            string piece = prefab.name;

            if (Dispatch.TryGetValue(piece, out var candidates))
            {
                Candidate pick = Choose(candidates);
                var mf = __result.GetComponent<MeshFilter>();
                if (pick == null || pick.Piece == null || !Templates.TryGetValue(pick.Piece, out var src))
                {
                    if (mf != null) mf.sharedMesh = _emptyMesh;      // this set has nothing on this bone
                    return;
                }
                if (mf != null) mf.sharedMesh = src.GetComponent<MeshFilter>().sharedMesh;
                var rend = __result.GetComponentInChildren<Renderer>();
                if (rend != null && rend.sharedMaterial != null && pick.Texture != null) rend.sharedMaterial.SetTexture("_MainTex", pick.Texture);
                piece = pick.Piece;
            }

            if (SuppressByPiece.TryGetValue(prefab.name, out var bones)) SuppressEquipment(view ?? __result.GetComponentInParent<Creature3dView>(), bones);
            if (HideByPiece.TryGetValue(prefab.name, out var regions)) HideBody(view ?? __result.GetComponentInParent<Creature3dView>(), regions);

            if (Skins.TryGetValue(piece, out SkinData skin))
            {
                Transform attach = __result.transform.parent;           // the bone the piece was attached to
                Transform root = attach != null ? attach.parent : null;
                if (root == null) return;
                var tf = new Transform[skin.Bones.Length];
                for (int b = 0; b < tf.Length; b++) { tf[b] = root.Find(skin.Bones[b]); if (tf[b] == null) return; }   // stays rigid
                __result.AddComponent<SkinnedPiece>().Init(skin, tf);
            }
        }
        catch (Exception ex) { Err("Piece setup failed: " + ex.Message); }
    }

    private static Candidate Choose(List<Candidate> candidates)
    {
        if (_ctx?.AugmentationMap != null)
        {
            foreach (string aug in _ctx.AugmentationMap.Values)
                foreach (Candidate c in candidates)
                    if (c.AugmentIds.Contains(aug)) return c;
        }
        return null;
    }

    public static void AfterRefreshItems(Creature3dView __instance)
    {
        try
        {
            if (__instance == null || SuppressByPiece.Count == 0 || __instance == _restoring) return;
            foreach (Transform child in __instance.GetComponentsInChildren<Transform>(true))
            {
                string n = child.name;
                string piece = n.StartsWith("AugUI_") ? n.Substring(6) : n.StartsWith("Aug_") ? n.Substring(4) : null;
                if (piece == null || !SuppressByPiece.TryGetValue(piece, out var bones)) continue;
                var mf = child.GetComponent<MeshFilter>();
                if (mf != null && (mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0)) continue;   // empty dispatcher
                SuppressEquipment(__instance, bones);
            }
        }
        catch (Exception ex) { Err("Equipment suppression failed: " + ex.Message); }
    }

    public static void AfterDestroyAugments(Creature3dView view)
    {
        try
        {
            if (view != null) { var hider = view.GetComponent<MeshHider>(); if (hider != null) hider.Clear(); }
            if (view != null && SuppressedViews.Remove(view))
            {
                _restoring = view;
                try { view.RefreshItemsRender(); } finally { _restoring = null; }
            }
        }
        catch (Exception ex) { Err("Equipment restore failed: " + ex.Message); }
    }

    private static void HideBody(Creature3dView view, List<string> regions)
    {
        if (view == null || !(BodyMeshFilterField?.GetValue(view) is MeshFilter mf) || mf == null) return;
        var hider = view.GetComponent<MeshHider>() ?? view.gameObject.AddComponent<MeshHider>();
        hider.Add(mf, regions);
    }

    private static void SuppressEquipment(Creature3dView view, List<string> bones)
    {
        if (view == null || !(EquippedItemsField?.GetValue(view) is List<GameObject> items)) return;
        var materials = MaterialsField?.GetValue(view) as List<Material>;
        var renderers = DynamicRenderersField?.GetValue(view) as List<MeshRenderer>;
        bool any = false;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            GameObject item = items[i];
            if (item == null || item.name.StartsWith("Aug")) continue;
            // Body equipment stays visible in both modes: it either covers the augment or
            // overlaps it. Only the established replacement-head/headgear behavior suppresses gear.
            if (!item.GetComponentsInChildren<ItemBone>(true).Any(b => VisibleAugmentClipping.IsHead(b.TargetBoneId) && bones.Contains(b.TargetBoneId))) continue;
            foreach (Renderer r in item.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterial != null) { materials?.Remove(r.sharedMaterial); UnityEngine.Object.Destroy(r.sharedMaterial); }
            foreach (MeshRenderer r in item.GetComponentsInChildren<MeshRenderer>(true)) renderers?.Remove(r);
            items.RemoveAt(i);
            UnityEngine.Object.Destroy(item);
            any = true;
        }
        if (any) SuppressedViews.Add(view);
    }

    public static void AfterVaLoaded()
    {
        if (!VisibleAugmentsEnabled || _failed) return;
        try { _vaGroups = null; _vaPlugin = null; if (ResolveVa()) Activate("Visible Augments loaded"); }
        catch (Exception ex) { DisableAfterFailure(ex); }
    }

    private static void Inject(string when)
    {
        if (!VisibleAugmentsEnabled || !_loaded) return;
        if (!ResolveVa()) { Log("Visible Augments not loaded yet (" + when + "); will retry."); return; }

        RemoveInjectedGroups();
        _appliedClipping = null;
        if (_vaGroups.Count == 0) { Log("Visible Augments has no groups yet (" + when + "); will retry."); return; }

        // Allocate actual free bits, including bit 63 (negative when stored in VA's signed long)
        ulong usedFlags = 0;
        foreach (object g in _vaGroups) usedFlags |= unchecked((ulong)(long)_groupType.GetField("Flag").GetValue(g));

        foreach (var kv in Templates) _vaPrefabs[kv.Key] = kv.Value;
        SuppressByPiece.Clear();
        HideByPiece.Clear();
        Dispatch.Clear();

        var units = new List<List<GroupSpec>>();
        foreach (var bySlot in Specs.Where(s => !string.IsNullOrEmpty(s.Slot)).GroupBy(s => s.Slot)) units.Add(bySlot.ToList());
        foreach (GroupSpec s in Specs.Where(s => string.IsNullOrEmpty(s.Slot))) units.Add(new List<GroupSpec> { s });

        var flags = AllocateFlags(usedFlags, units.Count);
        if (flags == null) throw new InvalidOperationException("Visible Augments' 64-group limit has no room for all AE groups.");
        int added = 0;
        foreach (List<GroupSpec> unit in units)
        {
            object group = unit.Count == 1 && string.IsNullOrEmpty(unit[0].Slot) ? BuildGroup(unit[0], flags[added]) : BuildSlotGroup(unit, flags[added]);
            _vaGroups.Add(group);
            Injected.Add(group);
            foreach (string id in (List<string>)_groupType.GetField("AugmentIds").GetValue(group)) _vaAugIdToGroup[id] = group;
            added++;
        }
        Log($"Registered {added} groups ({Specs.Count} augments) with Visible Augments ({when}).");
    }

    internal static List<long> AllocateFlags(ulong usedFlags, int count)
    {
        var flags = new List<long>();
        for (int bit = 0; bit < 64 && flags.Count < count; bit++)
            if ((usedFlags & (1UL << bit)) == 0) flags.Add(unchecked((long)(1UL << bit)));
        return flags.Count == count ? flags : null;
    }

    private static void RemoveInjectedGroups()
    {
        foreach (object group in Injected)
        {
            _vaGroups?.Remove(group);
            var ids = (List<string>)_groupType.GetField("AugmentIds").GetValue(group);
            foreach (string id in ids)
                if (_vaAugIdToGroup != null && ReferenceEquals(_vaAugIdToGroup[id], group)) _vaAugIdToGroup.Remove(id);
        }
        Injected.Clear();
    }

    private static string PieceBone(string name)
    {
        GameObject go = Templates.TryGetValue(name, out var t) ? t : _vaPrefabs != null && _vaPrefabs.Contains(name) ? _vaPrefabs[name] as GameObject : null;
        return go != null ? go.GetComponentInChildren<ItemBone>(true)?.TargetBoneId : null;
    }

    private static Texture Tex(string name, Texture fallback) => !string.IsNullOrEmpty(name) && Textures.TryGetValue(name, out var t) ? t : fallback;

    // A piece hides worn equipment only on the bone it is attached to, and only if its augment lists that bone.
    private static void RegisterSuppression(string piece, string bone, List<string> suppress)
    {
        if (!VisibleAugmentClipping.IsHead(bone) || !suppress.Contains(bone)) return;
        if (!SuppressByPiece.TryGetValue(piece, out var list)) SuppressByPiece[piece] = list = new List<string>();
        if (!list.Contains(bone)) list.Add(bone);
    }

    private static object NewGroup(GroupSpec s, long flag, List<string> ids)
    {
        object g = Activator.CreateInstance(_groupType, true);
        void Set(string f, object v) => _groupType.GetField(f).SetValue(g, v);
        Set("Flag", flag);
        Set("AugmentIds", ids);
        Set("TextureName", s.Texture);
        Set("Texture", Tex(s.Texture, null));
        Set("UvRegionsMale", new List<string>(s.UvMale));
        Set("UvRegionsFemale", new List<string>(s.UvFemale));
        Set("MasksMale", new List<string>(s.MasksMale));
        Set("MasksFemale", new List<string>(s.MasksFemale));
        Set("SuppressHair", s.SuppressHair);
        Set("SuppressHelmet", s.SuppressHelmet);
        Set("HideIfLeggingsEquipped", s.HideLeggings);
        Set("HideIfBootsEquipped", s.HideBoots);
        Set("HideIfArmorEquipped", s.HideArmor);
        Set("HideIfHelmetEquipped", s.HideHelmet);
        return g;
    }

    private static object NewPrefab(PrefabSpec p, string name, Texture fallback)
    {
        object d = Activator.CreateInstance(_prefabType, true);
        void P(string f, object v) => _prefabType.GetField(f).SetValue(d, v);
        P("Name", name);
        P("TextureName", p?.TextureName);
        P("Texture", Tex(p?.TextureName, fallback));
        P("HideIfLeggingsEquipped", p?.HideLeggings ?? false);
        P("HideIfBootsEquipped", p?.HideBoots ?? false);
        P("HideIfArmorEquipped", p?.HideArmor ?? false);
        P("HideIfHelmetEquipped", p?.HideHelmet ?? false);
        P("HideIfWeaponActive", new List<string>(p?.HideIfWeapon ?? new List<string>()));
        P("ShowOnlyIfWeaponActive", new List<string>(p?.ShowOnlyIfWeapon ?? new List<string>()));
        return d;
    }

    private static object BuildGroup(GroupSpec s, long flag)
    {
        object g = NewGroup(s, flag, new List<string>(s.AugmentIds));
        Texture groupTex = Tex(s.Texture, null);
        var prefabs = (IList)_groupType.GetField("Prefabs").GetValue(g);
        foreach (PrefabSpec p in s.Prefabs)
        {
            if (!Templates.ContainsKey(p.Name) && !_vaPrefabs.Contains(p.Name)) Err("Unknown piece '" + p.Name + "' in augments.json");
            prefabs.Add(NewPrefab(p, p.Name, groupTex));
            RegisterSuppression(p.Name, PieceBone(p.Name), s.Suppress);
            if (s.HideRegions.Count > 0) HideByPiece[p.Name] = s.HideRegions;
        }
        return g;
    }

    private static object BuildSlotGroup(List<GroupSpec> members, long flag)
    {
        GroupSpec first = members[0];
        string slot = first.Slot;
        object g = NewGroup(first, flag, members.SelectMany(m => m.AugmentIds).Distinct().ToList());
        var prefabs = (IList)_groupType.GetField("Prefabs").GetValue(g);

        // Dispatch key = piece name without the "AEVA_<Set>_" prefix, e.g. "forearm_LeftForeArm"
        string Key(string piece) { var parts = piece.Split(new[] { '_' }, 3); return parts.Length == 3 ? parts[2] : piece; }
        var keys = new List<string>();
        foreach (GroupSpec m in members) foreach (PrefabSpec p in m.Prefabs) if (!keys.Contains(Key(p.Name))) keys.Add(Key(p.Name));

        foreach (string key in keys)
        {
            string dname = "AEVA_slot_" + slot + "_" + key;
            var cands = new List<Candidate>();
            string bone = null;
            foreach (GroupSpec m in members)
            {
                string piece = m.Prefabs.Select(p => p.Name).FirstOrDefault(n => Key(n) == key);
                if (piece != null && !Templates.ContainsKey(piece)) { Err("Unknown piece '" + piece + "' in augments.json"); piece = null; }
                if (piece != null && bone == null) bone = PieceBone(piece);
                cands.Add(new Candidate { AugmentIds = new HashSet<string>(m.AugmentIds), Piece = piece, Texture = Tex(m.Texture, null) });
            }
            if (bone == null) continue;
            Dispatch[dname] = cands;
            if (!Templates.ContainsKey(dname)) Templates[dname] = MakeTemplate(dname, bone, _emptyMesh);
            _vaPrefabs[dname] = Templates[dname];
            prefabs.Add(NewPrefab(null, dname, Tex(first.Texture, null)));
            RegisterSuppression(dname, bone, first.Suppress);
            if (first.HideRegions.Count > 0) HideByPiece[dname] = first.HideRegions;
        }
        return g;
    }
}

internal sealed class SkinData
{
    public string[] Bones;
    public int[][] Idx;          // [influence][vertex] -> index into Bones
    public float[][] W;          // [influence][vertex] weight
    public Vector3[][] A, An;    // [influence][vertex] position / normal in that bone's space (x10)
}

public sealed class SkinnedPiece : MonoBehaviour
{
    private SkinData _d;
    private Transform[] _bones;
    private Matrix4x4[] _m;
    private Mesh _mesh;
    private Vector3[] _v, _n;

    internal void Init(SkinData data, Transform[] bones)
    {
        _d = data; _bones = bones; _m = new Matrix4x4[bones.Length];
        var mf = GetComponent<MeshFilter>();
        _mesh = Instantiate(mf.sharedMesh);
        _mesh.MarkDynamic();
        mf.sharedMesh = _mesh;
        _v = new Vector3[_mesh.vertexCount]; _n = new Vector3[_v.Length];
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (_d == null || _mesh == null) return;
        Matrix4x4 toLocal = transform.worldToLocalMatrix, s = Matrix4x4.Scale(new Vector3(0.1f, 0.1f, 0.1f));
        for (int b = 0; b < _bones.Length; b++) { if (_bones[b] == null) return; _m[b] = toLocal * _bones[b].localToWorldMatrix * s; }
        int K = _d.W.Length;
        for (int i = 0; i < _v.Length; i++)
        {
            Vector3 p = Vector3.zero, q = Vector3.zero;
            for (int k = 0; k < K; k++)
            {
                float w = _d.W[k][i];
                if (w <= 0f) continue;
                Matrix4x4 m = _m[_d.Idx[k][i]];
                p += w * m.MultiplyPoint3x4(_d.A[k][i]);
                q += w * m.MultiplyVector(_d.An[k][i]);
            }
            _v[i] = p; _n[i] = q.normalized;
        }
        _mesh.vertices = _v;
        _mesh.normals = _n;
        _mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }
}

public sealed class MeshHider : MonoBehaviour
{
    // Actor body vertex count -> region -> triangle indices (from aeva_hide.json)
    private static readonly Dictionary<int, Dictionary<string, int[]>> Regions = new Dictionary<int, Dictionary<string, int[]>>();
    private static readonly Dictionary<Mesh, Dictionary<string, Mesh>> Variants = new Dictionary<Mesh, Dictionary<string, Mesh>>();
    private static readonly Dictionary<Mesh, Mesh> Original = new Dictionary<Mesh, Mesh>();

    private MeshFilter _filter;
    private readonly SortedSet<string> _regions = new SortedSet<string>();
    private string _key = "";

    internal static void Load(JSONNode root)
    {
        foreach (JSONNode a in root["actors"].AsArray)
        {
            var map = new Dictionary<string, int[]>();
            foreach (KeyValuePair<string, JSONNode> r in a["regions"].AsObject)
            {
                var list = new List<int>();
                foreach (JSONNode i in r.Value.AsArray) list.Add(i.AsInt);
                map[r.Key] = list.ToArray();
            }
            Regions[a["vertexCount"].AsInt] = map;
        }
    }

    internal void Add(MeshFilter filter, List<string> regions)
    {
        _filter = filter;
        foreach (string r in regions) _regions.Add(r);
        _key = string.Join("+", _regions);
        enabled = true;
        LateUpdate();
    }

    internal void Clear()
    {
        _regions.Clear();
        _key = "";
        Restore();
        enabled = false;
    }

    private void Restore()
    {
        if (_filter != null && _filter.sharedMesh != null && Original.TryGetValue(_filter.sharedMesh, out Mesh src)) _filter.sharedMesh = src;
    }

    private void OnDisable() => Restore();

    private void LateUpdate()
    {
        if (_filter == null || _regions.Count == 0) return;
        Mesh cur = _filter.sharedMesh;
        if (cur == null) return;
        if (Original.TryGetValue(cur, out Mesh src))
        {
            if (Variants.TryGetValue(src, out var have) && have.TryGetValue(_key, out Mesh same) && same == cur) return;   // already ours
            cur = src;
        }
        Mesh v = VariantOf(cur);
        if (v != null && _filter.sharedMesh != v) _filter.sharedMesh = v;
    }

    private Mesh VariantOf(Mesh src)
    {
        if (!Variants.TryGetValue(src, out var byKey)) Variants[src] = byKey = new Dictionary<string, Mesh>();
        if (byKey.TryGetValue(_key, out Mesh v)) return v;
        v = null;
        try
        {
            if (Regions.TryGetValue(src.vertexCount, out var map) && src.isReadable)
            {
                var drop = new HashSet<int>();
                foreach (string r in _regions) if (map.TryGetValue(r, out int[] tris)) foreach (int t in tris) drop.Add(t);
                int[] all = src.triangles;
                var keep = new List<int>(all.Length);
                for (int t = 0; t * 3 + 2 < all.Length; t++)
                    if (!drop.Contains(t)) { keep.Add(all[t * 3]); keep.Add(all[t * 3 + 1]); keep.Add(all[t * 3 + 2]); }
                v = Instantiate(src);
                v.name = src.name + " (aeva " + _key + ")";
                v.SetTriangles(keep, 0);
                Original[v] = src;
            }
        }
        catch (Exception ex) { Debug.LogError("[" + ModMain.Id + "] Body mesh hide failed: " + ex.Message); v = null; }
        byKey[_key] = v;   // null = this body is not one we know (e.g. another actor)
        return v;
    }
}
// Ligma balls
// ^^^ Look at this guy, soooo unprofessional! 