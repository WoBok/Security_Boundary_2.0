using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class SecurityBoundaryChecker : MonoBehaviour
{
    public enum PlayerState
    {
        None,
        EnteredBoundary,
        ExitedBoundary,
        ApproachedBoundary,
        LeftBoundary
    }

    public Action OnEnteredSecurityBoundary;
    public Action OnExitedSecurityBoundary;
    public Action<MinDistanceInfo> OnApproachedSecurityBoundary;
    public Action OnLeftSecurityBoundary;

    public float minSafeDistance = 0.5f;
    public AreaType areaType = AreaType.AccessibleArea;
    public Transform areaTrans;
    public bool movable;

    Camera m_MainCamera;
    Camera MainCamera { get { if (m_MainCamera == null) m_MainCamera = Camera.main; return m_MainCamera; } }
    float2 PlayerPosition
    {
        get
        {
            if (movable)
            {
                var playerLocalPos = areaTrans.InverseTransformPoint(MainCamera.transform.position);
                return new float2(playerLocalPos.x, playerLocalPos.z);
            }
            else
            {
                var camPos = MainCamera.transform.position;
                return new float2(camPos.x, camPos.z);
            }
        }
    }

    PlayerState m_PlayerState;

    NativeArray<bool> m_IsPlayerInBoundary = new NativeArray<bool>(1, Allocator.Persistent);
    NativeArray<float2> m_PAs;
    NativeArray<float2> m_PBs;
    NativeArray<MinDistanceInfo> m_MinDistanceInfoes;

    JobHandle m_CheckInPolygonJobHandle;
    JobHandle m_MinDistanceInfoJobHandle;

    List<MinDistanceInfo> m_ApproachedMinDistanceInfoes = new List<MinDistanceInfo>();
    MinDistanceInfo m_MinDistanceInfo = new MinDistanceInfo() { minDistance = float.MaxValue };
#if UNITY_EDITOR
    bool m_IsPlayerInBoundaryGizmos;
    Vector2[] m_BoundaryVerticesGizmos;
    List<MinDistanceInfo> m_MiniDistanceInfosGizmos = new List<MinDistanceInfo>();
#endif
    public Vector2[] boundaryPoints
    {
        set
        {
            if (value == null || value.Length < 3)
                throw new ArgumentException("Boundary points must have at least 3 vertices");
#if UNITY_EDITOR
            m_BoundaryVerticesGizmos = value;
#endif
            var arrLength = value.Length;
            if (m_PAs.IsCreated) m_PAs.Dispose();
            m_PAs = new NativeArray<float2>(arrLength, Allocator.Persistent);
            if (m_PBs.IsCreated) m_PBs.Dispose();
            m_PBs = new NativeArray<float2>(arrLength, Allocator.Persistent);
            for (int i = 0; i < value.Length; i++)
            {
                var pA = value[i];
                m_PAs[i] = new float2(pA.x, pA.y);
                var pB = value[(i + 1) % value.Length];
                m_PBs[i] = new float2(pB.x, pB.y);
            }
            if (m_MinDistanceInfoes.IsCreated) m_MinDistanceInfoes.Dispose();
            m_MinDistanceInfoes = new NativeArray<MinDistanceInfo>(arrLength, Allocator.Persistent);
        }
    }
    void OnEnable()
    {
        m_PlayerState = PlayerState.None;
    }
    void Update()
    {
        if (Time.frameCount % 2 == 0)
            IsPlayerInBoundary();
    }
    void IsPlayerInBoundary()
    {
        if (m_CheckInPolygonJobHandle.IsCompleted)
        {
            m_CheckInPolygonJobHandle.Complete();
#if UNITY_EDITOR
            m_IsPlayerInBoundaryGizmos = m_IsPlayerInBoundary[0];
#endif
            if (m_IsPlayerInBoundary[0])
            {
                EnteringSecurityBoundary();
                if (areaType == AreaType.AccessibleArea)
                    IsNearBoundary();
            }
            else
            {
                ExitingSecurityBoundary();
                if (areaType == AreaType.RestrictedArea)
                    IsNearBoundary();
            }
            ScheduleCheckInPolygonJob();
        }
    }
    void ScheduleCheckInPolygonJob()
    {
        m_IsPlayerInBoundary[0] = false;
        var job = new CheckInPolygonJob()
        {
            pAs = m_PAs,
            pBs = m_PBs,
            point = PlayerPosition,
            isPointInPolygon = m_IsPlayerInBoundary
        };
        m_CheckInPolygonJobHandle = job.Schedule(m_PAs.Length, 64);
    }
    void EnteringSecurityBoundary()
    {
        if (areaType == AreaType.AccessibleArea)
            if (m_PlayerState == PlayerState.None || m_PlayerState == PlayerState.ExitedBoundary)
                DoEnteredEvent();
        if (areaType == AreaType.RestrictedArea)
            if (m_PlayerState == PlayerState.None || m_PlayerState == PlayerState.ApproachedBoundary)
                DoEnteredEvent();
    }
    void DoEnteredEvent()
    {
        m_PlayerState = PlayerState.EnteredBoundary;
        OnEnteredSecurityBoundary?.Invoke();
    }
    void ExitingSecurityBoundary()
    {
        if (areaType == AreaType.AccessibleArea)
            if (m_PlayerState == PlayerState.None || m_PlayerState == PlayerState.ApproachedBoundary)
                DoExitedEvent();
        if (areaType == AreaType.RestrictedArea)
            if (m_PlayerState == PlayerState.None || m_PlayerState == PlayerState.EnteredBoundary)
                DoExitedEvent();
    }
    void DoExitedEvent()
    {
        m_PlayerState = PlayerState.ExitedBoundary;
        OnExitedSecurityBoundary?.Invoke();
    }
    void IsNearBoundary()
    {
        m_ApproachedMinDistanceInfoes.Clear();
#if UNITY_EDITOR
        m_MiniDistanceInfosGizmos.Clear();
#endif
        if (m_MinDistanceInfoJobHandle.IsCompleted)
        {
            m_MinDistanceInfoJobHandle.Complete();

            foreach (var minDistanceInfo in m_MinDistanceInfoes)
            {
#if UNITY_EDITOR
                m_MiniDistanceInfosGizmos.Add(minDistanceInfo);
#endif
                if (minDistanceInfo.minDistance < m_MinDistanceInfo.minDistance)
                    m_MinDistanceInfo = minDistanceInfo;
                //m_ApproachedMinDistanceInfoes.Add(minDistanceInfo);
            }
            //if (m_ApproachedMinDistanceInfoes.Count > 0)
            if (m_MinDistanceInfo.minDistance <= minSafeDistance)
                ApproachingSecurityBoundary();
            else
                LeavingSecurityBoundary();
            m_MinDistanceInfo.minDistance = float.MaxValue;
            ScheduleMinDistanceInfoJob();
        }
    }
    void ScheduleMinDistanceInfoJob()
    {
        var job = new MinDistanceInfoJobParallel()
        {
            pAs = m_PAs,
            pBs = m_PBs,
            point = PlayerPosition,
            minDistanceInfo = m_MinDistanceInfoes
        };
        m_MinDistanceInfoJobHandle = job.Schedule(m_PAs.Length, 64);
    }
    void ApproachingSecurityBoundary()
    {
        m_PlayerState = PlayerState.ApproachedBoundary;
        //OnApproachedSecurityBoundary?.Invoke(m_ApproachedMinDistanceInfoes[0]);
        OnApproachedSecurityBoundary?.Invoke(m_MinDistanceInfo);
    }
    void LeavingSecurityBoundary()
    {
        if (m_PlayerState == PlayerState.ApproachedBoundary)
            DoLeftEvent();
    }
    void DoLeftEvent()
    {
        m_PlayerState = PlayerState.LeftBoundary;
        OnLeftSecurityBoundary?.Invoke();
    }
    public void Close()
    {
        ClearEvent();
        DisposeNativeContainer();
    }
    void ClearEvent()
    {
        OnEnteredSecurityBoundary = null;
        OnExitedSecurityBoundary = null;
        OnApproachedSecurityBoundary = null;
        OnLeftSecurityBoundary = null;
    }
    void DisposeNativeContainer()
    {
        m_CheckInPolygonJobHandle.Complete();
        m_MinDistanceInfoJobHandle.Complete();

        if (m_IsPlayerInBoundary.IsCreated) m_IsPlayerInBoundary.Dispose();
        if (m_PAs.IsCreated) m_PAs.Dispose();
        if (m_PBs.IsCreated) m_PBs.Dispose();
        if (m_MinDistanceInfoes.IsCreated) m_MinDistanceInfoes.Dispose();
    }
    void OnDestroy()
    {
        DisposeNativeContainer();
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        for (int i = 0; i < m_BoundaryVerticesGizmos.Length; i++)
        {
            var pVA = m_BoundaryVerticesGizmos[i];
            var pVB = m_BoundaryVerticesGizmos[(i + 1) % m_BoundaryVerticesGizmos.Length];
            Vector3 pA = new Vector3(pVA.x, 0, pVA.y);
            Vector3 pB = new Vector3(pVB.x, 0, pVB.y);
            Debug.DrawLine(pA, pB);
            Handles.Label(pA + Vector3.right * 0.05f + Vector3.up * 0.1f, $"{pVA}");
        }
        if (m_IsPlayerInBoundaryGizmos)
        {
            for (int i = 0; i < m_MiniDistanceInfosGizmos.Count; i++)
            {
                var pointInfo = m_MiniDistanceInfosGizmos[i];
                var playerPosition = new Vector2(MainCamera.transform.position.x, MainCamera.transform.position.z);
                Gizmos.DrawLine(new Vector3(playerPosition.x, 0, playerPosition.y), new Vector3(pointInfo.nearestPoint.x, 0, pointInfo.nearestPoint.y));
                GUI.color = Color.white;
                Handles.Label(new Vector3(pointInfo.nearestPoint.x, 0, pointInfo.nearestPoint.y - 0.1f), pointInfo.nearestPoint.ToString());
                var labelPosition = playerPosition + ((Vector2)pointInfo.nearestPoint - playerPosition) / 2;
                GUI.color = Color.green;
                Handles.Label(new Vector3(labelPosition.x, 0, labelPosition.y + 0.1f), pointInfo.minDistance.ToString());
            }
        }
        GUI.color = Color.white;
        Handles.Label(new Vector3(PlayerPosition.x, 0, PlayerPosition.y), PlayerPosition.ToString());
    }
#endif
}