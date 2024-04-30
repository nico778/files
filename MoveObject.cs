using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveObject : MonoBehaviour
{
    public float speed = 5.0f; // Speed of the cube movement

    void Update()
    {
        float moveHorizontal = Input.GetAxis("Horizontal") * speed * Time.deltaTime; // Calculate horizontal movement
        float moveVertical = Input.GetAxis("Vertical") * speed * Time.deltaTime; // Calculate vertical movement

        // Update the position of the cube
        transform.position = new Vector3(transform.position.x + moveHorizontal, transform.position.y, transform.position.z + moveVertical);
    }
}
