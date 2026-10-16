using UnityEngine;

public class SlashDamage : MonoBehaviour
{
    [SerializeField] int damage = 25;

    private bool hasHit = false;

    void OnTriggerEnter(Collider other)
    {
        // This slash has already damaged something
        // Prevent one slash from damaging the same enemy multiple times
        // if multiple collider events occur before the slash is destroyed.
        if (hasHit)
        {
            return;
        }

        if (other.TryGetComponent(out IDamageable damageable))
        {
            hasHit = true;

            damageable.Damage(damage);

            Destroy(gameObject);
        }
    }
}