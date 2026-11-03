using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// A numbered opening. The rectangle of Size (width, height) is centred on this transform and
    /// transform.forward is its front (the side players normally approach from). Its number comes
    /// from an INumberSource on this object or a child (BrassPlate or Letterboard). When exactly one
    /// other threshold shows the same number the two are paired: crossing one comes out of the
    /// other (ThresholdTraveller) and each shows the other's side (Portals). Built by OfficeKit.Threshold.
    /// </summary>
    public sealed class Threshold : MonoBehaviour
    {
        [Tooltip("Opening width and height in metres, centred on this transform.")]
        [SerializeField] Vector2 size = new Vector2(1f, 2.1f);
        [Tooltip("True only on the outside door 000: arriving through it ends the game (EndingDirector).")]
        [SerializeField] bool exterior;
        [Tooltip("Empty child where the portal surface is placed (Portals system).")]
        [SerializeField] Transform portalSurfaceRoot;

        INumberSource source;

        /// <summary>Opening width and height (m).</summary>
        public Vector2 Size => size;
        public bool Exterior => exterior;
        public Transform PortalSurfaceRoot => portalSurfaceRoot != null ? portalSurfaceRoot : transform;

        /// <summary>The plate on this threshold (BrassPlate or Letterboard), or null.</summary>
        public INumberSource Source => source ??= GetComponentInChildren<INumberSource>(true);

        /// <summary>Normalised number ("" = none).</summary>
        public string Number => ThresholdNumber.Normalize(Source != null ? Source.Raw : "");

        /// <summary>The paired threshold, or null. Set by ThresholdRegistry.</summary>
        public Threshold Partner { get; internal set; }

        public bool IsPaired => Partner != null;

        /// <summary>World pose of the opening's centre (forward = front).</summary>
        public Pose Pose => new Pose(transform.position, transform.rotation);

        /// <summary>Map from this opening to its partner (identity when unpaired).</summary>
        public Matrix4x4 PairMap => Partner != null ? ThresholdMath.PairMap(Pose, Partner.Pose) : Matrix4x4.identity;

        /// <summary>Builder entry point (edit mode).</summary>
        public void Setup(Vector2 openingSize, bool isExterior, Transform surfaceRoot)
        {
            size = openingSize;
            exterior = isExterior;
            portalSurfaceRoot = surfaceRoot;
        }

        void OnEnable()
        {
            source = null;
            ThresholdRegistry.Register(this);
        }

        void OnDisable() => ThresholdRegistry.Unregister(this);

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = IsPaired ? new Color(0.3f, 1f, 0.6f) : new Color(1f, 0.8f, 0.3f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.5f);
        }
    }
}
