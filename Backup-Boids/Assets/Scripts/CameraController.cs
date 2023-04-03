using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
   [SerializeField] private float camSpeed;
   [SerializeField] private float movementTime;

   private Vector3 _newPos;
   private void Start()
   {
      _newPos = transform.position;
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

      transform.position = Vector3.Lerp(transform.position, _newPos, Time.deltaTime * movementTime);
   }
}
