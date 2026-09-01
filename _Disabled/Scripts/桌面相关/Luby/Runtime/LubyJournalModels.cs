using System;

namespace DesktopPet.Luby
{
    [Serializable]
    public sealed class LubyJournalEntry
    {
        public double utcSeconds;
        public string kind;
        public string refId;
        public string text;
    }

    [Serializable]
    public sealed class LubyJournalLike
    {
        public string key;
        public int score;
    }

    /// <summary>日志事件 kind 常量（存档与模板共用）。</summary>
    public static class LubyJournalKinds
    {
        public const string Radio = "radio";
        public const string Well = "well";
        public const string AdventureGo = "adventure_go";
        public const string AdventureBack = "adventure_back";
        public const string AdventureTired = "adventure_tired";
        public const string Coin = "coin";
        public const string Greet = "greet";
        public const string IdleThought = "idle_thought";
    }
}
