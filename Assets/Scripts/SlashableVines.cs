using UnityEngine;

public class SlashableVines : MonoBehaviour, IDamageable
{
    public void Damage(int amount)
    {
        Destroy(gameObject);
    }
}
