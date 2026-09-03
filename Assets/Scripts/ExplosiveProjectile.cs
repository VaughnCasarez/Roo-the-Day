using UnityEngine;

public class ExplosiveProjectile : MonoBehaviour
{
    [SerializeField] float explosionRadius = 4f;
    [SerializeField] float explosionForce = 700f;
    [SerializeField] GameObject explosionEffect;
    [SerializeField] float lifetime = 10f;

    bool hasExploded;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode(collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
    }

    void OnTriggerEnter(Collider other)
    {
        Explode(transform.position);
    }

    void Explode(Vector3 position)
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;

        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, position, Quaternion.identity);
            Destroy(effect, 5f);
        }

        Collider[] affectedColliders = Physics.OverlapSphere(position, explosionRadius);
        foreach (Collider affectedCollider in affectedColliders)
        {
            if (affectedCollider.attachedRigidbody != null)
            {
                affectedCollider.attachedRigidbody.AddExplosionForce(
                    explosionForce,
                    position,
                    explosionRadius,
                    1f,
                    ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }
}