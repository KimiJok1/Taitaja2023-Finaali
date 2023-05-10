using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// Parts of the movement taken from https://github.com/KimiJok1/Taitaja-2023-Peliprojekti/blob/main/Assets/Scripts/PlayerController.cs
public class PlayerController : MonoBehaviour
{
    // Serialized objects for player
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject hitbox;
    [SerializeField] GameObject cameraPoint;
    [SerializeField] Rigidbody2D rigidBody;

    // Serialized movement variables
    [SerializeField] float speedMultiplier;
    [SerializeField] float jumpMultiplier;
    [SerializeField] float jumpLimit;

    // Energy variables
    [SerializeField] private float energy = 100;
    [SerializeField] public float maxEnergy = 100;

    // Energy multipliers
    [SerializeField] public float energyGainMultiplier = 1;
    [SerializeField] public float maxEnergyMultiplier = 1;

    // Inputs
    private float verticalInput;
    private float horizontalInput;

    // Movement variables
    private int jumpCount = 0;
    private bool isDead = false;
    private bool isFalling = false;
    private bool isGrounded = false;
    private bool isAttacking = false;
    private bool isOnCooldown = false;
    private float targetDirection = 1;

    // Objects for player
    private Animator animator;
    private SpriteRenderer sprRenderer;

    // Sound manager
    private PlayerSoundManager soundManager;

    // Start time
    float startTime;

    // Unused function to get animation length of any animation from the controller. Will probably be used someday.
    float GetAnimationLength(string name)
    {
        var animController = animator.runtimeAnimatorController;
        var clips = animController.animationClips;

        // Go through all clips and find the correct clip.
        foreach (AnimationClip clip in clips)
            if (clip.name == name) return clip.length;

        return 1;
    }

    IEnumerator Death()
    {
        // Freeze player, set to dead and run death animation
        isDead = true;
        rigidBody.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        animator.SetTrigger("Die");

        yield return new WaitForSeconds(3);
    }

    // Used for managing attacks
    IEnumerator Attack()
    {
        // Randomize animation
        int anim = UnityEngine.Random.Range(1,4);

        // Enable cooldown, play sound
        isOnCooldown = true;
        soundManager.PlaySound("Attack",anim);

        // (Debugging) show hitbox
        hitbox.GetComponent<SpriteRenderer>().enabled = true;

        // Freeze player position
        rigidBody.constraints = RigidbodyConstraints2D.FreezePosition | RigidbodyConstraints2D.FreezeRotation;

        // Play animation and wait for it to finish
        animator.SetTrigger("Attack" + anim);

        // Wait until animation starts
        yield return new WaitWhile(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0);

        // Enable hitbox collision check
        isAttacking = true;

        // Offset animation position
        transform.position = new Vector3(transform.position.x + (1.25f * targetDirection), transform.position.y, transform.position.z);
        hitbox.transform.localPosition = new Vector3((1.5f*targetDirection) - (1.25f * targetDirection),-0.95f,0);
        cameraPoint.transform.localPosition = new Vector3((1.25f * -targetDirection), 0f, 0f);

        // Wait for the animation length
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);

        // Reset animation position
        transform.position = new Vector3(transform.position.x - (1.25f * targetDirection), transform.position.y, transform.position.z);
        hitbox.transform.localPosition = new Vector3((1.5f*targetDirection),-0.95f,0);
        cameraPoint.transform.localPosition = new Vector3(0, 0f, 0f);

        // Unfreeze player position
        rigidBody.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Disable hitbox collision check
        isAttacking = false;

        // (debugging) hide hitbox, wait half a second
        hitbox.GetComponent<SpriteRenderer>().enabled = false;
        yield return new WaitForSeconds(0.5f);

        // Disable cooldown
        isOnCooldown = false;
    }

    // Called before attack to check attack type & to check cooldown
    void CheckAttack(string type)
    {
        if (isOnCooldown) return;
        if (isDead) return;

        switch(type)
        {
            case "Normal":
                StartCoroutine("Attack");
                break;
            case "Heavy":
                // StartCoroutine("HeavyAttack");
                break;
        }
    }

    public void GainEnergy(float amount)
    {
        if (isDead) return;

        // Add energy
        energy += (amount * energyGainMultiplier);
        
        // Energy limit
        if(energy > maxEnergy)
        {
            energy = maxEnergy;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // Run take damage animation and remove energy
        animator.SetTrigger("TakeDamage");
        energy -= amount;

        // Energy limit
        if(energy < 0)
        {
            energy = 0;
        }
    }

    void DamageOverTime()
    {
        energy -= .5f;
    }

    void Start()
    {
        // Get sprite animator, sprite renderer and sound manager
        animator = GetComponent<Animator>();
        sprRenderer = GetComponent<SpriteRenderer>();
        soundManager = GetComponent<PlayerSoundManager>();

        // Take damage over time
        InvokeRepeating("DamageOverTime", 1f, 1f);

        // Get start time
        startTime = Time.time;
    }

    void Update()
    {
        // Get inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        // Run these if player isn't dead
        if (!isDead)
        {
            if (horizontalInput != 0)
            {
                // Get direction multiplier
                targetDirection = horizontalInput < 0 ? -1 : 1;
                sprRenderer.flipX = targetDirection == 1 ? false : true;
            }

            // Set RigibBodys velocity based on input and speed multiplier
            rigidBody.velocity = new Vector2(Mathf.Abs(horizontalInput) * speedMultiplier * targetDirection, rigidBody.velocity.y);

            // Set running on animator if player is moving (TODO: Change this)
            animator.SetBool("Running",rigidBody.velocity.x != 0 && isGrounded);

            // If spacebar is pressed and player is grounded, set player's Y velocity to up direction multiplied by jump multiplier
            if (Input.GetKeyDown(KeyCode.Space) && (isGrounded || jumpCount < jumpLimit))
            {
                jumpCount++;
                isGrounded = false;
                rigidBody.velocity = Vector3.up * jumpMultiplier;

                // If first jump, play regular jump, else flip
                if (jumpCount == 1)
                {
                    animator.SetTrigger("Jump");
                }
                else
                {
                    animator.SetTrigger("JumpFlip");
                }
            }

            // If clicked, call for attack (normal)
            if (Input.GetMouseButtonDown(0))
                CheckAttack("Normal");

            // If right clicked, call for attack (heavy)
            if (Input.GetMouseButtonDown(1))
                CheckAttack("Heavy");

            if (Input.GetKeyDown(KeyCode.LeftControl))
                print("block");
        }

        // Update UI
        Transform EnergyBar = canvas.transform.Find("EnergyBar");
        EnergyBar.GetComponent<Slider>().value = (float)System.Math.Round(energy,0);
        EnergyBar.GetComponent<Slider>().maxValue = (float)System.Math.Round(maxEnergy * maxEnergyMultiplier,0);
        EnergyBar.Find("FillText").GetComponent<TMP_Text>().text = "Energy: " + System.Math.Round(energy,0) + "/" + System.Math.Round(maxEnergy * maxEnergyMultiplier,0);

        // Check player health
        if (System.Math.Round(energy,0) <= 0)
        {
            energy = 0;
            if (isDead) return;
            StartCoroutine("Death");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Compare tag to see what player is colliding with
        if (collision.gameObject.CompareTag("Ground")) 
        {
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
            jumpCount = isGrounded ? 0 : jumpCount;
            isFalling = isGrounded ? true : isFalling;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Compare tag to see what player is colliding with
        if (collision.gameObject.CompareTag("Ground")) 
        {
            isGrounded = false;
        }
    }
}
