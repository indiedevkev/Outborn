using UnityEngine;

public enum JobType
{
    None,
    Build,
    Haul,
    Mine,
    Harvest
}

public enum JobPriority
{
    Low = 1,
    Normal = 5,
    High = 10,
    Critical = 20
}

public abstract class Job
{
    public string jobID;
    public JobType jobType;
    public JobPriority priority;
    public Vector3 workLocation;
    public bool isAssigned;
    public bool isCompleted;
    public bool isCancelled;
    
    protected Pawn assignedPawn;
    protected float workRequired;
    protected float workDone;

    public Job(JobType type, Vector3 location, float workAmount = 1f)
    {
        jobID = System.Guid.NewGuid().ToString();
        jobType = type;
        workLocation = location;
        workRequired = workAmount;
        workDone = 0f;
        priority = JobPriority.Normal;
        isAssigned = false;
        isCompleted = false;
        isCancelled = false;
    }

    // Assign job to pawn
    public virtual bool AssignTo(Pawn pawn)
    {
        if (isAssigned) return false;
        
        assignedPawn = pawn;
        isAssigned = true;
        return true;
    }

    // Release job from pawn
    public virtual void Release()
    {
        assignedPawn = null;
        isAssigned = false;
    }

    // Can this pawn do this job?
    public virtual bool CanPawnDoJob(Pawn pawn)
    {
        return pawn != null && !isAssigned && !isCompleted && !isCancelled;
    }

    // Start working on job
    public virtual void StartJob()
    {
        Debug.Log($"Job {jobID} ({jobType}) started by {assignedPawn?.GetPawnName()}");
    }

    // Do work (called per frame or tick)
    public virtual void DoWork(float amount)
    {
        workDone += amount;
        
        if (workDone >= workRequired)
        {
            CompleteJob();
        }
    }

    // Complete job
    public virtual void CompleteJob()
    {
        isCompleted = true;
        Debug.Log($"Job {jobID} ({jobType}) completed!");
    }

    // Cancel job
    public virtual void CancelJob()
    {
        isCancelled = true;
        Release();
        Debug.Log($"Job {jobID} ({jobType}) cancelled!");
    }

    // Get progress (0-1)
    public float GetProgress()
    {
        return Mathf.Clamp01(workDone / workRequired);
    }

    // Get assigned pawn
    public Pawn GetAssignedPawn()
    {
        return assignedPawn;
    }
}