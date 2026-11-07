using UnityEngine;

public class PhaserBullet : MonoBehaviour
{
    private void Update()
    {
        transform.position += new Vector3(0f, PhaserWeapon.Instance.speed * Time.deltaTime);
        if (transform.position.y > 5.5)
        {
            if (ShipAgent.Instance != null && ShipAgent.Instance.IsControllingShip)
            {
                ShipAgent.Instance.OnShotMissed();
            }

            gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (ShipAgent.Instance != null && ShipAgent.Instance.IsControllingShip)
            {
                ShipAgent.Instance.OnAsteroidHit();
            }

            gameObject.SetActive(false);
        }
    }
}
