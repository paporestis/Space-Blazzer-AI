using UnityEngine;

public class MenuParallax : MonoBehaviour
{
    [SerializeField] private float movespeed;
    float backgroundWidth;
    void Start()
    {
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        backgroundWidth = sprite.texture.width / sprite.pixelsPerUnit;
    }

    void Update()
    {
        float moveY = movespeed * Time.deltaTime;
        transform.position += new Vector3(0, moveY);
        if (Mathf.Abs(transform.position.y) - backgroundWidth * 0.6 > 0)
        {
            transform.position = new Vector3(transform.position.x, 0f);
        }
    }
}
