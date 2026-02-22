using UnityEngine;
using UnityEngine.InputSystem;

public class DebugManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    
    [Header("Debug Settings")]
    [SerializeField] private float damageAmount = 20f;
    [SerializeField] private float healAmount = 15f;

    void Update()
    {
        if (Keyboard.current == null) return;
        
        // T = Damage selected pawns
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            DamageSelectedPawns();
        }
        
        // H = Heal selected pawns
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            HealSelectedPawns();
        }
        
        // K = Kill selected pawns
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            KillSelectedPawns();
        }
    }

    void DamageSelectedPawns()
    {
        var selected = selectionManager.GetSelectedObjects();
        int count = 0;
        
        foreach (var selectable in selected)
        {
            MonoBehaviour mono = selectable as MonoBehaviour;
            if (mono != null)
            {
                Pawn pawn = mono.GetComponent<Pawn>();
                if (pawn != null)
                {
                    pawn.TakeDamage(damageAmount);
                    count++;
                }
            }
        }
        
        if (count > 0)
        {
            Debug.Log($"[DEBUG] Damaged {count} pawns for {damageAmount} damage");
        }
    }

    void HealSelectedPawns()
    {
        var selected = selectionManager.GetSelectedObjects();
        int count = 0;
        
        foreach (var selectable in selected)
        {
            MonoBehaviour mono = selectable as MonoBehaviour;
            if (mono != null)
            {
                Pawn pawn = mono.GetComponent<Pawn>();
                if (pawn != null)
                {
                    pawn.Heal(healAmount);
                    count++;
                }
            }
        }
        
        if (count > 0)
        {
            Debug.Log($"[DEBUG] Healed {count} pawns for {healAmount} HP");
        }
    }

    void KillSelectedPawns()
    {
        var selected = selectionManager.GetSelectedObjects();
        int count = 0;
        
        foreach (var selectable in selected)
        {
            MonoBehaviour mono = selectable as MonoBehaviour;
            if (mono != null)
            {
                Pawn pawn = mono.GetComponent<Pawn>();
                if (pawn != null)
                {
                    pawn.TakeDamage(9999f);
                    count++;
                }
            }
        }
        
        if (count > 0)
        {
            Debug.Log($"[DEBUG] Killed {count} pawns");
        }
    }
}