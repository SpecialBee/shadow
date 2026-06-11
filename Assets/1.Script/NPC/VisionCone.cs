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

        private static readonly Color ColIdle       = new Color(1.0f, 1.0f, 0.4f, 0.18f);
        private static readonly Color ColSuspicious = new Color(1.0f, 0.75f, 0.0f, 0.28f);
        private static readonly Color ColAlert      = new Color(1.0f, 0.4f,  0.0f, 0.35f);
        private static readonly Color ColChase      = new Color(0.9f, 0.1f,  0.1f, 0.42f);

        private void Awake()
        {
            _mf  = GetComponent<MeshFilter>();
            _mr  = GetComponent<MeshRenderer>();
            _npc = GetComponentInParent<NPCController>();

            _mesh      = new Mesh { name = "VisionConeMesh" };
            _mf.mesh   = _mesh;

            var shader = Shader.Find("Sprites/Default");
            var mat    = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            mat.color  = ColIdle;
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
            int vCount = Segments + 2;
            var verts  = new Vector3[vCount];
            var tris   = new int[Segments * 3];

            verts[0] = Vector3.zero;

            float half = angleDeg * 0.5f;
            for (int i = 0; i <= Segments; i++)
            {
                float t   = (float)i / Segments;
                float deg = Mathf.Lerp(-half, half, t);
                var   dir = RotateVec(facing, deg);
                verts[i + 1] = new Vector3(dir.x * range, dir.y * range, 0f);
            }

            for (int i = 0; i < Segments; i++)
            {
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            _mesh.Clear();
            _mesh.vertices  = verts;
            _mesh.triangles = tris;
            _mesh.RecalculateNormals();
        }

        private void ApplyStateColor(NpcState state)
        {
            _mr.material.color = state switch
            {
                NpcState.Suspicious => ColSuspicious,
                NpcState.Alert      => ColAlert,
                NpcState.Chase      => ColChase,
                _                   => ColIdle,
            };
        }

        private static Vector2 RotateVec(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
