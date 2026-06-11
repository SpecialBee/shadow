using UnityEngine;
using System.Collections.Generic;

namespace ShadowSeller.Core
{
    // 타원형 그림자 은신 판정 컴포넌트.
    //   독립 배치 모드 (createVisual=true):  Inspector에서 radiusX/radiusY로 크기 지정, 비주얼 자동 생성.
    //   ShadowProjector 연동 모드 (createVisual=false): lossyScale을 반경으로 사용, 비주얼 미생성.
    //     → ShadowProjector가 GO 크기와 회전을 매 프레임 갱신하면 판정도 자동으로 동기화됨.
    //   PlayerExposureTracker가 EllipseShadow.All을 순회해 플레이어 5점 샘플 판정.
    public class EllipseShadow : MonoBehaviour
    {
        [Header("판정 범위 (독립 배치 시, 월드 단위)")]
        [SerializeField] public float radiusX = 1.2f;
        [SerializeField] public float radiusY = 0.6f;

        [Header("비주얼 (독립 배치 시)")]
        [SerializeField] public bool  createVisual = true;
        [SerializeField] private Color shadowColor  = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private int   sortingOrder = -1;

        public static readonly List<EllipseShadow> All = new List<EllipseShadow>();

        private void OnEnable()  => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            if (createVisual) BuildVisual();
        }

        // 월드 좌표 point가 이 타원 안에 있으면 true.
        //   createVisual=false 모드에서는 GO의 lossyScale/2 를 반경으로 사용.
        public bool ContainsPoint(Vector2 worldPoint)
        {
            float rx, ry;
            if (createVisual)
            {
                rx = radiusX;
                ry = radiusY;
            }
            else
            {
                rx = Mathf.Abs(transform.lossyScale.x) * 0.5f;
                ry = Mathf.Abs(transform.lossyScale.y) * 0.5f;
            }
            if (rx < 0.001f || ry < 0.001f) return false;

            Vector2 offset = worldPoint - (Vector2)transform.position;
            float rad = -transform.eulerAngles.z * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            float lx  = offset.x * cos - offset.y * sin;
            float ly  = offset.x * sin + offset.y * cos;

            return (lx / rx) * (lx / rx) + (ly / ry) * (ly / ry) <= 1f;
        }

        private void BuildVisual()
        {
            var go = new GameObject("_EllipseVisual");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            Vector3 ls = transform.lossyScale;
            go.transform.localScale = new Vector3(
                Mathf.Abs(ls.x) > 0.001f ? radiusX * 2f / ls.x : radiusX * 2f,
                Mathf.Abs(ls.y) > 0.001f ? radiusY * 2f / ls.y : radiusY * 2f,
                1f);

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = BuildGradientSprite(64);
            sr.color        = shadowColor;
            sr.sortingOrder = sortingOrder;

            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null) sr.material = new Material(shader);
        }

        // 중심이 불투명하고 가장자리가 투명한 그라데이션 원형 스프라이트.
        // ShadowProjector에서도 사용하므로 internal static.
        // ppu: 픽셀당 월드 단위. 기본 64f → 스프라이트 크기 1×1 월드 단위.
        //      원본 스프라이트 크기에 맞추려면 (res / srcWorldSize) 를 전달.
        internal static Sprite BuildGradientSprite(int res, float ppu = 64f)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = res * 0.5f;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * center);
                float t     = Mathf.Clamp01(dist / center);
                float alpha = 1f - t * t; // 역이차 — 중심 넓게 유지, 가장자리만 부드럽게
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f, Mathf.Max(ppu, 0.01f));
        }

        private void OnDrawGizmos()
        {
            float rx = createVisual ? radiusX : Mathf.Abs(transform.lossyScale.x) * 0.5f;
            float ry = createVisual ? radiusY : Mathf.Abs(transform.lossyScale.y) * 0.5f;

            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.85f);
            float rot  = transform.eulerAngles.z * Mathf.Deg2Rad;
            float cosR = Mathf.Cos(rot), sinR = Mathf.Sin(rot);
            Vector2 c = transform.position;
            const int seg = 40;

            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= seg; i++)
            {
                float a  = i * 2f * Mathf.PI / seg;
                float lx = Mathf.Cos(a) * rx;
                float ly = Mathf.Sin(a) * ry;
                Vector3 pt = (Vector3)(c + new Vector2(lx * cosR - ly * sinR, lx * sinR + ly * cosR));
                if (i > 0) Gizmos.DrawLine(prev, pt);
                prev = pt;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!createVisual) return;
            var visual = transform.Find("_EllipseVisual");
            if (visual == null) return;
            Vector3 ls = transform.lossyScale;
            visual.localScale = new Vector3(
                Mathf.Abs(ls.x) > 0.001f ? radiusX * 2f / ls.x : radiusX * 2f,
                Mathf.Abs(ls.y) > 0.001f ? radiusY * 2f / ls.y : radiusY * 2f,
                1f);
        }
#endif
    }
}
