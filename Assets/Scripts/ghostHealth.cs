using UnityEngine;

public class ghostHealth : MonoBehaviour
{
    [SerializeField] int maxHealth = 50;

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;

        Debug.Log(
            gameObject.name + " spawned with "
            + currentHealth + " HP."
        );
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log(
            gameObject.name
            + " took "
            + damage
            + " damage. HP: "
            + currentHealth
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " died!");

        Destroy(gameObject);
    }
}