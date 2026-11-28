using Lingjuan.Data;
using UnityEngine;

namespace Lingjuan.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TraceSpot : MonoBehaviour
    {
        [SerializeField] private BeastDefinition beast;
        [SerializeField, Min(0)] private int fragmentIndex;

        private SpriteRenderer _renderer;
        private Vector3 _authoredScale;

        public BeastDefinition Beast => beast;
        public int FragmentIndex => fragmentIndex;
        public TraceData Data => beast != null
            ? beast.traces?.Find(trace => trace != null && trace.fragmentIndex == fragmentIndex)
            : null;
        public SpriteRenderer Renderer => _renderer != null ? _renderer : GetComponent<SpriteRenderer>();

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _authoredScale = transform.localScale;
        }

        public void RestoreScale()
        {
            transform.localScale = _authoredScale;
        }

        public bool Hit(Ray ray)
        {
            if (!isActiveAndEnabled || !Renderer.enabled || Renderer.sprite == null ||
                !new Plane(transform.forward, transform.position).Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3 point = transform.InverseTransformPoint(ray.GetPoint(distance));
            Bounds bounds = Renderer.localBounds;
            return point.x >= bounds.min.x && point.x <= bounds.max.x &&
                   point.y >= bounds.min.y && point.y <= bounds.max.y;
        }
    }
}
