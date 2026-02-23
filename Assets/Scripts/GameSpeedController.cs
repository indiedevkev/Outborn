using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// RimWorld-ähnliche Zeitsteuerung:
/// 1 = normale Geschwindigkeit (1x)
/// 2, 3, 4 = schnellere Geschwindigkeit (2x, 3x, 4x)
/// Leertaste = Pause (alles angehalten)
/// </summary>
public class GameSpeedController : MonoBehaviour
{
    [Header("Geschwindigkeitsstufen")]
    [SerializeField] private float speed1 = 1f;
    [SerializeField] private float speed2 = 2f;
    [SerializeField] private float speed3 = 3f;
    [SerializeField] private float speed4 = 4f;

    private bool _paused;
    private int _currentSpeedIndex = 1; // 1–4

    void Start()
    {
        ApplySpeed();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Leertaste = Pause (an/aus)
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            _paused = !_paused;
            ApplySpeed();
            return;
        }

        // 1–4 = Geschwindigkeit (bei Pause zuerst Pause aufheben)
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            _paused = false;
            _currentSpeedIndex = 1;
            ApplySpeed();
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            _paused = false;
            _currentSpeedIndex = 2;
            ApplySpeed();
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            _paused = false;
            _currentSpeedIndex = 3;
            ApplySpeed();
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            _paused = false;
            _currentSpeedIndex = 4;
            ApplySpeed();
        }
    }

    private void ApplySpeed()
    {
        if (_paused)
        {
            Time.timeScale = 0f;
            return;
        }

        float scale = _currentSpeedIndex switch
        {
            1 => speed1,
            2 => speed2,
            3 => speed3,
            4 => speed4,
            _ => speed1
        };
        Time.timeScale = Mathf.Max(0f, scale);
    }

    /// <summary> Aktuell Pause? </summary>
    public bool IsPaused => _paused;

    /// <summary> Aktuelle Stufe 1–4. </summary>
    public int CurrentSpeedIndex => _currentSpeedIndex;

    /// <summary> Von UI/anderen Scripts: Pause umschalten. </summary>
    public void TogglePause()
    {
        _paused = !_paused;
        ApplySpeed();
    }

    /// <summary> Von UI: Geschwindigkeit auf Stufe 1–4 setzen. </summary>
    public void SetSpeed(int index)
    {
        if (index < 1 || index > 4) return;
        _paused = false;
        _currentSpeedIndex = index;
        ApplySpeed();
    }
}
