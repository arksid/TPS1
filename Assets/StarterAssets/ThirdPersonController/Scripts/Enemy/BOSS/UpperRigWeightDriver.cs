using UnityEngine;
using UnityEngine.Animations.Rigging;

public class UpperRigWeightDriver : MonoBehaviour
{
    public Rig upperRig; public float speed = 5f; float target = 0f;
    public void SetAim(bool on) { target = on ? 1f : 0f; }
    void Update()
    {
        if (!upperRig) return;
        upperRig.weight = Mathf.MoveTowards(upperRig.weight, target, speed * Time.deltaTime);
    }
}
