using UnityEngine;

public class Movement : MonoBehaviour
{
    private Quaternion myRotation;
    private Vector2 myScale;
    [SerializeField] private Camera myCamera;
    private Animator myAnimator;
    private SpriteRenderer mySR;
    private Rigidbody2D rb;

    //Jump settings
    private float jumpForce = 6f;
    //lower = shorter tap jump
    private float jumpCutMultiplier = 0.05f;
    private float h;
    private bool canJump;
    private int jumpCount;

    //Dash settings
    [Header("Dash")]
    [SerializeField] private HUDController hud;
    //stamina used per dash
    [SerializeField] private float dashStaminaCost = 50f;
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCooldown = 0.6f;
    private bool isDashing;
    private float dashTimeLeft;
    private float nextDashTime;
    private float defaultGravity;

    void Start()
    {
        myScale = transform.localScale;
        myRotation = transform.rotation;
        myAnimator = GetComponent<Animator>();
        mySR = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        defaultGravity = rb.gravityScale;
        jumpCount = 2;
        PlayAnim("Idle");
    }

    void Update()
    {
        //Don't react to input while the settings popup has the game paused
        if (Time.timeScale == 0f)
        {
                        return;
        }

        h = Input.GetAxis("Horizontal");

        //Dash: UseStamina is checked last, so stamina is only spent if the dash actually happens
        if (Input.GetKeyDown(KeyCode.E) && !isDashing && Time.time >= nextDashTime && hud != null && hud.UseStamina(dashStaminaCost))
        {
            isDashing = true;
            dashTimeLeft = dashDuration;
            nextDashTime = Time.time + dashCooldown;
            rb.gravityScale = 0f; // no dropping mid-dash
        }

        if (jumpCount > 0)
        {
            canJump = true;
        }

        //Skip the "landed" check while dashing, or a mid-air dash (vertical speed 0) would refill your jumps
        else if (!isDashing && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            canJump = true;
            jumpCount = 2;
        }

        else
        {
            canJump = false;
        }

        //Tap jump
        if (Input.GetKeyDown(KeyCode.W) && canJump && !isDashing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpCount--;
        }

        //Hold Jump
        if (Input.GetKeyUp(KeyCode.W) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        bool inAir = Mathf.Abs(rb.linearVelocity.y) > 0.01f;

        if (Input.GetKey(KeyCode.D))
        {
            mySR.flipX = false;
        }

        else if (Input.GetKey(KeyCode.A))
        {
            //reuse the right-facing animation, mirrored
            mySR.flipX = true;           
        }

        //Pick an animation
        if (isDashing)
        {
            //swap for a "Dash" state if you make one
            PlayAnim("Moving Right");    
        }

        else if (inAir)
        {
            float t = Mathf.InverseLerp(jumpForce, -jumpForce, rb.linearVelocity.y);
            myAnimator.speed = 0f;
            myAnimator.Play("Jump", 0, Mathf.Min(t, 0.99f));
        }

        else if (Input.GetKey(KeyCode.S))
        {
            PlayAnim("Crouch");
        }

        else if (Input.GetKey(KeyCode.D))
        {
            PlayAnim("Moving Right");
        }

        else if (Input.GetKey(KeyCode.A))
        {
            PlayAnim("Moving Right");
        }

        else
        {
            PlayAnim("Idle");
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll < 0)
        {
            myCamera.orthographicSize++;
        }

        else if (scroll > 0)
        {
            myCamera.orthographicSize--;
        }

        myCamera.orthographicSize = Mathf.Max(myCamera.orthographicSize, 1f);

        transform.localScale = myScale;
        transform.rotation = myRotation;
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            float facing = mySR.flipX ? -1f : 1f;
            rb.linearVelocity = new Vector2(facing * dashSpeed, 0f);

            dashTimeLeft -= Time.fixedDeltaTime;
            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
                rb.gravityScale = defaultGravity;
            }

            //skip normal movement during the dash
            return; 
        }

        //Speed comes from the settings menu slider
        rb.linearVelocity = new Vector2(h * GameSettings.MoveSpeed, rb.linearVelocity.y);
    }

    //Only switches animation if it's different from the one already playing
    private void PlayAnim(string animName)
    {
        myAnimator.speed = 1f;
        if (myAnimator.GetCurrentAnimatorStateInfo(0).IsName(animName))
        {
            return;
        }

        myAnimator.Play(animName);
    }
}