using UnityEngine;

public class Boom : MonoBehaviour
{
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        Destroy(gameObject, animator.GetCurrentAnimatorStateInfo(0).length);
    }

    void Update()
    {
        float moveY = (GameManager.Instance.worldSpeed * PlayerMovement.instance.boost) * Time.deltaTime;
        transform.position += new Vector3(0, -moveY);
    }
}
