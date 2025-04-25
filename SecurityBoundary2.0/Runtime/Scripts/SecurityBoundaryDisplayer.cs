using System.Collections;
using UnityEngine;

public class SecurityBoundaryDisplayer : MonoBehaviour
{
    [HideInInspector]
    public Vector3 areaCenterPosition;
    public Transform rootTrans;

    Vector2[] m_BoundaryLocalPoints;
    public Vector2[] boundaryLocalPoints
    {
        get => m_BoundaryLocalPoints;
        set
        {
            m_BoundaryLocalPoints = value;
            CreateMesh(value);
        }
    }

    Camera m_MainCamera;
    Camera MainCamera { get { if (m_MainCamera == null) m_MainCamera = Camera.main; return m_MainCamera; } }
    float CameraHeight { get => MainCamera.transform.position.y; }
    GameObject m_BoundaryObj;
    GameObject BoundaryObject
    {
        get
        {
            if (m_BoundaryObj == null)
            {
                m_BoundaryObj = new GameObject("Boundary", typeof(MeshFilter), typeof(MeshRenderer));
                m_BoundaryObj.layer = LayerMask.NameToLayer("SecurityBoundary");
                m_BoundaryObj.transform.SetParent(rootTrans.GetChild(0), false);
            }
            return m_BoundaryObj;
        }
    }
    Material m_BoundaryMaterial;
    Material BoundaryMaterial
    {
        get
        {
            if (m_BoundaryMaterial == null)
                m_BoundaryMaterial = new Material(Shader.Find("Shader Graphs/Boundary"));
            return m_BoundaryMaterial;
        }
    }
    float m_LenghtOfBoundary;

    GameObject m_SecurityArrawEffect;
    GameObject m_SecurityBoundaryUI;

    Coroutine m_ExitCoroutine;
    Coroutine m_ApproachCoroutine;
    Coroutine m_LeftCoroutine;

