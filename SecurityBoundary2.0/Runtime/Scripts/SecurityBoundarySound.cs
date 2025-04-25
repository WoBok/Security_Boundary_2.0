using UnityEngine;

public class SecurityBoundarySound : MonoBehaviour
{
    static SecurityBoundarySound m_Instance;
    public static SecurityBoundarySound Instance
    {
        get
        {
            if (m_Instance == null)
            {
                var obj = new GameObject("AudioPlayer");
                m_Instance = obj.AddComponent<SecurityBoundarySound>();
                m_Instance.m_AudioSource = obj.AddComponent<AudioSource>();
            }
            return m_Instance;
        }
    }

    AudioSource m_AudioSource;

    public void PlaySound(string name)
    {
        var audio = Resources.Load<AudioClip>($"SecurityBoundary/Audio/{name}");
        m_AudioSource.clip = audio;
        m_AudioSource.Play();
    }
    public static void  Clear()
    {
        if (m_Instance != null)
        {
            m_Instance.m_AudioSource = null;
            Destroy(m_Instance.gameObject);
            m_Instance = null;
        }
    }
}