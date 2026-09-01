using UnityEngine;

public class UIDissolveController : MonoBehaviour
{
    public Material dissolveMaterial;
    [Range(0f, 1f)]
    public float dissolveAmount = 0f;
    public float dissolveSpeed = 0.5f;
    private bool isDissolving = false;

    void Start()
    {
        dissolveAmount = 0f; // 确保初始是完整显示
        dissolveMaterial.SetFloat("_Cutoff", dissolveAmount);
    }

    void Update()
    {
        if (isDissolving)
        {
            dissolveAmount += Time.deltaTime * dissolveSpeed;
            dissolveAmount = Mathf.Clamp01(dissolveAmount);
            dissolveMaterial.SetFloat("_Cutoff", dissolveAmount);

            if (dissolveAmount >= 1f)
                isDissolving = false;
        }
    }

    // 点击按钮后启动溶解
    public void StartDissolve()
    {
        isDissolving = true;
    }
}