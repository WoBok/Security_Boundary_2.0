using UnityEngine;

public class SecurityBoundaryTest : MonoBehaviour
{
    public Transform boundaryRootTrans;
    void Awake()
    {
        SecurityBoundaryManager.Instance.Open(boundaryRootTrans, 0.5f);
    }
}