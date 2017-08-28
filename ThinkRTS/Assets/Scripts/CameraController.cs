using UnityEngine;

public class CameraController : MonoBehaviour
{
    void Update()
    {
        //Rotate the camera if the RMB is held down.
        if (Input.GetMouseButton(1))
        {
            float xMouse = Input.GetAxis("Mouse X");
            float yMouse = Input.GetAxis("Mouse Y");

            Camera.main.transform.Rotate(Vector3.up, xMouse, Space.World); //use world space for sideways rotation
            Camera.main.transform.Rotate(Vector3.left, yMouse*2, Space.Self);
        }

        //Translate the camera based on wasd keys'
        float xAxis = Input.GetAxis("Horizontal");
        float zAxis = Input.GetAxis("Vertical");

        Camera.main.transform.Translate(xAxis, 0, zAxis, Space.Self);
    }
}
