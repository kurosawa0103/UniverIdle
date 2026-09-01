using Sirenix.OdinInspector;
using UnityEngine;
/// <summary>
/// 统一地面高度：装饰贴地与 Luby 出生共用同一套规则。
/// 优先地面 Collider 顶边 → 手动 Y →（无 Collider 且关手动时）视口底。
/// 水平活动宽见 DesktopPetPlayfieldBounds（含 padding），不由此组件另算一套。
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class DesktopPetGround : MonoBehaviour
{
    [Title("桌宠地面", "装饰与 Luby 共用")]
    [LabelText("地面碰撞盒")]
    [Tooltip("脚底贴其顶边。推荐绑场景地面条 BoxCollider2D。")]
    [SerializeField]
    private Collider2D groundCollider;

    [LabelText("使用手动地面 Y")]
    [SerializeField]
    private bool useManualGroundY = true;

    [ShowIf(nameof(useManualGroundY))]
    [LabelText("手动地面 Y")]
    [SerializeField]
    private float manualGroundY = DesktopPetServices.DefaultManualGroundY;

    private void Awake()
    {
        if (DesktopPetServices.Ground != null && DesktopPetServices.Ground != this)
        {
            Debug.LogWarning("[DesktopPetGround] 场景中已有地面组件，忽略重复实例。");
            return;
        }

        DesktopPetServices.RegisterGround(this);
        DesktopPetPlayfieldBounds.RefreshGlobal();
    }

    private void OnDestroy()
    {
        DesktopPetServices.UnregisterGround(this);
    }

    public float ResolveGroundY()
    {
        if (groundCollider != null)
            return groundCollider.bounds.max.y;

        if (useManualGroundY)
            return manualGroundY;

        Camera cam = Camera.main;
        if (cam == null)
            return manualGroundY;

        Vector3 bottom = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, Mathf.Abs(cam.transform.position.z)));
        return bottom.y;
    }

    public Collider2D GroundCollider => groundCollider;
}
