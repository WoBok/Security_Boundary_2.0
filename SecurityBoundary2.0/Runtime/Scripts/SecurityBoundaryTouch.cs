using UnityEngine;

public class SecurityBoundaryTouch : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Hand")
        {
            var effect = Instantiate(Resources.Load<GameObject>("SecurityBoundary/Prefebs/SecurityTouchEffect"));
            effect.transform.SetParent(transform);
            effect.transform.position = other.transform.position;
            effect.transform.localEulerAngles = Vector3.zero;
            SecurityBoundarySound.Instance.PlaySound("Ocean_Game_Collect_Air_Bubble");
            Destroy(effect, 0.35f);
        }
    }
}