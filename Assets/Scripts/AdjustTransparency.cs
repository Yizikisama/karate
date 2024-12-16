using UnityEngine;

public class AdjustTransparency : MonoBehaviour
{
    public float transparency = 0.5f;  // 透明度值，范围0到1

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        Color color = renderer.material.color;
        color.a = transparency;
        renderer.material.color = color;
    }
}