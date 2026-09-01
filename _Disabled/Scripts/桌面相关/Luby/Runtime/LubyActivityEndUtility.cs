using System;
using System.Collections.Generic;

namespace DesktopPet.Luby
{
    /// <summary>
    /// 结束列表中“属于某只 luby 的 session”的通用循环，减少重复的倒序 RemoveAt/End 模式。
    /// </summary>
    public static class LubyActivityEndUtility
    {
        public static void EndAllForLuby<TSession>(
            List<TSession> sessions,
            LubyInstanceComponent luby,
            Func<TSession, LubyInstanceComponent> getSessionLuby,
            Action<TSession> endAction)
        {
            if (sessions == null || luby == null)
                return;
            if (getSessionLuby == null)
                throw new ArgumentNullException(nameof(getSessionLuby));

            for (int i = sessions.Count - 1; i >= 0; i--)
            {
                TSession s = sessions[i];
                if (s == null)
                {
                    sessions.RemoveAt(i);
                    continue;
                }

                if (getSessionLuby(s) != luby)
                    continue;

                endAction?.Invoke(s);
                sessions.RemoveAt(i);
            }
        }
    }
}

