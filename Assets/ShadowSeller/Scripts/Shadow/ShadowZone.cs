using UnityEngine;

namespace ShadowSeller.Core
{
    [RequireComponent(typeof(Collider2D))]
    public class ShadowZone : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;

            // SpriteRenderer가 이미 있으면(ShadowProjector 생성 등) 시각 자동 생성 스킵
            if (GetComponent<SpriteRenderer>() == null)
                CreateVisual();
        }

        private void CreateVisual()
        {
            var col = GetComponent<Collider2D>();

            Vector3 localPos   = Vector3.zero;
            Vector3 localScale = Vector3.one;

            if (col is BoxCollider2D box)
            {
                localPos   = new Vector3(box.offset.x, box.offset.y, 0f);
                localScale = new Vector3(box.size.x, box.size.y, 1f);
            }
            else if (col is CircleCollider2D circle)
            {
                float d = circle.radius * 2f;
                localScale = new Vector3(d, d, 1f);
            }
            else return; // PolygonCollider2D 등 — 수동 배치 필요

            var go = new GameObject("_Visual");
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = localScale;

            var sr         = go.AddComponent<SpriteRenderer>();
            sr.sprite       = BuildSprite();
            sr.color        = new Color(0.1f, 0.3f, 0.9f, 0.25f);
            sr.sortingOrder = -1;
        }

        private static Sprite BuildSprite()
        {
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.1f, 0.3f, 0.9f, 0.30f);

            var col = GetComponent<Collider2D>();
            if (col is BoxCollider2D box)
                Gizmos.DrawCube(transform.position + (Vector3)(Vector2)box.offset, box.size);
            else if (col is CircleCollider2D circle)
                Gizmos.DrawSphere(transform.position, circle.radius);
            else if (col is PolygonCollider2D poly)
            {
                var pts = poly.points;
                for (int i = 0; i < pts.Length; i++)
                {
                    var a = transform.TransformPoint(pts[i]);
                    var b = transform.TransformPoint(pts[(i + 1) % pts.Length]);
                    Gizmos.DrawLine(a, b);
                }
            }
        }
    }
}
