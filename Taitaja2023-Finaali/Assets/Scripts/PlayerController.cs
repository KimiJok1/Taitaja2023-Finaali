using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Rigidbody2D rigidBody;

    [SerializeField] float speedMultiplier;
    [SerializeField] float jumpMultiplier;

    [SerializeField] private float verticalInput;
    [SerializeField] private float horizontalInput;

    [SerializeField] private bool isGrounded = true;

    void Start()
    {
        
    }

    void Update()
    {
        // Get inputs
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        // Get direction multiplier
        float dir = horizontalInput < 0 ? -1 : 1;

        // Set RigibBodys velocity based on input and speed multiplier
        rigidBody.velocity = new Vector2(Mathf.Abs(horizontalInput) * speedMultiplier * dir, rigidBody.velocity.y);

        // If spacebar is pressed and player is grounded, set player's Y velocity to up direction multiplied by jump multiplier
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            isGrounded = false;
            rigidBody.velocity = Vector3.up * jumpMultiplier;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) {
            bool checkDir = true;

            // Get all contact points for object
            ContactPoint2D[] allPoints = new ContactPoint2D[collision.contactCount];
            collision.GetContacts(allPoints);

            // Compare the points
            foreach (var i in allPoints)
                if (i.point.y > transform.position.y) 
                    checkDir = false;

            // Enable jumping again
            isGrounded = checkDir == true ? checkDir : isGrounded;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) {
            bool checkDir = true;

            // Get all contact points for object
            ContactPoint2D[] allPoints = new ContactPoint2D[collision.contactCount];
            collision.GetContacts(allPoints);

            // Compare the points
            foreach (var i in allPoints)
                if (i.point.y > transform.position.y) 
                    checkDir = false;

            // Enable jumping again
            isGrounded = checkDir == true ? checkDir : isGrounded;
        }
    }
}
