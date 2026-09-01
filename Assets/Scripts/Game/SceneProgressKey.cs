namespace UniverIdle.Game
{
  public static class SceneProgressKey
  {
    public static string Make(string workId, string sceneId) => workId + ":" + sceneId;
  }
}