    bool m_IsVSTOpened;
    bool m_IsVSTClosed;
    bool m_IsCameraVSTOpend;
    bool m_IsCameraVSTClosed;
    bool m_IsApproached;
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///EnteredSecurityBoundary
    public void EnteredSecurityBoundary()
    {
        CloseExitedBoundaryIndicator();
    }
    void CloseExitedBoundaryIndicator()
    {
        if (m_ExitCoroutine != null)
            StopCoroutine(m_ExitCoroutine);
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
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///ExitedSecurityBoundary
    public void ExitedSecurityBoundary()
    {
        SwitchVSTState(true);
        SwitchCameraState(true);
        if (m_ApproachCoroutine != null)
            StopCoroutine(m_ApproachCoroutine);
        m_IsApproached = true;
        if (m_LeftCoroutine != null)
            StopCoroutine(m_LeftCoroutine);
        UpdateBoundaryHight();
        OpenExitedBoundaryIndicator();
        BoundaryMaterial.SetFloat("_Progress", 1);
        BoundaryMaterial.SetFloat("_Alpha", 1);
        BoundaryMaterial.SetFloat("_CurrentDistance", 0.3f);
    }
    void OpenExitedBoundaryIndicator()
    {
        if (m_ExitCoroutine != null)
            StopCoroutine(m_ExitCoroutine);
        m_ExitCoroutine = StartCoroutine(ExitedBoundaryIndicator());
    }
    IEnumerator ExitedBoundaryIndicator()
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
            m_SecurityArrawEffect = LoadPrefab("SecurityArrowEffect");
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
            var direction = areaCenterPosition - MainCamera.transform.position;
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
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///ApproachedSecurityBoundary
    public void ApproachedSecurityBoundary(MinDistanceInfo pointsInfo)
    {
        if (m_LeftCoroutine != null)
            StopCoroutine(m_LeftCoroutine);
        if (!m_IsApproached)
        {
            m_IsApproached = true;
            UpdateBoundaryHight();
            m_ApproachCoroutine = StartCoroutine(ShowBoundary(CalculateUVCenter(pointsInfo)));
        }
        if (!m_IsVSTOpened)
        {
            m_IsVSTOpened = true;
            //m_IsVSTClosed = false;
            SwitchVSTState(true);
        }
        if (pointsInfo.minDistance <= 0.3f)
        {
            if (!m_IsCameraVSTOpend)
            {
                m_IsCameraVSTOpend = true;
                m_IsCameraVSTClosed = false;
                SwitchCameraState(true);
            }
            var seeThroughtPosition = new Vector3(pointsInfo.nearestPoint.x, CameraHeight, pointsInfo.nearestPoint.y);
            BoundaryMaterial.SetVector("_Position", seeThroughtPosition);
            BoundaryMaterial.SetFloat("_CurrentDistance", pointsInfo.minDistance);
        }
        else
        {
            //if (!m_IsVSTClosed)
            //{
            //    m_IsVSTClosed = true;
            //    m_IsVSTOpened = false;
            //    SwitchVSTState(false);
            //}
            if (!m_IsCameraVSTClosed)
            {
                m_IsCameraVSTClosed = true;
                m_IsCameraVSTOpend = false;
                SwitchCameraState(false);
            }
        }
    }
    float CalculateUVCenter(MinDistanceInfo pointsInfo)
    {
        float uvLength = 0;
        for (var i = 0; i < pointsInfo.pointIndex; i++)
        {
            var pa = boundaryLocalPoints[i];
            var pb = boundaryLocalPoints[(i + 1) % boundaryLocalPoints.Length];
            uvLength += Vector2.Distance(pa, pb);
        }
        var nearestPointWS = new Vector3(pointsInfo.nearestPoint.x, 0, pointsInfo.nearestPoint.y);
        var nearestPointLS = rootTrans.GetChild(1).InverseTransformPoint(nearestPointWS);
        uvLength += Vector2.Distance(boundaryLocalPoints[pointsInfo.pointIndex], new Vector2(nearestPointLS.x, nearestPointLS.z));
        return uvLength / m_LenghtOfBoundary;
    }
    IEnumerator ShowBoundary(float center)
    {
        BoundaryMaterial.SetFloat("_Alpha", 1);
        BoundaryMaterial.SetFloat("_Center", Mathf.Min(center, 1));
        float progress = 0;
        while (progress <= 1)
        {
            BoundaryMaterial.SetFloat("_Progress", progress);
            progress += Time.deltaTime;
            yield return null;
        }
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    ///LeftSecurityBoundary
    public void LeftSecurityBoundary()
    {
        if (m_ApproachCoroutine != null)
            StopCoroutine(m_ApproachCoroutine);
        m_IsApproached = false;

        m_LeftCoroutine = StartCoroutine(HideBoundary());

        m_IsVSTOpened = false;
        SwitchVSTState(false);
    }
    IEnumerator HideBoundary()
    {
        float alpha = 1;
        while (alpha >= 0)
        {
            BoundaryMaterial.SetFloat("_Alpha", alpha);
            alpha -= Time.deltaTime;
            yield return null;
        }
        BoundaryMaterial.SetFloat("_Alpha", 0);
    }
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Pause()
    {
        ClearCoroutines();
        ClearExitedResoucre();
        BoundaryMaterial.SetFloat("_Progress", 0);
    }
    public void Close()
    {
        m_BoundaryLocalPoints = null;
        m_MainCamera = null;
        ClearCoroutines();
        ClearResource();
    }
    void ClearCoroutines()
    {
        ClearCoroutine(m_ExitCoroutine);
        ClearCoroutine(m_ApproachCoroutine);
        ClearCoroutine(m_ExitCoroutine);
    }
    void ClearCoroutine(Coroutine coroutine)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }
    void ClearResource()
    {
        DestroyResource(m_BoundaryObj);
        DestroyResource(m_BoundaryMaterial);
        ClearExitedResoucre();
    }
    void DestroyResource(Object obj)
    {
        if (obj != null)
        {
            Object.Destroy(obj);
            obj = null;
        }
    }
    void ClearExitedResoucre()
    {
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
    }
    GameObject LoadPrefab(string name)
    {
        var newObj = Instantiate(Resources.Load<GameObject>("SecurityBoundary/Prefebs/" + name));
        int securityBoundaryLayer = LayerMask.NameToLayer("SecurityBoundary");
        SetLayer(newObj, securityBoundaryLayer);
        return newObj;
    }
    void SetLayer(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayer(child.gameObject, layer);
    }
    void SwitchVSTState(bool state) => SecurityBoundaryVST.SwitchVSTState(state);
    void SwitchCameraState(bool state) => SecurityBoundaryVST.SwitchCameraState(state);
    void CreateMesh(Vector2[] boundaryLocalPoints)
    {
        var pointCount = boundaryLocalPoints.Length;

        //Vertices
        var vertices = new Vector3[(pointCount + 1) * 2];
        for (int i = 0; i < pointCount; i++)
        {
            var pointPos = boundaryLocalPoints[i];
            vertices[i] = new Vector3(pointPos.x, 0, pointPos.y);
            vertices[i + pointCount + 1] = vertices[i] + new Vector3(0, 1, 0);
        }
        vertices[pointCount] = vertices[0];
        vertices[pointCount * 2 + 1] = vertices[pointCount + 1];

        //Triangles
        var triangles = new int[(pointCount + 1) * 6];
        for (int i = 0; i < pointCount; i++)
        {
            var index = i * 6;
            triangles[index] = i;
            triangles[index + 1] = i + pointCount + 1;
            triangles[index + 2] = i + pointCount + 2;
            triangles[index + 3] = i + 1;
            triangles[index + 4] = i;
            triangles[index + 5] = i + pointCount + 2;
        }

        //UVs
        var uv = new Vector2[(pointCount + 1) * 2];
        var length = 0.0f;
        for (int i = 0; i < pointCount; i++)
        {
            var posA = boundaryLocalPoints[i];
            var posB = boundaryLocalPoints[(i + 1) % pointCount];
            var distance = Vector3.Distance(posA, posB);
            length += distance;
        }
        m_LenghtOfBoundary = length;
        var uvLength = 0.0f;
        for (int i = 1; i <= pointCount - 1; i++)
        {
            var posA = boundaryLocalPoints[i];
            var posB = boundaryLocalPoints[i - 1];
            var distance = Vector3.Distance(posA, posB);
            uvLength += distance;
            var u = uvLength / length;
            uv[i] = new Vector2(u, 0);
            uv[i + pointCount + 1] = new Vector2(u, 1);
        }
        uv[0] = new Vector2(0, 0);
        uv[pointCount + 1] = new Vector2(0, 1);
        uv[pointCount] = new Vector2(1, 0);
        uv[pointCount * 2 + 1] = new Vector2(1, 1);

        //Boundary GameObject
        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        var meshFilter = BoundaryObject.GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        var meshRenderer = BoundaryObject.GetComponent<MeshRenderer>();
        meshRenderer.material = BoundaryMaterial;
        BoundaryMaterial.SetFloat("_LineCount", length / 0.1f);
    }
    void UpdateBoundaryHight()
    {
        var boundaryPosition = BoundaryObject.transform.position;
        BoundaryObject.transform.position = new Vector3(boundaryPosition.x, CameraHeight - 0.5f, boundaryPosition.z);
    }
    void OnDestroy()
    {
        Close();
    }
}