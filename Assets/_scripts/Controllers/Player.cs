using Elysium.Combat;
using Elysium.Core;
using Elysium.UI.ProgressBar;
using Elysium.Utils.Attributes;
using Elysium.Utils.Timers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour, IPushable, IPlayer
{
    [SerializeField, ReadOnly] private Vector2 input = Vector2.zero;
    [SerializeField] private int health = 100;
    [SerializeField] private int damage = 10;
    [SerializeField] private int heal = 10;
    [SerializeField] private float enemyPushForce = 36000f;
    [SerializeField] private Ethereal ethereal;
    [SerializeField] private LongValueSO playerScore = default;
    [SerializeField] private GameObject deathAnim = default;
    [RequireInterface(typeof(IRangeIndicator))]
    [SerializeField] private UnityEngine.Object[] indicators = new UnityEngine.Object[0];

    private Canvas canvas = default;
    private Movement movement = default;
    private Rigidbody2D rb = default;
    private Collider[] colliders = default;
    private HealthController healthController = default;
    private Boss boss = default;
    private SpriteRenderer renderer = default;

    public bool airJump = false; 

    private IEtherealEffect fireEffect = null;
    private IEtherealEffect waterEffect = null;
    private IEtherealEffect windEffect = null;
    private IEtherealEffect vineEffect = null;
    private IEtherealEffect earthEffect = null;

    [Separator("Hotbar", true)]
    [SerializeField] UI_HotbarSlot fireHotbar = null;
    [SerializeField] UI_HotbarSlot waterHotbar = null;
    [SerializeField] UI_HotbarSlot windHotbar = null;
    [SerializeField] UI_HotbarSlot vineHotbar = null;
    [SerializeField] UI_HotbarSlot earthHotbar = null;
    [SerializeField] UI_ProgressBar bossBar = null;

    [SerializeField] GameObject firesoulpopup = null;
    [SerializeField] GameObject watersoulpopup = null;
    [SerializeField] GameObject windsoulpopup = null;
    [SerializeField] GameObject vinesoulpopup = null;
    [SerializeField] GameObject earthsoulpopup = null;

    [Separator("Particles", true)]
    [SerializeField] GameObject[] particles = new GameObject[4];

    [Separator("Fire Effect", true)]
    [SerializeField] GameObject fireExplosionHit = null;
    [SerializeField] GameObject fireExplosionTick = null;

    [Separator("Water Effect", true)]
    [SerializeField] GameObject healEffectTick = null;

    [Separator("Vine Effect", true)]

    private IEtherealEffect selectedEffect = null;

    public ModelController Anim { get; set; }
    public Rigidbody2D Rigidbody => rb;
    public Movement Movement => movement;
    public HealthController HealthController => healthController;
    public Vector2? Destination { get; set; }    
    public Vector2 Checkpoint { get; set; }

    public void Push(float _force, Vector2 _direction)
    {
        Debug.DrawRay(transform.position, _direction * _force, Color.red);
        rb.velocity = Vector2.zero;
        rb.AddForce(_direction * _force);
    }

    public void SetParticles(int _index)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].SetActive(i == _index);
        }
    }

    public void ActivateCollisions(bool _active)
    {
        gameObject.layer = _active ? LayerMask.NameToLayer("Player") : LayerMask.NameToLayer("Intangible");
        foreach (Transform t in transform)
        {
            t.gameObject.layer = _active ? LayerMask.NameToLayer("Player") : LayerMask.NameToLayer("Intangible");
        }            

        var c = renderer.color;
        c.a = _active ? 1f : 0.3f;
        renderer.color = c;
    }

    public void ReceiveReward(RewardPackage _reward)
    {
        playerScore.Value += _reward.Score;
    }    

    public void CreateEarthEffect()
    {
        earthEffect = new EarthEffect(
                    this,
                    ethereal,
                    new Color32(117, 59, 11, 100),
                    new Color32(117, 59, 11, 255),
                    4,
                    Ethereal.BASE_TIME_IN_FORM,
                    4f
                    );
        earthHotbar.SetupCooldownBar(earthEffect as IFillable);
        earthsoulpopup.SetActive(true);
        Checkpoint = transform.position;
    }

    public void CreateVineEffect()
    {
        vineEffect = new VineEffect(
                    this,
                    ethereal,
                    Color.green,
                    Color.green,
                    3,
                    Ethereal.BASE_TIME_IN_FORM,
                    4f
                    );
        vineHotbar.SetupCooldownBar(vineEffect as IFillable);
        vinesoulpopup.SetActive(true);
        Checkpoint = transform.position;
    }

    public void CreateWindEffect()
    {
        windEffect = new WindEffect(
                    this,
                    ethereal,
                    Color.gray,
                    Color.gray,
                    2,
                    Ethereal.BASE_TIME_IN_FORM,
                    7f,
                    -2f,
                    7f,
                    36000f
                    );
        windHotbar.SetupCooldownBar(windEffect as IFillable);
        windsoulpopup.SetActive(true);
        Checkpoint = transform.position;
    }

    public void CreateWaterEffect()
    {
        waterEffect = new WaterEffect(
                    this,
                    ethereal,
                    Color.blue,
                    Color.blue,
                    1,
                    Ethereal.BASE_TIME_IN_FORM,
                    5f,
                    heal,
                    healEffectTick
                    );
        waterHotbar.SetupCooldownBar(waterEffect as IFillable);
        watersoulpopup.SetActive(true);
        Checkpoint = transform.position;
    }

    public void CreateFireEffect()
    {
        fireEffect = new FireEffect(
                    this,
                    ethereal,
                    Color.red,
                    Color.red,
                    0,
                    Ethereal.BASE_TIME_IN_FORM,
                    2f,
                    10f,
                    5f,
                    fireExplosionHit,
                    fireExplosionTick
                    );
        fireHotbar.SetupCooldownBar(fireEffect as IFillable);
        firesoulpopup.SetActive(true);
        Checkpoint = transform.position;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        canvas = FindObjectOfType<Canvas>();
        movement = GetComponent<Movement>();
        colliders = GetComponentsInChildren<Collider>();
        healthController = GetComponent<HealthController>();
        Anim = GetComponentInChildren<ModelController>();
        boss = FindObjectOfType<Boss>();
        renderer = GetComponentInChildren<SpriteRenderer>();

        healthController.MaxResource = new Elysium.Utils.RefValue<int>(() => health);
        healthController.Fill();
        healthController.OnDeath += Die;

        firesoulpopup.gameObject.SetActive(false);
        watersoulpopup.gameObject.SetActive(false);
        earthsoulpopup.gameObject.SetActive(false);
        windsoulpopup.gameObject.SetActive(false);
        vinesoulpopup.gameObject.SetActive(false);

        Checkpoint = transform.position;
    }
    
    protected virtual void Start()
    {
        ethereal.gameObject.SetActive(false);
    }

    protected virtual void Update()
    {
        if (boss != null) { bossBar.gameObject.SetActive(Vector2.Distance(transform.position, boss.transform.position) < boss.EngageRange); }

        if (Destination.HasValue) { AutomaticallyMoveToDestination(); }
        else { MoveBasedOnInput(); }

        DrawChangeStateUI(!ethereal.IsActive);
        if (selectedEffect != null && Input.GetMouseButtonDown(0)) 
        {
            if (!ethereal.IsActive && selectedEffect.IsAvailable) { DeployEthereal(); }
            else if (ethereal.IsDeployed) { RetrieveEthereal(); }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            selectedEffect = null;
            DeactivateAllHotbars();
        }

        // Different Spirits
        if (fireEffect != null && !ethereal.IsActive && fireEffect.IsAvailable && Input.GetKeyDown(KeyCode.Alpha1))
        { 
            selectedEffect = fireEffect;
            DeactivateAllHotbars();
            fireHotbar.Highlight(true);
        }

        if (vineEffect != null && !ethereal.IsActive && vineEffect.IsAvailable && Input.GetKeyDown(KeyCode.Alpha2)) 
        { 
            selectedEffect = vineEffect;
            DeactivateAllHotbars();
            vineHotbar.Highlight(true);
        }

        if (earthEffect != null && !ethereal.IsActive && earthEffect.IsAvailable && Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedEffect = earthEffect;
            DeactivateAllHotbars();
            earthHotbar.Highlight(true);
        }

        if (waterEffect != null && !ethereal.IsActive && waterEffect.IsAvailable && Input.GetKeyDown(KeyCode.Alpha4)) 
        { 
            selectedEffect = waterEffect;
            DeactivateAllHotbars();
            waterHotbar.Highlight(true);
        }

        if (windEffect != null && !ethereal.IsActive && windEffect.IsAvailable && Input.GetKeyDown(KeyCode.Alpha5))
        { 
            selectedEffect = windEffect;
            DeactivateAllHotbars();
            windHotbar.Highlight(true);
        }
    }

    private void AutomaticallyMoveToDestination()
    {
        Vector2 direction = Destination.Value - (Vector2)transform.position;
        movement.Move(direction.normalized);

        if (Vector2.Distance((Vector2)transform.position, Destination.Value) < 1.5f)
        {
            Destination = null;
            lastKnownPos = null;
            if (collisionTimer != null)
            {
                collisionTimer.End();
                collisionTimer.Dispose();
                collisionTimer = null;
            }
            rb.velocity = Vector2.zero;
            movement.MoveSpeed = 10f;
            ethereal.InvokePlayerArrival();
            return;
        }
    }

    private void MoveBasedOnInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = 0;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            y = 1;
            if(airJump) 
            {
                movement.IsGrounded = true;
                airJump = false;
            }
        }

        input = new Vector2(x, y);
        Anim.SetMoveSpeed(input.x);
        var jFloat = movement.IsGrounded ? 0 : 1;
        Anim.SetAnimatorFloat("jump", jFloat);
        movement.Move(input.x, input.y == 1);
    }    

    private void DeployEthereal()
    {
        ethereal.Deploy(this, selectedEffect);
    }

    private void RetrieveEthereal()
    {
        ethereal.Retrieve(this);
    }

    private void Die()
    {
        ethereal.ForceRetrieve(this);
        selectedEffect = null;
        DeactivateAllHotbars();

        Instantiate(deathAnim, transform.position, deathAnim.transform.rotation);

        var dt = Timer.CreateTimer(1f, () => !this, false);
        dt.OnEnd += () =>
        {
            RespawnAtCheckpoint();
        };
                
        gameObject.SetActive(false);
    }

    private void RespawnAtCheckpoint()
    {
        transform.position = Checkpoint;
        healthController.Fill();
        healthController.Ressurect();

        boss.FullReset();
        gameObject.SetActive(true);
    }

    private void DrawChangeStateUI(bool _active)
    {
        foreach (IRangeIndicator indicator in indicators)
        {
            indicator.Radius = 5f;
            indicator.SetActive(_active);
        }
    }

    private void DeactivateAllHotbars()
    {
        fireHotbar.Highlight(false);
        waterHotbar.Highlight(false);
        windHotbar.Highlight(false);
        vineHotbar.Highlight(false);
        earthHotbar.Highlight(false);
    }

    private void OnCollisionEnter2D(Collision2D _collision)
    {
        //var dealer = _collision.collider.transform.root.GetComponentInChildren<IDamageDealer>();
        //if (dealer != null)
        //{
        //    Vector2 direction = (Vector2)ethereal.transform.position - (Vector2)_collision.collider.transform.position;
        //    healthController.TakeDamage(dealer, dealer.Damage.Value);
        //    Push(enemyPushForce, direction.normalized);
        //    Anim.PlayAnimation("Hit");
        //}
    }

    Vector2? lastKnownPos = null;
    TimerInstance collisionTimer = null;
    private void OnCollisionStay2D(Collision2D _collision)
    {
        bool isInLayer = Movement.WhatIsGround.value == (Movement.WhatIsGround.value | (1 << _collision.gameObject.layer));
        if (Destination.HasValue && isInLayer)
        {
            if (!lastKnownPos.HasValue)
            {
                lastKnownPos = transform.position;
                collisionTimer = Timer.CreateTimer(0.2f, () => false, false);
                return;
            }

            if (lastKnownPos == transform.position && collisionTimer != null && collisionTimer.IsEnded)
            {
                Destination = null;
                lastKnownPos = null;
                collisionTimer.End();
                collisionTimer.Dispose();
                collisionTimer = null;
                ethereal.ForceRetrieve(this);
            }            
        }
    }
}
