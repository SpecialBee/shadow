using UnityEngine;

namespace ShadowSeller.Core
{
    // NPC 시야각 시각화 — NPCController가 Awake에서 자식 GO에 자동 부착.
    // 매 LateUpdate에 NPC의 FacingDir·viewAngle·viewRange를 읽어 부채꼴 메시를 재빌드.
    // 상태별 색상: Idle=노랑 / Suspicious=주황 / Alert=짙은주황 / Chase=빨강.
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VisionCone : MonoBehaviour
    {
        private const int Segments = 24;

        private MeshFilter    _mf;
        private MeshRenderer  _mr;
        private NPCController _npc;
        private Mesh          _mesh;

        private readonly Vector3[] _verts  = new Vector3[Segments + 2];
        private readonly int[]     _tris   = new int[Segments * 3];
        private readonly Color[]   _colors = new Color[Segments + 2];

        // 중심(NPC): 밝은 색상 / 끝: 투명 — Additive 블렌드로 빛 느낌 연출
        private static readonly Color ColIdle       = new Color(1.0f, 1.0f, 0.3f, 0.45f);
        private static readonly Color ColSuspicious = new Color(1.0f, 0.65f, 0.0f, 0.60f);
        private static readonly Color ColAlert      = new Color(1.0f, 0.3f,  0.0f, 0.72f);
        private static readonly Color ColChase      = new Color(1.0f, 0.05f, 0.05f, 0.85f);

        private void Awake()
        {
            _mf  = GetComponent<MeshFilter>();
            _mr  = GetComponent<MeshRenderer>();
            _npc = GetComponentInParent<NPCController>();

            _mesh      = new Mesh { name = "VisionConeMesh" };
            _mf.mesh   = _mesh;

            // Additive 블렌딩: 겹치는 영역이 더 밝아져 빛처럼 보임
            var shader = Shader.Find("Legacy Shaders/Particles/Additive")
                      ?? Shader.Find("Sprites/Default")
                      ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            var mat          = new Material(shader);
            mat.color        = Color.white; // 버텍스 컬러가 실제 색상 담당
            _mr.material     = mat;
            _mr.sortingOrder = -1;
        }

        private void LateUpdate()
        {
            if (_npc == null || _npc.KindData == null) return;
            RebuildMesh(_npc.FacingDir, _npc.KindData.viewAngle, _npc.KindData.viewRange);
            ApplyStateColor(_npc.CurrentState);
        }

        private void RebuildMesh(Vector2 facing, float angleDeg, float range)
        {
            _verts[0] = Vector3.zero;

            float half = angleDeg * 0.5f;
            for (int i = 0; i <= Segments; i++)
            {
                float t   = (float)i / Segments;
                float deg = Mathf.Lerp(-half, half, t);
                var   dir = RotateVec(facing, deg);
                _verts[i + 1] = new Vector3(dir.x * range, dir.y * range, 0f);
            }

            for (int i = 0; i < Segments; i++)
            {
                _tris[i * 3]     = 0;
                _tris[i * 3 + 1] = i + 1;
                _tris[i * 3 + 2] = i + 2;
            }

            _mesh.Clear();
            _mesh.vertices  = _verts;
            _mesh.triangles = _tris;
            _mesh.RecalculateNormals();
        }

        private void ApplyStateColor(NpcState state)
        {
            Color c = state switch
            {
                NpcState.Suspicious => ColSuspicious,
                NpcState.Alert      => ColAlert,
                NpcState.Chase      => ColChase,
                _                   => ColIdle,
            };

            // 중심(index 0): 불투명하게 밝음 / 호 꼭짓점들: 완전 투명 → 그라디언트 생성
            _colors[0] = c;
            for (int i = 1; i < _colors.Length; i++)
                _colors[i] = new Color(c.r, c.g, c.b, 0f);

            _mesh.colors = _colors;
        }

        private static Vector2 RotateVec(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
