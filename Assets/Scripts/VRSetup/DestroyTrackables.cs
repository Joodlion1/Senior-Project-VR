using UnityEngine;

public class DestroyTrackables : MonoBehaviour
{
    void Start()
    {
        Transform trackables = transform.Find("Camera Offset/Trackables");
        if (trackables == null) trackables = transform.Find("Trackables");
        if (trackables != null) Destroy(trackables.gameObject);
    }
}
