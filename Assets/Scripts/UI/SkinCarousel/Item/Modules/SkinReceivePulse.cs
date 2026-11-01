using System;
using UnityEngine;

[Serializable]
public class SkinReceivePulse
{
    [Header("Receive Points Anim")]
    [SerializeField] float pulseScale = 1.3f;
    [SerializeField, Range(0f, 1f)] float minStrength = 0.45f;
    [SerializeField] float decay = 12f;
    [SerializeField] float minInterval = 0.07f;
    [SerializeField] float maxInterval = 0.22f;
    [SerializeField] float hold = 0.3f;

    float _punch;
    float _timer;
    float _nextIn;

    public float Punch => _punch;
    public bool Active => _timer > 0f;

    public void Trigger()
    {
        if (_timer <= 0f)
            _nextIn = 0f;

        _timer = hold;
    }

    public void Reset()
    {
        _punch = 0f;
        _timer = 0f;
    }

    public void Tick(float dt)
    {
        if (_timer > 0f)
        {
            _timer -= dt;
            _nextIn -= dt;

            if (_nextIn <= 0f)
            {
                float peak = Mathf.Max(0f, pulseScale - 1f);
                float strength = UnityEngine.Random.Range(minStrength, 1f);
                _punch = Mathf.Max(_punch, peak * strength);
                _nextIn = UnityEngine.Random.Range(minInterval, maxInterval);
            }
        }

        if (_punch > 0.0001f)
            _punch = Mathf.Lerp(_punch, 0f, 1f - Mathf.Exp(-decay * dt));
        else
            _punch = 0f;
    }
}
