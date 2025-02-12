using UnityEngine;

public class CameraPlayer : MonoBehaviour
{
    private Transform cameraPosition;
    public float sensY = 200;
    public float sensX = 200;
    [SerializeField]
    private GameObject player;

    float rotationY;
    float rotationX;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        player = GameObject.FindGameObjectWithTag("Player");
        cameraPosition = player.transform.GetChild(0);
    }
    void Update()
    {
        transform.position = cameraPosition.position;

        float mouseX = Input.GetAxis("Mouse X") * sensX * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensY * Time.deltaTime;

        rotationY += mouseX;
        rotationX -= mouseY;
    
        rotationX = Mathf.Clamp(rotationX, -80f, 80);
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
        player.transform.rotation = Quaternion.Euler(0, rotationY, 0);
            
    }
}
