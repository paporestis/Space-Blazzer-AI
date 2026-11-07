using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement instance;


    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private ShipAgent shipAgent;

    private Material defaultMaterial;
    [SerializeField] private Material whiteMaterial;

    private Vector2 playerDirection;
    [SerializeField] private float moveSpeed;
    public float boost = 1f;
    private float boostPower = 4f;
    private bool isBoosting = false;

    [SerializeField] private float fuel;
    [SerializeField] private float maxFuel;
    [SerializeField] private float fuelRegen;

    [SerializeField] private float health;
    [SerializeField] private float maxHealth;
    [SerializeField] private GameObject destroyEffect;
    [SerializeField] private ParticleSystem engineEffect;

    public float HealthRatio => maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 0f;

    private void Awake()
    {
        shipAgent = GetComponent<ShipAgent>();

        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        };
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMaterial = spriteRenderer.material;

        fuel = maxFuel;
        UIController.Instance.UpdateFuelSlider(fuel, maxFuel);
        health = maxHealth;
        UIController.Instance.UpdateHealthSlider(health, maxHealth);
        UIController.Instance.UpdatePointsText(0);
    }


    void Update()
    {
        if (shipAgent != null && shipAgent.IsControllingShip)
        {
            return;
        }

        if (Time.timeScale > 0)
        {
            float directionX = Input.GetAxisRaw("Horizontal");
            float directionY = Input.GetAxisRaw("Vertical");

            animator.SetFloat("MoveX", directionX);
            animator.SetFloat("MoveY", directionY);

            playerDirection = new Vector2(directionX, directionY).normalized;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Fire2"))
            {
                EnterBoost();
            }
            else if (Input.GetKeyUp(KeyCode.Space) || Input.GetButtonUp("Fire2"))
            {
                ExitBoost();
            }

            if (Input.GetKeyDown(KeyCode.RightShift) || Input.GetButtonDown("Fire1"))
            { 
                PhaserWeapon.Instance.Shoot();
            }
        }
    }
    void FixedUpdate()
    {
        if (shipAgent == null || !shipAgent.IsControllingShip)
        {
            rb.linearVelocity = new Vector2(playerDirection.x * moveSpeed, playerDirection.y * moveSpeed);
        }


        if (isBoosting)
        {
            if (fuel >= 0.2f) fuel -= 0.2f;
            else
            {
                ExitBoost();
            }
        }
        else
        {
            if (fuel < maxFuel)
            { 
                fuel += fuelRegen; 
            }
        }
        UIController.Instance.UpdateFuelSlider(fuel, maxFuel);
    }

    public void EnterBoost()
    {
        if (fuel > 10)
        {
            animator.SetBool("Boosting", true);
            boost = boostPower;
            isBoosting = true;
            engineEffect.Play();
        }
    }

    public void ExitBoost()
    {
        animator.SetBool("Boosting", false);
        boost = 1f; 
        isBoosting = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            TakeDamage(1);            
        }
    }

    private void TakeDamage(int damage)
    {
        health -= damage;
        UIController.Instance.UpdateHealthSlider(health, maxHealth);

        if (shipAgent != null && shipAgent.IsControllingShip)
        {
            shipAgent.OnPlayerDamaged();
            if (health <= 0)
            {
                shipAgent.OnPlayerDestroyed();
                return;
            }
        }

        AudioManager.Instance.PlaySound(AudioManager.Instance.block);
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            if (defaultMaterial == null)
            {
                defaultMaterial = spriteRenderer.material;
            }

            spriteRenderer.material = whiteMaterial;
            StartCoroutine(ResetMaterial());
        }

        if (health <= 0)
        {
            boost = 0;
            gameObject.SetActive(false);
            Instantiate(destroyEffect, transform.position, transform.rotation);
            GameManager.Instance.GameOver();
            AudioManager.Instance.PlaySound(AudioManager.Instance.fire);
        }
    }

    public void ResetForAgentEpisode()
    {
        health = maxHealth;
        UIController.Instance.UpdateHealthSlider(health, maxHealth);
        if (spriteRenderer != null && defaultMaterial != null)
        {
            spriteRenderer.material = defaultMaterial;
        }
    }

    IEnumerator ResetMaterial()
    {
        yield return new WaitForSeconds(0.2f);
        if (spriteRenderer != null && defaultMaterial != null)
        {
            spriteRenderer.material = defaultMaterial;
        }
    }
}
