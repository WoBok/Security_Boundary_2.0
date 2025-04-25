using UnityEngine;

public class SecurityBoundaryManager
{
    static SecurityBoundaryManager m_Instance;
    public static SecurityBoundaryManager Instance { get { if (m_Instance == null) m_Instance = new SecurityBoundaryManager(); return m_Instance; } }

    public System.Action OnEnteredSecurityBoundary
    {
        get
        {
            if (m_SecurityBoundaryAreaDispatcher != null)
                return m_SecurityBoundaryAreaDispatcher.OnEnteredSecurityBoundary;
            Debug.LogError("Dispatcher is null!");
            return null;
        }
        set
        {
            if (m_SecurityBoundaryAreaDispatcher != null)
                m_SecurityBoundaryAreaDispatcher.OnEnteredSecurityBoundary = value;
        }
    }
    public System.Action OnExitedSecurityBoundary
    {
        get
        {
            if (m_SecurityBoundaryAreaDispatcher != null)
                return m_SecurityBoundaryAreaDispatcher.OnExitedSecurityBoundary;
            Debug.LogError("Dispatcher is null!");
            return null;
        }
        set
        {
            if (m_SecurityBoundaryAreaDispatcher != null)
                m_SecurityBoundaryAreaDispatcher.OnExitedSecurityBoundary = value;
        }
    }
    public System.Action OnEnteredRestrictedArea
    {
        get
        {
            if (m_SecurityBoundaryAreaDispatcher != null)
                return m_SecurityBoundaryAreaDispatcher.OnEnteredRestrictedArea;
            Debug.LogError("Dispatcher is null!");
            return null;
        }
        set
        {
            if (m_SecurityBoundaryAreaDispatcher != null)
                m_SecurityBoundaryAreaDispatcher.OnEnteredRestrictedArea = value;
        }
    }
    public System.Action OnExitedRestrictedArea
    {
        get
        {
            if (m_SecurityBoundaryAreaDispatcher != null)
                return m_SecurityBoundaryAreaDispatcher.OnExitedRestrictedArea;
            Debug.LogError("Dispatcher is null!");
            return null;
        }
        set
        {
            if (m_SecurityBoundaryAreaDispatcher != null)
                m_SecurityBoundaryAreaDispatcher.OnExitedRestrictedArea = value;
        }
    }
    SecurityBoundaryAreaDispatcher m_SecurityBoundaryAreaDispatcher;
    public void Open(Transform boundaryRootTrans, float minSafeDistance)
    {
        if (m_SecurityBoundaryAreaDispatcher == null)
        {
            var dispatcherObj = new GameObject("SecurityBoundaryAreaDispatcher");
            m_SecurityBoundaryAreaDispatcher = dispatcherObj.AddComponent<SecurityBoundaryAreaDispatcher>();
        }
        m_SecurityBoundaryAreaDispatcher.CreateArea(boundaryRootTrans, minSafeDistance, minSafeDistance);
    }
    public void Close()
    {
        if (m_SecurityBoundaryAreaDispatcher != null)
        {
            m_SecurityBoundaryAreaDispatcher.Close();
            Object.Destroy(m_SecurityBoundaryAreaDispatcher.gameObject);
            m_SecurityBoundaryAreaDispatcher = null;
        }
    }
    public void SetMovableSecurityBoundary(bool movable)
    {
        if (m_SecurityBoundaryAreaDispatcher != null)
            m_SecurityBoundaryAreaDispatcher.SetMovableSecurityBoundary(movable);
    }
}