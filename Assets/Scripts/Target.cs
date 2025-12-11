using UnityEngine;

public class Target : MonoBehaviour
{
    public void Hit()
    {
        // 先简单：被击中就销毁自己，后续可以在这里加特效、计分等
        Destroy(gameObject);
    }
}

