using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// loosely based on on https://github.com/LeandroDotta/Unity-SimpleHealthSystem/blob/main/Assets/Health%20System/Scripts/HealthManager.cs#L27

public class HealthManager: MonoBehaviour
{
    [SerializeField, Min(0)] private float _maxHealth = 100;
    private float _curHealth = 100;
    [Tooltip("strating health. set to 0 to set to max health automatically")]
    [SerializeField, Min(0)] private float _startingHealth;
    [SerializeField, Min(0)] private float _cooldown; // cooldown from damage. can just be 0for no cooldown.
    
    // events editable in inspector. these are for informing other scripts about events.
    /// <summary>
    /// arggs: sender, change in health
    /// </summary>
    [field: SerializeField] public UnityEvent<HealthManager, float> OnChangeHealth { get; private set; } = new UnityEvent<HealthManager, float>();
    [field: SerializeField] public UnityEvent<HealthManager> OnDeath { get; private set; } = new UnityEvent<HealthManager>();
    [field: SerializeField] public UnityEvent<HealthManager> OnCooldownStart { get; private set; } = new UnityEvent<HealthManager>();
    [field: SerializeField] public UnityEvent<HealthManager> OnCooldownEnd { get; private set; } = new UnityEvent<HealthManager>();

    private float _cdTimer; // track time for cooldowns
    private bool _isOnCD = false;
    private Coroutine _cdCoroutine;
    //public int ID { get; set; }
    
    public bool IsOnCD
    {
        get { return _isOnCD; }
        private set
        {
            if (value == _isOnCD) return;
            _isOnCD = value;
            if (_isOnCD)
                OnCooldownStart.Invoke(this); // initiate cooldown
            else
                OnCooldownEnd.Invoke(this); // initiate cooldown end
        }
    }

    /// <summary>
    /// Cooldown time (not timer) clamped to nonnegative
    /// </summary>
    public float CoolDown
    {
        get { return _cooldown; }
        set { _cooldown = Mathf.Max(value, 0f); }
    }

    public float CooldownTimer
    {
        get
        {
            return _isOnCD ? _cdTimer : 0f;
        }
    }
    
    /// <summary>
    /// health, clamps between 0 and maxhealth
    /// </summary>
    public float Health
    {
        get { return _curHealth; }
        private set { _curHealth = Mathf.Clamp(value, 0, _maxHealth); }
    }
    public bool IsDead
    {
        get { return Health <= 0; }
    }

    /// <summary>
    /// max health. lowers health to maxHealth if is higher.
    /// </summary>
    public float MaxHealth
    {
        get { return _maxHealth; }
        set
        {
            _maxHealth = Mathf.Max(value, 0f);
            if (Health > _maxHealth)
            {
                float prevHealth = Health;
                Health = _maxHealth;
                OnChangeHealth.Invoke(this, Health - prevHealth);
                if (IsDead) OnDeath.Invoke(this);
            }
        }
    }
    

    private void Awake()
    {
        Health = GetStartingHealth();
    }

    private void OnDisable()
    {
        // coroutines dont run when disabled
        _cdCoroutine = null;
        _isOnCD = false;
        _cdTimer = 0;
    }



    /// <summary>
    /// Amount of damage to deal to health
    /// </summary>
    /// <param name="amount"> positive amount of damage </param>
    /// <returns> false if negative input, on cooldown, or already died </returns>
    public bool Damage(float amount)
    {
        if (amount <= 0 || IsOnCD || IsDead) return false;
        float prevHealth = Health;
        Health =  Mathf.Clamp(Health - amount, 0, _maxHealth);
        float healthChange = Health -  prevHealth;
        OnChangeHealth.Invoke(this, healthChange);
        
        if (IsDead)
        {
            OnDeath.Invoke(this);
            return true;
        }
        if (_cooldown > 0 && isActiveAndEnabled)
            _cdCoroutine = StartCoroutine(CooldownCoroutine());
        return true;
    }

    /// <summary>
    /// Heals health
    /// </summary>
    /// <param name="amount"> positive amount to heal</param>
    /// <returns> false if nonpositive input or is dead already</returns>
    public bool Heal(float amount)
    {
        if (amount <= 0 || IsDead) return false;
        float prevHealth = Health;
        Health = Mathf.Min(Health + amount, _maxHealth);
        float healthChange = Health - prevHealth;
        
        if (healthChange <= 0) return false;
        
        OnChangeHealth.Invoke(this,  healthChange);
        return true;
    }

    /// <summary>
    /// restore starting health and clear cooldowne. revives. mostly for reactivating pooled object
    /// </summary>
    public void ResetHealth()
    {
        if (_cdCoroutine != null)
        {
            StopCoroutine(_cdCoroutine);
            _cdCoroutine = null;
        }

        _cdTimer = 0;
        IsOnCD = false;
        float prevHealth = Health;
        Health = GetStartingHealth();
        float healthChange = Health - prevHealth;
        if (!Mathf.Approximately(prevHealth, Health))
            OnChangeHealth.Invoke(this, healthChange);
    }

    // retrieve starting health, including previous state if didnt die
    private float GetStartingHealth()
    {
        return _startingHealth > 0 ? Mathf.Min(_startingHealth, _maxHealth) : _maxHealth;
    }


    private IEnumerator CooldownCoroutine()
    {
        _cdTimer = _cooldown;
        IsOnCD = true;
        while (_cdTimer > 0)
        {
            yield return null;
            _cdTimer -= Time.deltaTime;
        }
            
        _cdTimer = 0;
        _cdCoroutine = null;
        IsOnCD = false;

    }
}
