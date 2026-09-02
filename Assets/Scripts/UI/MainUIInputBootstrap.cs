using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>运行时保证场景里有 EventSystem；不重复创建。</summary>
  [DisallowMultipleComponent]
  public sealed class MainUIInputBootstrap : MonoBehaviour
  {
    private void Awake() => EnsureEventSystem();

    public static void EnsureEventSystem()
    {
      var existing = EventSystem.current;
      if (existing == null)
        existing = Object.FindFirstObjectByType<EventSystem>();

      if (existing != null)
      {
        if (existing.GetComponent<StandaloneInputModule>() == null)
          existing.gameObject.AddComponent<StandaloneInputModule>();
        return;
      }

      var es = new GameObject("EventSystem");
      es.AddComponent<EventSystem>();
      es.AddComponent<StandaloneInputModule>();
    }
  }
}
