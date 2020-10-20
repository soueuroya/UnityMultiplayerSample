using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 15.0f;
    [SerializeField] float rotateSpeed = 120.0f;

    public bool isSmaeID = false;
    public string ID = null;

    public NetworkClient networkClient;

    private void Awake()
    {
        networkClient = FindObjectOfType<NetworkClient>();
    }

    void Start()
    {
        if (isSmaeID)
            InvokeRepeating("UpdateInput", 1, Utility.interval);
    }

    void Update()
    {
        if (!isSmaeID)
            return;
        ProcessInput();
    }

    private void ProcessInput()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.Rotate(Vector3.up * -rotateSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.Rotate(Vector3.up * +rotateSpeed * Time.deltaTime);
        }
    }

    public void SetPlayer(string id, Vector3 pos, Vector3 rot, Color color)
    {
        ID = id;

        if (networkClient.playerID == id)
            isSmaeID = true;

        transform.position = pos;
        transform.eulerAngles = rot;
        this.GetComponent<Renderer>().material.color = new Color(color.r, color.g, color.b);
    }

    public void UpdateInput()
    {
        networkClient.UpdatePlayer(gameObject);
    }

}
