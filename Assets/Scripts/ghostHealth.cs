using UnityEngine;

public class ghostHealth : MonoBehaviour, IDamageable
{
    [SerializeField] int maxHealth = 50;

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void Damage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}