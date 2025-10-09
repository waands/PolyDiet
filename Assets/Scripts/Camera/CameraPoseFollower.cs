using UnityEngine;

[DisallowMultipleComponent]
public class CameraPoseFollower : MonoBehaviour
{
    public Transform source;   // arraste aqui a câmera "driver" (a que tem SimpleOrbitCamera)
    public Camera   sourceCam; // opcional: copiar FOV e clip

    void LateUpdate()
    {
        if (!source) return;
        transform.SetPositionAndRotation(source.position, source.rotation);

        if (sourceCam && TryGetComponent(out Camera dst))
        {
            dst.fieldOfView   = sourceCam.fieldOfView;
            dst.nearClipPlane = sourceCam.nearClipPlane;
            dst.farClipPlane  = sourceCam.farClipPlane;
        }
    }
}


