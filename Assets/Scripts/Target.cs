using UnityEngine;

public class Target : MonoBehaviour
{
    [Header("Physics")]
    public bool makeCollidersTriggers = true;

    void Awake()
    {
        if (!makeCollidersTriggers) return;

        // Targets shouldn't physically push the player around in an aim trainer.
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.isTrigger = true;
        }
    }

    public void Hit()
    {
        // 先简单：被击中就销毁自己，后续可以在这里加特效、计分等
        Destroy(gameObject);
    }
}
