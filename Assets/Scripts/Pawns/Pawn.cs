using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class Pawn : MonoBehaviour, ISelectable
{
    [Header("Pawn Info")]
    [SerializeField] private string pawnName = "Colonist";
    [SerializeField] private float moveSpeed = 3.5f;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    
    [Header("Work")]
    [SerializeField] private float workSpeed = 1f;
    [SerializeField] private float workRange = 2f;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private Color selectedColor = Color.cyan;
    
    [Header("UI")]
    [SerializeField] private GameObject nameTagPrefab;
    [SerializeField] private GameObject healthBarPrefab;
    
    private NavMeshAgent agent;
    private bool isSelected = false;
    private GameObject indicatorInstance;
    private Renderer pawnRenderer;
    private Color originalColor;
    
    // UI
    private PawnNameTag nameTag;
    private PawnHealthBar healthBar;
    
    // State
    private Vector3 targetPosition;
    private bool hasDestination = false;
    
    // Jobs
    private Job currentJob;
    private bool isWorking = false;
    private float jobSearchInterval = 1f;
    private float jobSearchTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        pawnRenderer = GetComponentInChildren<Renderer>();
        
        if (pawnRenderer != null)
        {
            originalColor = pawnRenderer.material.color;
        }
        
        agent.speed = moveSpeed;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        
        currentHealth = maxHealth;
    }

    void Start()
    {
        CreateSelectionIndicator();
        CreateUI();
    }

    void Update()
    {
        // Manual movement (only if no job!)
        if (hasDestination && currentJob == null && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    OnReachedDestination();
                }
            }
        }
        
        // Job system
        UpdateJobSystem();
        
        // DEBUG
        if (isSelected && Keyboard.current != null)
        {
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                TakeDamage(20f);
            }
            
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                Heal(15f);
            }
        }
    }

    #region Job System
    
    void UpdateJobSystem()
    {
        // If has job, work on it FIRST!
        if (currentJob != null)
        {
            WorkOnCurrentJob();
            return;  // Don't search for new job while working!
        }
        
        // Only search if NO job, not moving, and not working
        if (currentJob == null && !hasDestination && !isWorking)
        {
            jobSearchTimer += Time.deltaTime;
            
            if (jobSearchTimer >= jobSearchInterval)
            {
                jobSearchTimer = 0f;
                SearchForJob();
            }
        }
    }

    void SearchForJob()
    {
        if (JobManager.Instance == null) return;
        if (currentJob != null) return;  // Safety check
        
        Job job = JobManager.Instance.GetBestJobForPawn(this);
        
        if (job != null)
        {
            if (JobManager.Instance.AssignJobToPawn(job, this))
            {
                currentJob = job;
                MoveToJob(job);
                Debug.Log($"[{pawnName}] Assigned to {job.jobType} job at {job.workLocation}");
            }
        }
    }

    void MoveToJob(Job job)
    {
        // Don't use MoveTo() - it would cancel the job!
        targetPosition = job.workLocation;
        agent.SetDestination(job.workLocation);
        hasDestination = true;
    }

    void WorkOnCurrentJob()
    {
        if (currentJob == null) return;
        
        // Check if reached work location
        float distance = Vector3.Distance(transform.position, currentJob.workLocation);
        
        if (distance <= workRange)
        {
            // Stop moving
            if (hasDestination)
            {
                agent.ResetPath();
                hasDestination = false;
            }
            
            // Start working
            if (!isWorking)
            {
                isWorking = true;
                currentJob.StartJob();
                Debug.Log($"[{pawnName}] Started working on {currentJob.jobType}");
            }
            
            // Do work
            currentJob.DoWork(workSpeed * Time.deltaTime);
            
            // Check if completed
            if (currentJob.isCompleted)
            {
                FinishJob();
            }
        }
        else
        {
            // Still moving to job location
            if (!hasDestination)
            {
                // Re-path if needed
                agent.SetDestination(currentJob.workLocation);
                hasDestination = true;
            }
        }
    }

    void FinishJob()
    {
        Debug.Log($"[{pawnName}] Finished {currentJob.jobType} job!");
        currentJob = null;
        isWorking = false;
        jobSearchTimer = 0f;  // Reset timer to search for new job immediately
    }

    public void CancelCurrentJob()
    {
        if (currentJob != null)
        {
            JobManager.Instance.ReleaseJob(currentJob);
            currentJob = null;
            isWorking = false;
        }
    }

    public Job GetCurrentJob() => currentJob;
    public bool IsWorking() => isWorking;
    
    #endregion

    #region ISelectable Implementation
    
    public void OnSelected()
    {
        isSelected = true;
        
        if (indicatorInstance != null)
        {
            indicatorInstance.SetActive(true);
        }
        
        if (pawnRenderer != null)
        {
            pawnRenderer.material.color = selectedColor;
        }
    }

    public void OnDeselected()
    {
        isSelected = false;
        
        if (indicatorInstance != null)
        {
            indicatorInstance.SetActive(false);
        }
        
        if (pawnRenderer != null)
        {
            pawnRenderer.material.color = originalColor;
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }
    
    #endregion

    #region Movement
    
    public void MoveTo(Vector3 destination)
    {
        // Cancel current job if manually moving
        if (currentJob != null)
        {
            CancelCurrentJob();
        }
        
        targetPosition = destination;
        agent.SetDestination(destination);
        hasDestination = true;
    }

    void OnReachedDestination()
    {
        hasDestination = false;
    }

    public bool IsMoving()
    {
        return hasDestination && agent.velocity.sqrMagnitude > 0.1f;
    }
    
    #endregion

    #region Health
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    void Die()
    {
        Debug.Log($"{pawnName} died!");
        CancelCurrentJob();
    }

    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    
    #endregion

    #region Visual
    
    void CreateSelectionIndicator()
    {
        if (selectionIndicator != null)
        {
            indicatorInstance = Instantiate(selectionIndicator, transform);
            indicatorInstance.transform.localPosition = Vector3.down * 0.45f;
        }
        else
        {
            indicatorInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicatorInstance.name = "SelectionIndicator";
            indicatorInstance.transform.SetParent(transform);
            indicatorInstance.transform.localPosition = Vector3.down * 0.45f;
            indicatorInstance.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
            
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = selectedColor;
            indicatorInstance.GetComponent<Renderer>().material = mat;
            
            Destroy(indicatorInstance.GetComponent<Collider>());
        }
        
        indicatorInstance.SetActive(false);
    }

    void CreateUI()
    {
        if (nameTagPrefab != null)
        {
            GameObject nameTagObj = Instantiate(nameTagPrefab);
            nameTag = nameTagObj.GetComponent<PawnNameTag>();
            if (nameTag != null)
            {
                nameTag.Setup(transform, pawnName);
            }
        }
        
        if (healthBarPrefab != null)
        {
            GameObject healthBarObj = Instantiate(healthBarPrefab);
            healthBar = healthBarObj.GetComponent<PawnHealthBar>();
            if (healthBar != null)
            {
                healthBar.Setup(transform, currentHealth, maxHealth);
            }
        }
    }
    
    #endregion

    public string GetPawnName() => pawnName;
    public Vector3 GetTargetPosition() => targetPosition;
}