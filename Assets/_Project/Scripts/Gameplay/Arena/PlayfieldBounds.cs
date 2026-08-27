using UnityEngine;

namespace Arkanoid.Gameplay
{
    /// <summary>Границы игрового поля (плоскость XY).</summary>
    public sealed class PlayfieldBounds : MonoBehaviour
    {
        [SerializeField]
        private float minX = -5f;

        [SerializeField]
        private float maxX = 5f;

        [SerializeField]
        private float minY = -8.6f;

        [SerializeField]
        private float maxY = 10.5f;

        public float MinX => minX;
        public float MaxX => maxX;
        public float MinY => minY;
        public float MaxY => maxY;

        public void Set(float minXValue, float maxXValue, float minYValue, float maxYValue)
        {
            minX = minXValue;
            maxX = maxXValue;
            minY = minYValue;
            maxY = maxYValue;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            var c = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            var s = new Vector3(maxX - minX, maxY - minY, 0.1f);
            Gizmos.DrawWireCube(c, s);
        }
#endif
    }
}
