using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class BlockOutsideViewport : MonoBehaviour, ICanvasRaycastFilter
{
    public Camera viewportCam; // ���� ī�޶�
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCam)
    {
        var cam = viewportCam ? viewportCam : Camera.main;
        return cam ? cam.pixelRect.Contains(sp) : true;
    }
}
