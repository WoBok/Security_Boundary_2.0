using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityBoundaryAreaDispatcher : MonoBehaviour
{
    public Action OnEnteredSecurityBoundary;
    public Action OnExitedSecurityBoundary;
    public Action OnEnteredRestrictedArea;
    public Action OnExitedRestrictedArea;

    List<SecurityBoundaryArea> m_Areas = new List<SecurityBoundaryArea>();

    bool m_IsNotFirstEnter;

    int m_TotalBoundaryCount;
    int m_ExitedBoundaryCount;

    Transform m_BoundaryRootTrans;
    Coroutine m_UpdateBoundaryPointsCoroutine;

    public void CreateArea(Transform boundaryRootTrans, float accessibleAreaMinSafeDistance, float restrictedAreaMinSafeDistance)
    {
        m_TotalBoundaryCount = boundaryRootTrans.childCount;
        m_BoundaryRootTrans = boundaryRootTrans;
        for (int i = 0; i < m_TotalBoundaryCount; i++)
        {
            var areaTrans = boundaryRootTrans.GetChild(i);
            var areaCenterTrans = areaTrans.GetChild(0);
            var accessibleAreaTrans = areaTrans.GetChild(1);
            var restrictedAreaTrans = areaTrans.GetChild(2);
            var area = new SecurityBoundaryArea();
            m_Areas.Add(area);
            BindEvent(area);

            if (accessibleAreaTrans != null)
                AddAccessibleArea(area, accessibleAreaTrans, accessibleAreaMinSafeDistance, areaCenterTrans, areaTrans.name + accessibleAreaTrans.name);
            else Debug.LogError("Accessible area not set correctly!");
            if (restrictedAreaTrans != null)
                AddRestrictedArea(area, restrictedAreaTrans, restrictedAreaMinSafeDistance, areaCenterTrans, areaTrans.name);
            else Debug.LogError("Restricted area not set correctly!");
        }
    }
    public void SetMovableSecurityBoundary(bool movable)
    {
        for (int i = 0; i < m_Areas.Count; i++)
        {
            var areaTrans = m_BoundaryRootTrans.GetChild(i);
            var areaCenterTrans = areaTrans.GetChild(0);
            var accessibleAreaTrans = areaTrans.GetChild(1);
            var restrictedAreaTransParent = areaTrans.GetChild(2);
            if (accessibleAreaTrans != null && restrictedAreaTransParent != null)
            {
                var restrictedRootTranses = new Dictionary<string, Transform>();
                for (int j = 0; j < restrictedAreaTransParent.childCount; j++)
                {
                    var restrictedAreaTrans = restrictedAreaTransParent.GetChild(j);
                    var name = areaTrans.name + restrictedAreaTrans.name;
                    restrictedRootTranses.Add(name, restrictedAreaTrans);
                }
                m_Areas[i].SetMovableSecurityBoundary(accessibleAreaTrans, restrictedRootTranses, movable);
            }
        }
    }
    public void Close()
    {
        OnEnteredSecurityBoundary = null;
        OnExitedSecurityBoundary = null;
        OnEnteredRestrictedArea = null;
        OnExitedRestrictedArea = null;
        foreach (var area in m_Areas)
            area.Close();
    }
    void AddAccessibleArea(SecurityBoundaryArea area, Transform rootTrans, float minSafeDistance, Transform areaCenterTrans, string areaName)
    {
        area.AddAccessibleAreaBoundary(rootTrans, minSafeDistance, areaCenterTrans, areaName);
    }
    void AddRestrictedArea(SecurityBoundaryArea area, Transform rootTrans, float minSafeDistance, Transform areaCenterTrans, string areaName)
    {
        for (int i = 0; i < rootTrans.childCount; i++)
        {
            var restrictedAreaTrans = rootTrans.GetChild(i);
            area.AddRestrictedAreaBoundary(restrictedAreaTrans, minSafeDistance, areaCenterTrans, areaName + restrictedAreaTrans.name);
        }
    }
    void BindEvent(SecurityBoundaryArea area)
    {
        area.OnEnteredSecurityBoundary = EnteredSecurityBoundary;
        area.OnExitedSecurityBoundary = ExitedSecurityBoundary;
        area.OnEnteredRestrictedArea = () => OnEnteredRestrictedArea?.Invoke();
        area.OnExitedRestrictedArea = () => OnExitedRestrictedArea?.Invoke();
    }
    void EnteredSecurityBoundary(SecurityBoundaryArea enteredArea)
    {
        Debug.Log("EnteredSecurityBoundary");
        m_ExitedBoundaryCount = 0;
        OnEnteredSecurityBoundary?.Invoke();
        if (!m_IsNotFirstEnter)
        {
            m_IsNotFirstEnter = true;
            enteredArea.ContinueRestrictedAreaCheck();
            foreach (var area in m_Areas)
                if (area != enteredArea)
                    area.Pause();
        }
        else
        {
            enteredArea.ContinueRestrictedAreaCheck();
            foreach (var area in m_Areas)
                if (area != enteredArea)
                    area.PauseAccessibleAreaCheck();
        }
    }
    void ExitedSecurityBoundary(SecurityBoundaryArea exitedArea)
    {
        if (m_ExitedBoundaryCount == 0)
        {
            foreach (var area in m_Areas)
            {
                area.ContinueAccessibleAreaCheck();
                area.PauseRestrictedAreaCheck();
            }
        }
        CheckExitedSecurityBoundary();
    }
    void CheckExitedSecurityBoundary()
    {
        m_ExitedBoundaryCount++;
        if (m_ExitedBoundaryCount == m_TotalBoundaryCount)
        {
            Debug.Log("ExitedSecurityBoundary");
            OnExitedSecurityBoundary?.Invoke();
            m_ExitedBoundaryCount = 0;
        }
    }
    void OnDestroy()
    {
        Close();
    }
}