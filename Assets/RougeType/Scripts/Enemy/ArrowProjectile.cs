// ArrowProjectile.cs

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ArrowProjectile : MonoBehaviour
{
    private int damage;
    private float speed;

    public void Initialize(int dmg, float spd)
    {
        damage = dmg;
        speed = spd;
    }

    void Update()
    {
        transform.position += -transform.right * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            Wall wall = other.GetComponent<Wall>();
            if (wall != null)
            {
                wall.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
