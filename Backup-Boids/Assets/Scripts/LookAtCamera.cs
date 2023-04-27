using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Transform _cameraTransform;
    private bool _oneOnTwo;
    
    private void Start()
    {
        _cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        _oneOnTwo = !_oneOnTwo;
        if (!_oneOnTwo) return;
        
        var rotation = _cameraTransform.rotation;
        transform.LookAt(transform.position + rotation * Vector3.forward, rotation * Vector3.up);
    }
}