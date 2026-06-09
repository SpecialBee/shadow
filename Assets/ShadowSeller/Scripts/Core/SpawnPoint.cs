using UnityEngine;

namespace ShadowSeller.Core
{
    public class SpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);

            var pos = transform.position;
            float r = 0.3f;

            // 원
            DrawCircle(pos, r);

            // 십자
            Gizmos.DrawLine(pos + Vector3.up    * r, pos - Vector3.up    * r);
            Gizmos.DrawLine(pos + Vector3.right * r, pos - Vector3.right * r);
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            int steps = 24;
            for (int i = 0; i < steps; i++)
            {
                float a0 = i       * Mathf.PI * 2f / steps;
                float a1 = (i + 1) * Mathf.PI * 2f / steps;
                Gizmos.DrawLine(
                    center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * radius,
                    center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius);
            }
        }
    }
}
