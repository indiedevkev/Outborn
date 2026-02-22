using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class JobManager : MonoBehaviour
{
    public static JobManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float jobCheckInterval = 0.5f;
    
    private List<Job> availableJobs = new List<Job>();
    private List<Job> activeJobs = new List<Job>();
    private List<Job> completedJobs = new List<Job>();
    
    private float jobCheckTimer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        jobCheckTimer += Time.deltaTime;
        
        if (jobCheckTimer >= jobCheckInterval)
        {
            jobCheckTimer = 0f;
            CleanupJobs();
        }
    }

    #region Add/Remove Jobs
    
    public void AddJob(Job job)
    {
        if (job == null) return;
        
        availableJobs.Add(job);
        Debug.Log($"Job added: {job.jobType} at {job.workLocation}");
    }

    public void RemoveJob(Job job)
    {
        availableJobs.Remove(job);
        activeJobs.Remove(job);
    }

    public void CancelJob(Job job)
    {
        job.CancelJob();
        RemoveJob(job);
    }
    
    #endregion

    #region Get Jobs
    
    public Job GetBestJobForPawn(Pawn pawn)
    {
        if (availableJobs.Count == 0) return null;
        
        // Sort by priority and distance
        Job bestJob = availableJobs
            .Where(j => j.CanPawnDoJob(pawn))
            .OrderByDescending(j => j.priority)
            .ThenBy(j => Vector3.Distance(pawn.transform.position, j.workLocation))
            .FirstOrDefault();
        
        return bestJob;
    }

    public List<Job> GetAvailableJobs()
    {
        return availableJobs.Where(j => !j.isAssigned && !j.isCompleted && !j.isCancelled).ToList();
    }

    public List<Job> GetJobsOfType(JobType type)
    {
        return availableJobs.Where(j => j.jobType == type).ToList();
    }
    
    #endregion

    #region Job Assignment
    
    public bool AssignJobToPawn(Job job, Pawn pawn)
    {
        if (job == null || pawn == null) return false;
        if (!job.CanPawnDoJob(pawn)) return false;
        
        if (job.AssignTo(pawn))
        {
            availableJobs.Remove(job);
            activeJobs.Add(job);
            return true;
        }
        
        return false;
    }

    public void ReleaseJob(Job job)
    {
        if (job == null) return;
        
        job.Release();
        activeJobs.Remove(job);
        availableJobs.Add(job);
    }
    
    #endregion

    #region Cleanup
    
    void CleanupJobs()
    {
        // Move completed/cancelled jobs
        for (int i = activeJobs.Count - 1; i >= 0; i--)
        {
            Job job = activeJobs[i];
            
            if (job.isCompleted || job.isCancelled)
            {
                activeJobs.RemoveAt(i);
                completedJobs.Add(job);
            }
        }
        
        for (int i = availableJobs.Count - 1; i >= 0; i--)
        {
            Job job = availableJobs[i];
            
            if (job.isCancelled)
            {
                availableJobs.RemoveAt(i);
            }
        }
        
        // Clear old completed jobs (keep last 100)
        if (completedJobs.Count > 100)
        {
            completedJobs.RemoveRange(0, completedJobs.Count - 100);
        }
    }
    
    #endregion

    #region Debug
    
    public int GetAvailableJobCount() => availableJobs.Count;
    public int GetActiveJobCount() => activeJobs.Count;
    public int GetCompletedJobCount() => completedJobs.Count;
    
    #endregion
}