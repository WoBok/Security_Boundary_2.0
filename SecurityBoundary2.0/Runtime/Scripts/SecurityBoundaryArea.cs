using System.Collections.Generic;
using UnityEngine;

public enum AreaType
{
    None,
    AccessibleArea,//活动区
    RestrictedArea//障碍区
}

public class SecurityBoundaryArea
{
    public System.Action<SecurityBoundaryArea> OnEnteredSecurityBoundary;
    public System.Action<SecurityBoundaryArea> OnExitedSecurityBoundary;
    public System.Action OnEnteredRestrictedArea;
    public System.Action OnExitedRestrictedArea;
    class SecurityBoundary
    {
        public SecurityBoundaryChecker checker;
        public SecurityBoundaryDisplayer display;
    }
    SecurityBoundary m_AccessibleAreaBoundary = new SecurityBoundary();
    Dictionary<string, SecurityBoundary> m_RestrictedAreaBoundaries = new Dictionary<string, SecurityBoundary>();

    public void AddAccessibleAreaBoundary(Transform rootTrans, float minSafeDistance, Transform areaCenterTrans, string areaName)
    {
        if (m_AccessibleAreaBoundary.checker != null)
            Object.Destroy(m_AccessibleAreaBoundary.checker.gameObject);
        if (m_AccessibleAreaBoundary.display != null)
            Object.Destroy(m_AccessibleAreaBoundary.display.gameObject);
        SetupBoundary(m_AccessibleAreaBoundary, AreaType.AccessibleArea, rootTrans, minSafeDistance, areaCenterTrans, areaName);
        BindAccessibleAreaCheckerEvent();
    }
    public void AddRestrictedAreaBoundary(Transform rootTrans, float minSafeDistance, Transform areaCenterTrans, string areaName)
    {
        var securityBoundary = new SecurityBoundary();
        SetupBoundary(securityBoundary, AreaType.RestrictedArea, rootTrans, minSafeDistance, areaCenterTrans, areaName);
        BindRestrictedAreaCheckerEvent(securityBoundary.checker);
        if (!m_RestrictedAreaBoundaries.ContainsKey(areaName))
            m_RestrictedAreaBoundaries.Add(areaName, securityBoundary);
        else
        {
            Object.Destroy(m_RestrictedAreaBoundaries[areaName].display.gameObject);
            m_RestrictedAreaBoundaries[areaName] = securityBoundary;
        }
    }
    public void RemoveRestrictedAreaBoundary(string areaName)
    {
        if (m_RestrictedAreaBoundaries.ContainsKey(areaName))
            m_RestrictedAreaBoundaries.Remove(areaName);
        else Debug.Log($"Restricted area {areaName} not found! Removal Failed!");
    }
    public void SetMovableSecurityBoundary(Transform accessibleRootTrans, Dictionary<string, Transform> restrictedRootTranses, bool movable)
    {
        UpdateAccessibleAreaBoundaryPoints(accessibleRootTrans, movable);
        UpdateRestrictedAreaBoundaryPoints(restrictedRootTranses, movable);
        UpdateAccessibleAreaMovable(accessibleRootTrans, movable);
        UpdateRestrictedAreaMovable(restrictedRootTranses, movable);
    }
    void UpdateAccessibleAreaMovable(Transform rootTrans, bool movable)
    {
        if (m_AccessibleAreaBoundary.checker != null)
        {
            m_AccessibleAreaBoundary.checker.areaTrans = movable ? rootTrans.GetChild(0) : null;
            m_AccessibleAreaBoundary.checker.movable = movable;
        }
        else Debug.LogError("Accessible area boundary checker not initialized!");
    }
    void UpdateRestrictedAreaMovable(Dictionary<string, Transform> rootTranses, bool movable)
    {
        foreach (var rootTrans in rootTranses)
        {
            if (m_RestrictedAreaBoundaries.ContainsKey(rootTrans.Key))
            {
                m_RestrictedAreaBoundaries[rootTrans.Key].checker.areaTrans = movable ? rootTrans.Value.GetChild(0) : null;
                m_RestrictedAreaBoundaries[rootTrans.Key].checker.movable = movable;
            }
            else Debug.LogError($"Restricted area {rootTrans.Key} not found!");
        }
    }
    public void UpdateAccessibleAreaBoundaryPoints(Transform rootTrans, bool moveable = false)
    {
        if (m_AccessibleAreaBoundary.checker != null)
            UpdateAreaBoundaryPoints(m_AccessibleAreaBoundary.checker, rootTrans, moveable);
        else Debug.LogError("Accessible area boundary checker not initialized!");
    }
    public void UpdateRestrictedAreaBoundaryPoints(Dictionary<string, Transform> rootTranses, bool moveable = false)
    {
        if (rootTranses.Count == m_RestrictedAreaBoundaries.Count)
            foreach (var rootTrans in rootTranses)
                if (m_RestrictedAreaBoundaries.ContainsKey(rootTrans.Key))
                    UpdateAreaBoundaryPoints(m_RestrictedAreaBoundaries[rootTrans.Key].checker, rootTrans.Value, moveable);
                else Debug.LogError($"Restricted area {rootTrans.Key} not found!");
        else Debug.LogError("Number of vertices in the restricted area does not match the number of restricted areas!");
    }
    void UpdateAreaBoundaryPoints(SecurityBoundaryChecker checker, Transform rootTrans, bool moveable = false)
    {
        var pointsTrans = rootTrans.GetChild(1);
        if (moveable)
            checker.boundaryPoints = GetLocalPoints(pointsTrans);

        else
            checker.boundaryPoints = GetPoints(pointsTrans);
    }
    public void UpdateAccessibleAreaMinSafeDistance(float minSafeDistance)
    {
        if (m_AccessibleAreaBoundary.checker != null)
            m_AccessibleAreaBoundary.checker.minSafeDistance = minSafeDistance;
        else Debug.LogError("Accessible area boundary checker not initialized!");
    }
    public void UpdateRestrictedAreaMinSafeDistance(Dictionary<string, float> minSafeDistances)
    {
        if (minSafeDistances.Count == m_RestrictedAreaBoundaries.Count)
            foreach (var minSafeDistance in minSafeDistances)
                if (m_RestrictedAreaBoundaries.ContainsKey(minSafeDistance.Key))
                    m_RestrictedAreaBoundaries[minSafeDistance.Key].checker.minSafeDistance = minSafeDistance.Value;
                else Debug.LogError($"Restricted area {minSafeDistance.Key} not found!");
        else Debug.LogError("The number of minimum safe distances in the obstacle zone does not match the number of obstacle zones!");
    }
    public void UpdateAccessibleAreaareaCenterTrans(Transform areaCenterTrans)
    {
        if (m_AccessibleAreaBoundary.display != null)
            m_AccessibleAreaBoundary.display.areaCenterPosition = areaCenterTrans.position;
        else Debug.LogError("Accessible area boundary display not initialized!");
    }
    public void UpdateRestrictedAreaareaCenterTrans(Dictionary<string, Transform> areaCenterTranses)
    {
        if (areaCenterTranses.Count == m_RestrictedAreaBoundaries.Count)
            foreach (var areaCenterTrans in areaCenterTranses)
                if (m_RestrictedAreaBoundaries.ContainsKey(areaCenterTrans.Key))
                    m_RestrictedAreaBoundaries[areaCenterTrans.Key].display.areaCenterPosition = areaCenterTrans.Value.position;
                else Debug.LogError($"Restricted area {areaCenterTrans.Key} not found!");
        else Debug.LogError("The number of restricted area centers does not match the number of restricted areas!");
    }
    public void Pause()
    {
        PauseAccessibleAreaCheck();
        PauseRestrictedAreaCheck();
    }
    public void Continue()
    {
        ContinueAccessibleAreaCheck();
        ContinueRestrictedAreaCheck();
    }
    public void PauseAccessibleAreaCheck()
    {
        ChangeAccessibleAreaCheckState(false);
    }
    public void PauseRestrictedAreaCheck()
    {
        ChangeRestrictedAreaCheckState(false);
    }
    public void ContinueAccessibleAreaCheck()
    {
        ChangeAccessibleAreaCheckState(true);
    }
    public void ContinueRestrictedAreaCheck()
    {
        ChangeRestrictedAreaCheckState(true);
    }
    public void Close()
    {
        DestroyBoundaryDisplay();
        DestroyBoundaryChecker();
        m_RestrictedAreaBoundaries.Clear();
        ClearEvent();
        SecurityBoundarySound.Clear();
    }
    void SetupBoundary(SecurityBoundary boundary, AreaType areaType, Transform rootTrans, float minSafeDistance, Transform areaCenterTrans, string areaName)
    {
        var pointsTrans = rootTrans.GetChild(1);
        var boundaryPoints = GetPoints(pointsTrans);
        var boundaryLocalPoints = GetLocalPoints(pointsTrans);

        boundary.checker = CreateBoundaryChecker(areaType, areaName);
        boundary.checker.boundaryPoints = boundaryPoints;
        boundary.checker.minSafeDistance = minSafeDistance;
        boundary.checker.areaType = areaType;

        boundary.display = CreateBoundaryDisplay(areaType, boundary.checker, areaName);
        boundary.display.areaCenterPosition = areaCenterTrans.position;
        boundary.display.rootTrans = rootTrans;
        boundary.display.boundaryLocalPoints = boundaryLocalPoints;
    }
    SecurityBoundaryChecker CreateBoundaryChecker(AreaType areaType, string areaName)
    {
        var boundaryCheckerObj = new GameObject($"{areaName}BoundaryChecker");
        Object.DontDestroyOnLoad(boundaryCheckerObj);
        var boundaryChecker = boundaryCheckerObj.AddComponent<SecurityBoundaryChecker>();
        boundaryChecker.areaType = areaType;
        return boundaryChecker;
    }
    void BindAccessibleAreaCheckerEvent()
    {
        m_AccessibleAreaBoundary.checker.OnEnteredSecurityBoundary += () => { OnEnteredSecurityBoundary?.Invoke(this); };
        m_AccessibleAreaBoundary.checker.OnExitedSecurityBoundary += () => { OnExitedSecurityBoundary?.Invoke(this); };
    }
    void BindRestrictedAreaCheckerEvent(SecurityBoundaryChecker boundaryChecker)
    {
        boundaryChecker.OnEnteredSecurityBoundary += OnEnteredRestrictedArea;
        boundaryChecker.OnExitedSecurityBoundary += OnExitedRestrictedArea;
    }
    SecurityBoundaryDisplayer CreateBoundaryDisplay(AreaType areaType, SecurityBoundaryChecker boundaryChecker, string areaName)
    {
        var boundaryDisplayObj = new GameObject($"{areaName}BoundaryDisplay");
        Object.DontDestroyOnLoad(boundaryDisplayObj);
        var boundaryDisplay = boundaryDisplayObj.AddComponent<SecurityBoundaryDisplayer>();
        boundaryChecker.OnEnteredSecurityBoundary +=
            areaType == AreaType.AccessibleArea ? boundaryDisplay.EnteredSecurityBoundary : boundaryDisplay.ExitedSecurityBoundary;
        boundaryChecker.OnExitedSecurityBoundary +=
            areaType == AreaType.AccessibleArea ? boundaryDisplay.ExitedSecurityBoundary : boundaryDisplay.EnteredSecurityBoundary;
        boundaryChecker.OnApproachedSecurityBoundary += boundaryDisplay.ApproachedSecurityBoundary;
        boundaryChecker.OnLeftSecurityBoundary += boundaryDisplay.LeftSecurityBoundary;
        return boundaryDisplay;
    }
    void ChangeAccessibleAreaCheckState(bool state)
    {
        ChangeAreaCheckState(m_AccessibleAreaBoundary, state);
    }
    void ChangeRestrictedAreaCheckState(bool state)
    {
        foreach (var restrictedAreaBoundary in m_RestrictedAreaBoundaries.Values)
            ChangeAreaCheckState(restrictedAreaBoundary, state);
    }
    void ChangeAreaCheckState(SecurityBoundary securityBoundary, bool state)
    {
        if (securityBoundary.checker != null)
            securityBoundary.checker.enabled = state;
        if (securityBoundary.display != null)
        {
            if (state == false)
            {
                securityBoundary.display.Pause();
            }
            securityBoundary.display.enabled = state;
        }
    }
    void DestroyBoundaryChecker()
    {
        if (m_AccessibleAreaBoundary.checker != null)
        {
            if (m_AccessibleAreaBoundary.checker != null)
            {
                m_AccessibleAreaBoundary.checker.Close();
                Object.Destroy(m_AccessibleAreaBoundary.checker.gameObject);
            }
        }
        foreach (var restrictedAreaBoundary in m_RestrictedAreaBoundaries.Values)
        {
            if (restrictedAreaBoundary.checker != null)
            {
                restrictedAreaBoundary.checker.Close();
                Object.Destroy(restrictedAreaBoundary.checker.gameObject);
            }
        }
    }
    void DestroyBoundaryDisplay()
    {
        if (m_AccessibleAreaBoundary.display != null)
        {
            if (m_AccessibleAreaBoundary.display != null)
            {
                m_AccessibleAreaBoundary.display.Close();
                Object.Destroy(m_AccessibleAreaBoundary.display.gameObject);
            }
        }
        foreach (var restrictedAreaBoundary in m_RestrictedAreaBoundaries.Values)
        {
            if (restrictedAreaBoundary.display != null)
            {
                restrictedAreaBoundary.display.Close();
                Object.Destroy(restrictedAreaBoundary.display.gameObject);
            }
        }
    }
    void ClearEvent()
    {
        OnEnteredSecurityBoundary = null;
        OnExitedSecurityBoundary = null;
    }
    Vector2[] GetPoints(Transform rootTrans)
    {
        var boundaryPointsCount = rootTrans.childCount;
        var boundaryPoints = new Vector2[boundaryPointsCount];
        for (int i = 0; i < boundaryPointsCount; i++)
        {
            var pos = rootTrans.GetChild(i).position;
            boundaryPoints[i] = new Vector2(pos.x, pos.z);
        }
        return boundaryPoints;
    }
    Vector2[] GetLocalPoints(Transform rootTrans)
    {
        var boundaryLocalPointsCount = rootTrans.childCount;
        var boundaryLocalPoints = new Vector2[boundaryLocalPointsCount];
        for (int i = 0; i < boundaryLocalPointsCount; i++)
        {
            var pos = rootTrans.GetChild(i).localPosition;
            boundaryLocalPoints[i] = new Vector2(pos.x, pos.z);
        }
        return boundaryLocalPoints;
    }
}