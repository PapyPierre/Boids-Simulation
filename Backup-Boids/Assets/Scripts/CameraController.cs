using UnityEngine;

public class CameraController : MonoBehaviour
{
   [SerializeField] private float camSpeed;
   [SerializeField] private float antiInertia;

   private Vector3 _newPos;
   [SerializeField] private Vector3 zoomAmount;
   private Vector3 _newZoom;
   
   private void Start()
   {
      _newPos = transform.position;
      _newZoom = transform.localPosition;
   }

   private void Update()
   {
      HandleMovementInput();
   }

   private void HandleMovementInput()
   {
      if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.UpArrow))
      {
         _newPos += ((transform.forward + transform.up) * camSpeed);
      }
      if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
      {
         _newPos += ((transform.forward + transform.up) * -camSpeed);
      }
      if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow))
      {
         _newPos += (transform.right * -camSpeed);
      }
      if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
      {
         _newPos += (transform.right * camSpeed);
      }

      if (Input.GetKey(KeyCode.A))
      {
         _newZoom += zoomAmount;
      }
      if (Input.GetKey(KeyCode.E))
      {
         _newZoom -= zoomAmount;
      }

      transform.position = Vector3.Lerp(transform.position, _newPos, Time.deltaTime * antiInertia);
      transform.localPosition = Vector3.Lerp(transform.localPosition, _newZoom, Time.deltaTime * antiInertia);
   }
}
