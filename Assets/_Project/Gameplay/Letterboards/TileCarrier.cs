using System.Collections.Generic;
using Kit.Core.Events;
using Kit.Gameplay.Signage;
using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>
    /// Lives on the player (added by LetterboardSystem on PlayerSpawned). On Interact it raycasts
    /// from the main camera along its real forward (with pitch) against LetterSlot triggers within
    /// LetterboardConfig.Reach: a full slot is pulled into the hand, an empty slot receives the
    /// oldest carried tile. Carried tiles show as small plates at the lower right of the view
    /// (layer ViewModel), which is a hand, not a HUD.
    /// </summary>
    public sealed class TileCarrier : MonoBehaviour
    {
        const string ViewRootName = "TileHand";
        const string ViewModelLayer = "ViewModel";

        readonly List<char> carried = new List<char>();
        static readonly RaycastHitDistanceComparer hitComparer = new RaycastHitDistanceComparer();

        Transform viewRoot;
        Quaternion lastCameraRotation;
        Vector3 sway;

        /// <summary>Tiles in hand, oldest first.</summary>
        public IReadOnlyList<char> Carried => carried;

        void OnEnable()
        {
            EventBus.Subscribe<PlayerInteracted>(OnInteracted);
            EventBus.Subscribe<GameStarted>(OnGameStarted);
            RefreshView();
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PlayerInteracted>(OnInteracted);
            EventBus.Unsubscribe<GameStarted>(OnGameStarted);
            DestroyView();
        }

        /// <summary>
        /// A light lag on the carried tiles when the view turns, so the hand feels held rather than
        /// painted on the lens. Only runs while tiles are shown.
        /// </summary>
        void LateUpdate()
        {
            if (viewRoot == null) return;
            var cam = viewRoot.parent;
            if (cam == null) return;
            var cfg = ConfigLookup.Get<LetterboardConfig>();

            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            var delta = Quaternion.Inverse(lastCameraRotation) * cam.rotation;
            lastCameraRotation = cam.rotation;
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (float.IsNaN(axis.x) || float.IsInfinity(axis.x)) axis = Vector3.zero;
            Vector3 turnRate = axis * (angle * Mathf.Deg2Rad / dt); // rad/s, camera-local axis

            // Yaw (local y) pushes the hand sideways, pitch (local x) pushes it vertically.
            Vector3 target = new Vector3(-turnRate.y, turnRate.x, 0f) * cfg.ViewSway;
            target = Vector3.ClampMagnitude(target, 0.03f);
            sway = Vector3.Lerp(sway, target, 1f - Mathf.Exp(-12f * dt));
            viewRoot.localPosition = sway;
        }

        void OnInteracted(PlayerInteracted e)
        {
            var cam = Camera.main;
            if (cam == null) InteractAlong(e.Position, e.Forward);
            else InteractAlong(cam.transform.position, cam.transform.forward);
        }

        void OnGameStarted(GameStarted _) => Clear();

        /// <summary>Adds a tile directly (tests, debug). False when the hand is full.</summary>
        public bool Give(char glyph)
        {
            if (glyph == ' ' || glyph == '_') return false;
            if (carried.Count >= ConfigLookup.Get<LetterboardConfig>().MaxCarried) return false;
            carried.Add(glyph);
            RefreshView();
            return true;
        }

        /// <summary>Empties the hand (tests, debug, restart).</summary>
        public void Clear()
        {
            carried.Clear();
            RefreshView();
        }

        /// <summary>
        /// Interacts with whatever slot the ray from <paramref name="origin"/> along
        /// <paramref name="direction"/> hits within reach. Returns true if a tile moved.
        /// Called on Interact with the main camera's ray; public so tests can aim exactly.
        /// </summary>
        public bool InteractAlong(Vector3 origin, Vector3 direction)
        {
            var slot = FindSlot(origin, direction);
            if (slot == null || slot.Board == null) return false;

            var board = slot.Board;
            var cfg = ConfigLookup.Get<LetterboardConfig>();
            if (board.GlyphAt(slot.Index) != ' ')
            {
                if (carried.Count >= cfg.MaxCarried) return false;
                if (!board.TryPull(slot.Index, out char glyph)) return false;
                carried.Add(glyph);
                RefreshView();
                return true;
            }

            if (carried.Count == 0) return false;
            if (!board.TryPlace(slot.Index, carried[0])) return false;
            carried.RemoveAt(0);
            RefreshView();
            return true;
        }

        /// <summary>
        /// The first LetterSlot along the ray within reach. Triggers are included (slots are
        /// triggers); any solid collider in front of the slot blocks it, so you can't reach
        /// through walls. The player's own colliders are skipped since the ray starts inside them.
        /// </summary>
        LetterSlot FindSlot(Vector3 origin, Vector3 direction)
        {
            if (direction.sqrMagnitude < 1e-8f) return null;
            float reach = ConfigLookup.Get<LetterboardConfig>().Reach;
            var hits = Physics.RaycastAll(origin, direction.normalized, reach, Physics.AllLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, hitComparer);
            foreach (var hit in hits)
            {
                var col = hit.collider;
                if (col == null) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform) || col is CharacterController) continue;
                var slot = col.GetComponent<LetterSlot>();
                if (slot != null) return slot;
                if (!col.isTrigger) return null;
            }
            return null;
        }

        /// <summary>
        /// Rebuilds the carried-tile plates under the main camera. Skipped when there's no main
        /// camera (tests), and the whole row is rebuilt because it holds at most a few tiles.
        /// </summary>
        void RefreshView()
        {
            DestroyView();
            if (!isActiveAndEnabled || carried.Count == 0 || !Application.isPlaying) return;
            var cam = Camera.main;
            if (cam == null) return;

            var cfg = ConfigLookup.Get<LetterboardConfig>();
            int layer = LayerMask.NameToLayer(ViewModelLayer);
            if (layer < 0) layer = 0;

            var root = new GameObject(ViewRootName);
            root.transform.SetParent(cam.transform, false);
            viewRoot = root.transform;
            lastCameraRotation = cam.transform.rotation;
            viewRoot.localPosition = sway;

            float depth = cfg.ViewTileSize.x * 0.12f;
            int count = carried.Count;
            for (int i = 0; i < count; i++)
            {
                var tile = new GameObject("Tile_" + i);
                tile.transform.SetParent(viewRoot, false);
                // Newest tile sits at ViewOffset; older ones line up to its left.
                tile.transform.localPosition = cfg.ViewOffset + Vector3.left * ((count - 1 - i) * cfg.ViewTileSpacing);
                tile.transform.localRotation = Quaternion.Euler(0f, cfg.ViewTileYaw, 0f);

                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "Plate";
                DestroyImmediate(plate.GetComponent<Collider>()); // a hand tile must never block rays or the player
                plate.transform.SetParent(tile.transform, false);
                plate.transform.localScale = new Vector3(cfg.ViewTileSize.x, cfg.ViewTileSize.y, depth);
                if (cfg.FeltMaterial != null) plate.GetComponent<Renderer>().sharedMaterial = cfg.FeltMaterial;
                var r = plate.GetComponent<Renderer>();
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;

                // The camera looks along the tile's +Z, i.e. at its -Z face, which is the side a
                // WorldText reads from, so the glyph needs no yaw here (unlike on a board).
                WorldText.Create(tile.transform, "Glyph", carried[i].ToString(), new Vector3(0f, 0f, -depth * 0.5f - 0.0005f),
                    Quaternion.identity, cfg.ViewTileSize.y * cfg.GlyphScale, WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter);
            }
            SetLayerRecursive(root.transform, layer);
        }

        void DestroyView()
        {
            if (viewRoot == null) return;
            // No re-parenting here: OnDisable can run while the camera itself is being torn down,
            // and changing the hierarchy then is an error. A one-frame overlap on rebuild is invisible.
            var go = viewRoot.gameObject;
            viewRoot = null;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
        }

        /// <summary>Sorts RaycastAll results nearest first (RaycastAll returns them unordered).</summary>
        sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
