using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [HideInInspector]
    public Vector3 currentPosition;
    private Vector3 currentRotation;
    private NetworkMan networkManager;

    [SerializeField] float speed = 1;
    [SerializeField] float rotSpeed = 10;


    
    void Start()
    {
        networkManager = GameObject.FindObjectOfType<NetworkMan>();

        InvokeRepeating("UpdateTransformToServer", 1, 0.03f);
    }

   
    void Update()
    {
        InputManager();
    }


    private void InputManager()
    {
        if (Input.GetKey(KeyCode.A)) currentPosition -= new Vector3(speed * Time.deltaTime, 0, 0);
        
        if (Input.GetKey(KeyCode.D)) currentPosition += new Vector3(speed * Time.deltaTime, 0, 0);

        if (Input.GetKey(KeyCode.W)) currentPosition += new Vector3(0, speed * Time.deltaTime, 0);

        if (Input.GetKey(KeyCode.S)) currentPosition -= new Vector3(0, speed * Time.deltaTime, 0);

        if (Input.GetKey(KeyCode.J)) currentRotation += new Vector3(0, rotSpeed * Time.deltaTime, 0);

        if (Input.GetKey(KeyCode.K)) currentRotation -= new Vector3(0, rotSpeed * Time.deltaTime, 0);

    }

    void UpdateTransformToServer()
    {
        networkManager.UpdateTransformToServer(currentPosition, currentRotation);
    }
}
