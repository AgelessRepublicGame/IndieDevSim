using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class MinigameTypingSound : MonoBehaviour
{
    private AudioClip _typingClip;
    private float _volume = 0.7f;
    private Vector2 _pitchRange = new Vector2(0.95f, 1.05f);
    private AudioSource _audioSource;
    private readonly Dictionary<TMP_InputField, UnityAction<string>> _listeners =
        new Dictionary<TMP_InputField, UnityAction<string>>();

    public void Configure(AudioClip typingClip, float volume, Vector2 pitchRange)
    {
        _typingClip = typingClip;
        _volume = Mathf.Clamp01(volume);
        _pitchRange = pitchRange;
    }

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
    }

    private void Start()
    {
        StartCoroutine(FindInputFieldsRoutine());
    }

    private IEnumerator FindInputFieldsRoutine()
    {
        while (true)
        {
            BindNewInputFields();
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private void BindNewInputFields()
    {
        TMP_InputField[] inputFields = GetComponentsInChildren<TMP_InputField>(true);
        foreach (TMP_InputField inputField in inputFields)
        {
            if (inputField == null || _listeners.ContainsKey(inputField))
            {
                continue;
            }

            UnityAction<string> listener = _ => PlayTypingSound(inputField);
            inputField.onValueChanged.AddListener(listener);
            _listeners.Add(inputField, listener);
        }
    }

    private void PlayTypingSound(TMP_InputField inputField)
    {
        if (_typingClip == null
            || _audioSource == null
            || inputField == null
            || !inputField.isFocused)
        {
            return;
        }

        float minimumPitch = Mathf.Min(_pitchRange.x, _pitchRange.y);
        float maximumPitch = Mathf.Max(_pitchRange.x, _pitchRange.y);
        _audioSource.pitch = Random.Range(minimumPitch, maximumPitch);
        _audioSource.PlayOneShot(_typingClip, _volume);
    }

    private void OnDestroy()
    {
        foreach (KeyValuePair<TMP_InputField, UnityAction<string>> entry in _listeners)
        {
            if (entry.Key != null)
            {
                entry.Key.onValueChanged.RemoveListener(entry.Value);
            }
        }
        _listeners.Clear();
    }
}
