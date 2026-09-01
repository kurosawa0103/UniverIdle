using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UniverIdle.UI
{
  /// <summary>保证场景里有可工作的 EventSystem（挂 Canvas 根上）。</summary>
  [DisallowMultipleComponent]
  public sealed class MainUIInputBootstrap : MonoBehaviour
  {
    private void Awake() => EnsureEventSystem();

    public static void EnsureEventSystem()
    {
      if (EventSystem.current != null)
      {
        if (EventSystem.current.GetComponent<StandaloneInputModule>() == null)
          EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
        return;
      }

      var es = new GameObject("EventSystem");
      es.AddComponent<EventSystem>();
      es.AddComponent<StandaloneInputModule>();
    }
  }
}
