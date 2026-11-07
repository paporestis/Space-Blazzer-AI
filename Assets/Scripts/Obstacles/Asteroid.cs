using System.Collections;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    private Material defaultMaterial;
    [SerializeField] private Material whiteMaterial;

    [SerializeField] private GameObject destroyEffect;
    [SerializeField] private int lives;
    [SerializeField] private Sprite[] Sprites;
    private bool isDestroyed;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        defaultMaterial = spriteRenderer.material;
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer.sprite = Sprites[Random.Range(0, Sprites.Length)];
        float pushX = Random.Range(1f, -1f);
        float pushY = Random.Range(0, -1f);

        rb.linearVelocity = new Vector2(pushX, pushY);
        float randomScale = Random.Range(0.6f, 1f);
        transform.localScale = new Vector2(randomScale, randomScale);
    }

    
    void Update()
    {
        float moveY = (GameManager.Instance.worldSpeed * PlayerMovement.instance.boost) * Time.deltaTime;
        transform.position += new Vector3(0, -moveY);
        if (transform.position.y < -8)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Bullet"))
        {
            bool destroyedByBullet = collision.gameObject.CompareTag("Bullet");
            spriteRenderer.material = whiteMaterial;
            StartCoroutine(ResetMaterial());
            AudioManager.Instance.PlayModifiedSound(AudioManager.Instance.hit);
            lives--;
            if (lives <= 0 && !isDestroyed)
            {
                isDestroyed = true;
                Instantiate(destroyEffect, transform.position, transform.rotation);
                AudioManager.Instance.PlayModifiedSound(AudioManager.Instance.boom2);
                UIController.Instance.AddPoint();
                if (destroyedByBullet && ShipAgent.Instance != null && ShipAgent.Instance.IsControllingShip)
                {
                    ShipAgent.Instance.OnAsteroidDestroyed();
                }

                Destroy(gameObject);
            }
        }
    }

    IEnumerator ResetMaterial()
    {
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.material = defaultMaterial;
    }
}
