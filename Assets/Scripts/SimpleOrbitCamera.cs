using UnityEngine;

public class SimpleOrbitCamera : MonoBehaviour
{
    public Transform target;     // deixe vazio para orbitar (0,0,0)
    public float distance = 4f;
    public float minDistance = 1.5f;
    public float maxDistance = 12f;
    public float rotateSpeed = 100f;
    public float zoomSpeed = 4f;
    public Vector2 pitchClamp = new Vector2(-20f, 80f);

    float _yaw;
    float _pitch = 15f;

    void LateUpdate()
    {
        Vector3 pivot = target ? target.position : Vector3.zero;

        if (Input.GetMouseButton(0))
        {
            _yaw += Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            _pitch -= Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            _pitch = Mathf.Clamp(_pitch, pitchClamp.x, pitchClamp.y);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pos = pivot - rot * Vector3.forward * distance;

        transform.SetPositionAndRotation(pos, rot);
    }
}
