using UnityEngine;

/// <summary>Fits the complete design inside the device safe area, including HUD.</summary>
public class SafeBoardLayout : MonoBehaviour
{
    void LateUpdate()
    {
        var r = (RectTransform)transform;
        var parent = (RectTransform)r.parent;
        if (Screen.width == 0 || Screen.height == 0) return;
        Rect safe = Screen.safeArea;
        // The editor/device simulator can report the previous orientation's safe area
        // for a frame after resizing. Never scale against a rectangle outside the view.
        if (safe.width <= 0 || safe.height <= 0 || safe.xMin < 0 || safe.yMin < 0 || safe.xMax > Screen.width || safe.yMax > Screen.height)
            safe = new Rect(0,0,Screen.width,Screen.height);
        float adHeight=CashflowAds.Instance!=null?CashflowAds.Instance.ReservedBottomPixels:0;
        // Reserve space above the bottom safe inset; board play has no banner.
        safe.yMin+=Mathf.Min(adHeight,safe.height*.35f);
        float sx = parent.rect.width / Screen.width, sy = parent.rect.height / Screen.height;
        float scale = Mathf.Min((safe.width * sx - 24) / 1280, (safe.height * sy - 24) / 720);
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.sizeDelta = new Vector2(1280, 720);
        r.anchoredPosition = new Vector2((safe.center.x - Screen.width / 2f) * sx, (safe.center.y - Screen.height / 2f) * sy);
        r.localScale = Vector3.one * Mathf.Max(.01f, scale);
    }
}
