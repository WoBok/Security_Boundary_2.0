using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecurityBoundaryDisplay : MonoBehaviour
{
    [HideInInspector]
    public Transform areaCenterTrans;

    GameObject m_SecurityAreaEffect;
    GameObject m_SecurityArrawEffect;
    GameObject m_SecurityBoundaryUI;

    Dictionary<float, GameObject> m_BoundaryEffects = new Dictionary<float, GameObject>();

    Camera m_MainCamera;
    Camera MainCamera { get { if (m_MainCamera == null) m_MainCamera = Camera.main; return m_MainCamera; } }

    Coroutine m_Coroutine;

    bool m_needPlayEnterSound;

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///EnteredBoundary
    public void EnteredBoundary()
    {
        CloseSecurityAreaEffect();
        CloseExitedBoundaryEffect();
        OpenAllBoundaryEffect();
        SwitchVSTState(false);
        PlaySound();
    }
    void CloseSecurityAreaEffect()
    {
        if (m_SecurityAreaEffect != null)
        {
            Destroy(m_SecurityAreaEffect);
            m_SecurityAreaEffect = null;
        }
    }
    void CloseExitedBoundaryEffect()
    {
        if (m_Coroutine != null)
            StopCoroutine(m_Coroutine);
        CloseSecurityArrawEffect();
        CloseSecurityBoundaryUI();
    }
    void CloseSecurityArrawEffect()
    {
        if (m_SecurityArrawEffect != null)
        {
            Destroy(m_SecurityArrawEffect);
            m_SecurityArrawEffect = null;
        }
    }
    void CloseSecurityBoundaryUI()
    {
        if (m_SecurityBoundaryUI != null)
        {
            Destroy(m_SecurityBoundaryUI);
            m_SecurityBoundaryUI = null;
        }
    }
    void PlaySound()
    {
        if (m_needPlayEnterSound)
        {
            m_needPlayEnterSound = false;
            SecurityBoundarySound.Instance.PlaySound("Ocean_Game_Collect_Air_Bubble");
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///ExitedBoundary
    public void ExitedBoundary()
    {
        OpenSecurityAreaEffect();
        OpenExitedBoundaryEffect();
        CloseAllBoundaryEffect();
        SwitchVSTState(true);
        m_needPlayEnterSound = true;
    }
    void OpenSecurityAreaEffect()
    {
        if (m_SecurityAreaEffect == null)
        {
            m_SecurityAreaEffect = LoadPrefab("SecurityAreaEffect");
            m_SecurityAreaEffect.transform.position = areaCenterTrans.position;
            m_SecurityAreaEffect.transform.rotation = areaCenterTrans.rotation;
        }
    }
    void OpenExitedBoundaryEffect()
    {
        if (m_Coroutine != null)
            StopCoroutine(m_Coroutine);
        m_Coroutine = StartCoroutine(ExitedBoundaryEffect());
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///EnteredBoundary&ExitedBoundary
    void OpenAllBoundaryEffect()
    {
        foreach (var b in m_BoundaryEffects)
        {
            b.Value.SetActive(true);
        }
    }
    void CloseAllBoundaryEffect()
    {
        foreach (var b in m_BoundaryEffects)
        {
            b.Value.SetActive(false);
        }
    }
    IEnumerator ExitedBoundaryEffect()
    {
        CreateSecurityArrawEffect();
        CreateSecurityBoundaryUI();
        while (true)
        {
            DisplayArrowEffect();
            DisplayUI();
            yield return null;
        }
    }
    void CreateSecurityArrawEffect()
    {
        if (m_SecurityArrawEffect == null)
        {
            m_SecurityArrawEffect = LoadPrefab("SecurityArrowEffect");
        }
    }
    void CreateSecurityBoundaryUI()
    {
        if (m_SecurityBoundaryUI == null)
        {
            m_SecurityBoundaryUI = LoadPrefab("SecurityBoundaryUI");
            m_SecurityBoundaryUI.transform.position = GetUITargetPosition();
            var direction = m_SecurityBoundaryUI.transform.position - MainCamera.transform.position;
            m_SecurityBoundaryUI.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    void DisplayArrowEffect()
    {
        if (m_SecurityArrawEffect != null)
        {
            var direction = areaCenterTrans.transform.position - MainCamera.transform.position;
            direction.y = 0;
            var playerPosition = new Vector3(MainCamera.transform.position.x, 0, MainCamera.transform.position.z);
            m_SecurityArrawEffect.transform.position = playerPosition + direction.normalized * 0.20f + Vector3.up * 0.1f;
            m_SecurityArrawEffect.transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    void DisplayUI()
    {
        if (m_SecurityBoundaryUI != null)
        {
            var uiTargetPosition = GetUITargetPosition();
            var direction = m_SecurityBoundaryUI.transform.position - MainCamera.transform.position;
            var uiTargetRotation = Quaternion.LookRotation(direction);
            m_SecurityBoundaryUI.transform.rotation = Quaternion.Lerp(m_SecurityBoundaryUI.transform.rotation, uiTargetRotation, Time.deltaTime * 3f);
            m_SecurityBoundaryUI.transform.position = Vector3.Lerp(m_SecurityBoundaryUI.transform.position, uiTargetPosition, Time.deltaTime * 3);
        }
    }
    Vector3 GetUITargetPosition()
    {
        var position = MainCamera.transform.position;
        position.y = 0.8f;
        var positionOffset = MainCamera.transform.forward;
        positionOffset.y = 0;
        return position + positionOffset;
    }
    void SwitchVSTState(bool state)
    {
        SecurityBoundaryVST.SwitchVSTState(state);
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///NearedBoundary
    public void ApproachedBoundary(List<MinDistanceInfo> pointsInfo)
    {
        DestroyUselessEffect(pointsInfo);
        UpdateBoundaryEffect(pointsInfo);
    }

    List<float> uselessBoundaryEffectTags = new List<float>();
    void DestroyUselessEffect(List<MinDistanceInfo> distanceInfo)
    {
        uselessBoundaryEffectTags.Clear();
        foreach (var b in m_BoundaryEffects)
        {
            if (!distanceInfo.Exists(p => p.tag == b.Key))
            {
                uselessBoundaryEffectTags.Add(b.Key);
            }
        }
        foreach (var tag in uselessBoundaryEffectTags)
        {
            Destroy(m_BoundaryEffects[tag]);
            m_BoundaryEffects.Remove(tag);
        }
    }
    void UpdateBoundaryEffect(List<MinDistanceInfo> pointsInfo)
    {
        foreach (var p in pointsInfo)
        {
            var targetPosition = new Vector3(p.nearestPoint.x, MainCamera.transform.position.y, p.nearestPoint.y);
            var targetPositionOffset = (targetPosition - MainCamera.transform.position).normalized * 0.1f;
            targetPosition += targetPositionOffset;
            if (!m_BoundaryEffects.ContainsKey(p.tag))
            {
                var boundaryEffect = LoadPrefab("SecurityBoundaryEffect");
                boundaryEffect.transform.GetChild(0).gameObject.AddComponent<SecurityBoundaryTouch>();
                m_BoundaryEffects[p.tag] = boundaryEffect;
                m_BoundaryEffects[p.tag].transform.position = targetPosition;
            }
            m_BoundaryEffects[p.tag].transform.position = Vector3.Lerp(m_BoundaryEffects[p.tag].transform.position, targetPosition, Time.deltaTime * 2);
            m_BoundaryEffects[p.tag].transform.rotation = Quaternion.LookRotation(new Vector3(p.projectedLineDirection.x, 0, p.projectedLineDirection.y));
            var eulerAngles = m_BoundaryEffects[p.tag].transform.eulerAngles;
            m_BoundaryEffects[p.tag].transform.eulerAngles = new Vector3(eulerAngles.x, eulerAngles.y - 90, eulerAngles.z);
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///MovedAwayFromBoundary
    public void LeftBoundary()
    {
        foreach (var b in m_BoundaryEffects)
        {
            Destroy(b.Value);
        }
        m_BoundaryEffects.Clear();
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    GameObject LoadPrefab(string name)
    {
        return Instantiate(Resources.Load<GameObject>("SecurityBoundary/Prefebs/" + name));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Pause()
    {
        ClearCoroutine();
        ClearResource();
    }
    public void Close()
    {
        areaCenterTrans = null;
        m_MainCamera = null;
        ClearCoroutine();
        ClearResource();
        //SwitchVSTState(false);
    }
    void ClearCoroutine()
    {
        if (m_Coroutine != null)
        {
            StopCoroutine(m_Coroutine);
            m_Coroutine = null;
        }
    }
    void ClearResource()
    {
        if (m_SecurityAreaEffect != null)
        {
            Destroy(m_SecurityAreaEffect);
            m_SecurityAreaEffect = null;
        }
        if (m_SecurityArrawEffect != null)
        {
            Destroy(m_SecurityArrawEffect);
            m_SecurityArrawEffect = null;
        }
        if (m_SecurityBoundaryUI! != null)
        {
            Destroy(m_SecurityBoundaryUI);
            m_SecurityBoundaryUI = null;
        }
        if (m_BoundaryEffects != null)
        {
            foreach (var b in m_BoundaryEffects)
                Destroy(b.Value);
            m_BoundaryEffects.Clear();
        }
    }
    void OnDestroy()
    {
        Close();
    }
}