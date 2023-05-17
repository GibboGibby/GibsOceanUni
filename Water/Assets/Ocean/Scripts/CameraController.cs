using System.Collections;
using System.Collections.Generic;
using GibsOcean;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Start is called before the first frame update
    
    private float horizonalInput, verticalInput, upDownInput;
    private Vector3 moveDirection;
    
    private Quaternion initialRot;
    [Header("Mouse Movement Settings")] private float xRotation;
    [SerializeField] private float sensitivity = 50f;
    [SerializeField] private bool onBoat;
    [SerializeField] private bool shouldFloat;

    [SerializeField] private Transform boatCamPos;
    [SerializeField] private WaveGen waveGen;

    private bool sprint = false;
    [SerializeField] private float sprintMultiplier = 2f;
    



    [SerializeField] private float sensMultiplier = 1f;
    [SerializeField] private float moveSpeed = 1f;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        initialRot = transform.localRotation;
    }
    private float desiredX;
    
    private void FixedUpdate()
    {
        moveDirection = transform.forward * verticalInput + transform.right * horizonalInput + transform.up * upDownInput;
        if (sprint)
            moveDirection *= sprintMultiplier;
        transform.position += moveDirection * moveSpeed;
    }
    private void MyInput()
    {
        horizonalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        if (Input.GetKey(KeyCode.Q))
            upDownInput = 1;
        else if (Input.GetKey(KeyCode.E))
            upDownInput = -1;
        else
            upDownInput = 0;
        if (Input.GetKeyDown(KeyCode.L))
            onBoat = !onBoat;
        if (Input.GetKeyDown(KeyCode.K))
            shouldFloat = !shouldFloat;

        if (Input.GetKey(KeyCode.LeftShift))
            sprint = true;
        else
            sprint = false;

    }
    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;


        Vector3 rot = transform.localRotation.eulerAngles;
        desiredX = rot.y + mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        //Perform the rotations
        Vector3 v = transform.rotation.eulerAngles;
        transform.localRotation = Quaternion.Euler(xRotation, desiredX, v.z);
    }

    // Update is called once per frame
    void Update()
    {
        
        MyInput();
        Look();
       

        float waterHeight = waveGen.GetWaterHeight(transform.position);
        if (shouldFloat)
        {
            if (transform.position.y < waterHeight + 0.5f)
            {
                transform.position = new Vector3(transform.position.x, waterHeight + 0.5f, transform.position.z);
            }
        }

        if (onBoat)
            transform.position = boatCamPos.position;
    }
}
