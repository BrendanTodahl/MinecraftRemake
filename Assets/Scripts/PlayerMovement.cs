using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    private const float speed = 5.0f;
    private const float jumpForce = 250.0f;

    private float elevateAngle;
    private new GameObject camera;

    public float ElevateAngle
    {
        get
        {
            return elevateAngle;
        }

        set
        {
            elevateAngle = value;
        }
    }
    public GameObject Camera
    {
        get
        {
            return camera;
        }

        set
        {
            camera = value;
        }
    }

    void Start () {
        Camera = transform.FindChild("Main Camera").gameObject;
        Cursor.lockState = CursorLockMode.Locked;
        ElevateAngle = 0.0f;
	}
    
    void FixedUpdate () {
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");
        transform.Translate(new Vector3(inputX, 0.0f, inputY) * speed * Time.deltaTime);

        ElevateAngle += -Input.GetAxis("Mouse Y");
        if (ElevateAngle < -90.0f)
        {
            ElevateAngle = -90.0f;
        }
        else if (ElevateAngle > 90.0f)
        {
            ElevateAngle = 90.0f;
        }
        Camera.transform.localEulerAngles = new Vector3(ElevateAngle, 0.0f, 0.0f);

        float lookLeftRight = Input.GetAxis("Mouse X");
        transform.Rotate(0.0f, lookLeftRight, 0.0f);

        if (Input.GetButtonDown("Jump") && isGrounded())
        {
            GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce);
        }
	}

    private bool isGrounded()
    {
        bool result = false;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.0f))
        {
            if (hit.transform.gameObject.CompareTag("Environment"))
            {
                result = true;
            }
        }
        return result;
    }
}
