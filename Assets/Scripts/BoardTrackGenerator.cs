using UnityEngine;
using UnityEngine.UI;

/// <summary>A constant-width stadium track. Tile edges and token positions share one path.</summary>
public static class BoardTrackGenerator
{
    public static float Straight = 328f;
    public static Vector2 Point(float t, float radius)
    {
        float straight=Straight;
        float length = 4 * straight + 2 * Mathf.PI * radius;
        float d = Mathf.Repeat(t * length + straight, length);
        if (d < 2 * straight) return new Vector2(-straight + d, -radius);
        d -= 2 * straight;
        if (d < Mathf.PI * radius) { float a = d / radius - Mathf.PI / 2; return new Vector2(straight + Mathf.Cos(a) * radius, Mathf.Sin(a) * radius); }
        d -= Mathf.PI * radius;
        if (d < 2 * straight) return new Vector2(straight - d, radius);
        d -= 2 * straight;
        float b = d / radius + Mathf.PI / 2;
        return new Vector2(-straight + Mathf.Cos(b) * radius, Mathf.Sin(b) * radius);
    }

    public static Vector2 Normal(float t, float radius)
    {
        Vector2 p = Point(t, radius);
        if (p.x > Straight) return (p - new Vector2(Straight, 0)).normalized;
        if (p.x < -Straight) return (p - new Vector2(-Straight, 0)).normalized;
        return new Vector2(0, Mathf.Sign(p.y));
    }

    public static BoardTrackSegment Segment(Transform parent, int index, int count, float radius, float width, Color color)
    {
        var go = new GameObject("Space " + (index + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(BoardTrackSegment));
        go.transform.SetParent(parent, false);
        var segment = go.GetComponent<BoardTrackSegment>();
        segment.rectTransform.sizeDelta = new Vector2(1200+2*(Straight-328), 540);
        segment.index = index; segment.count = count; segment.radius = radius; segment.width = width; segment.color = color;
        return segment;
    }
}

public class BoardTrackSegment : MaskableGraphic, ICanvasRaycastFilter
{
    public int index, count;
    public float radius, width;
    const int Steps = 32;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        for (int j = 0; j <= Steps; j++)
        {
            float t = (index - .5f + j / (float)Steps) / count;
            Vector2 p = BoardTrackGenerator.Point(t, radius), n = BoardTrackGenerator.Normal(t, radius);
            vh.AddVert(p - n * width / 2, color, Vector2.zero);
            vh.AddVert(p + n * width / 2, color, Vector2.one);
            if (j > 0) { int k = j * 2; vh.AddTriangle(k - 2, k, k - 1); vh.AddTriangle(k - 1, k, k + 1); }
        }
        // One shared divider per space, so borders never double up.
        float start = (index - .5f) / count;
        Vector2 center = BoardTrackGenerator.Point(start, radius), normal = BoardTrackGenerator.Normal(start, radius);
        Vector2 tangent = new Vector2(-normal.y, normal.x) * .7f;
        int v = vh.currentVertCount;
        Color border = new Color(1, .92f, .72f, .9f);
        vh.AddVert(center - normal * width / 2 - tangent, border, Vector2.zero);
        vh.AddVert(center + normal * width / 2 - tangent, border, Vector2.zero);
        vh.AddVert(center + normal * width / 2 + tangent, border, Vector2.zero);
        vh.AddVert(center - normal * width / 2 + tangent, border, Vector2.zero);
        vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v, v + 2, v + 3);
    }
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var p);
        for (int j = 0; j < Steps; j++)
        {
            float a = (index - .5f + j / (float)Steps) / count, b = (index - .5f + (j + 1f) / Steps) / count;
            Vector2 x = BoardTrackGenerator.Point(a, radius), y = BoardTrackGenerator.Point(b, radius);
            Vector2 edge = y - x;
            float u = Vector2.Dot(p - x, edge) / edge.sqrMagnitude;
            if (u >= 0 && u <= 1 && Vector2.Distance(p, x + edge * u) <= width / 2) return true;
        }
        return false;
    }
}
