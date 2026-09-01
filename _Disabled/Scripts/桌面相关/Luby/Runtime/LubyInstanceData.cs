using System;
using System.Collections.Generic;
using DesktopPet.Save;

namespace DesktopPet.Luby
{
    [Serializable]
    public sealed class LubyInstanceData
    {
        public string instanceId;
        public string templateId;
        public string personalityId;
        public string traitId;
        /// <summary>第二特质；空=无双特质。</summary>
        public string traitId2;
        /// <summary>获得时随机生成的宠物名（展示用）。</summary>
        public string petName;
        /// <summary>外形 Prefab 名；对应模板外形池条目。</summary>
        public string appearanceKey;
        public float x;
        public float y;
        public float scale = 1f;
        /// <summary>UTC 秒时间戳；&gt;0 = 本趟探险尚未回桌（含已到期待走回）。0 = 未在探险。</summary>
        public double adventureEndsAtUtc;
        /// <summary>本趟抽中的事件 id；离桌期间保留，归来结算用。</summary>
        public string adventureEventId;
        /// <summary>本趟抽中的区域 id；离桌期间保留。</summary>
        public string adventureRegionId;
        /// <summary>本趟出发时的背景 id；回桌/占栏位只在同背景生效。</summary>
        public string adventureBackgroundId;
        /// <summary>上回事件 id（看板展示）。</summary>
        public string lastAdventureEventId;
        /// <summary>上回区域 id（看板展示）。</summary>
        public string lastAdventureRegionId;
        /// <summary>上回标题。</summary>
        public string lastAdventureTitle;
        /// <summary>上回获得金币。</summary>
        public int lastAdventureGold;
        /// <summary>上回结算 UTC 秒。</summary>
        public double lastAdventureEndedAtUtc;
        /// <summary>本地日键 yyyyMMdd；换日清零趟数。</summary>
        public string adventureDayKey;
        /// <summary>当日已结算探险趟数（软顶用）。</summary>
        public int adventureTripsToday;

        /// <summary>本趟离桌/回桌时使用的屏外 X；0 = 未记录。</summary>
        public float adventureExitX;

        /// <summary>近况日记（环形，最多见 LubyJournalService.MaxEntries）。</summary>
        public List<LubyJournalEntry> journalEntries = new List<LubyJournalEntry>();

        /// <summary>喜好计数（稀疏）。</summary>
        public List<LubyJournalLike> journalLikes = new List<LubyJournalLike>();

        /// <summary>本趟已出门、尚未回桌（倒计时中或已到期待走回）。仓库隐藏 / 禁止拖出用这个。</summary>
        public bool IsOnAdventureTrip => adventureEndsAtUtc > 0d;

        /// <summary>本趟是否绑定在指定背景（空背景 id 的旧档视为任意背景兼容）。</summary>
        public bool IsOnAdventureTripForBackground(string backgroundId)
        {
            if (!IsOnAdventureTrip)
                return false;
            if (string.IsNullOrEmpty(adventureBackgroundId))
                return true;
            return adventureBackgroundId == backgroundId;
        }

        /// <summary>仍在探险离桌倒计时中（尚未到归来时刻）。</summary>
        public bool IsAwayOnAdventure =>
            adventureEndsAtUtc > 0d && adventureEndsAtUtc > UtcNowUnixSeconds();

        public static double UtcNowUnixSeconds()
        {
            return (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        }

        public LubyInstanceData Clone()
        {
            var clone = new LubyInstanceData
            {
                instanceId = instanceId,
                templateId = templateId,
                personalityId = personalityId,
                traitId = traitId,
                traitId2 = traitId2,
                petName = petName,
                appearanceKey = appearanceKey,
                x = x,
                y = y,
                scale = scale,
                adventureEndsAtUtc = adventureEndsAtUtc,
                adventureEventId = adventureEventId,
                adventureRegionId = adventureRegionId,
                adventureBackgroundId = adventureBackgroundId,
                lastAdventureEventId = lastAdventureEventId,
                lastAdventureRegionId = lastAdventureRegionId,
                lastAdventureTitle = lastAdventureTitle,
                lastAdventureGold = lastAdventureGold,
                lastAdventureEndedAtUtc = lastAdventureEndedAtUtc,
                adventureDayKey = adventureDayKey,
                adventureTripsToday = adventureTripsToday,
                adventureExitX = adventureExitX
            };
            LubyJournalService.CopyJournal(this, clone);
            return clone;
        }

        public static LubyInstanceData FromSaveEntry(DesktopPetLubyEntry e)
        {
            if (e == null)
                return null;
            var data = new LubyInstanceData
            {
                instanceId = e.instanceId,
                templateId = e.templateId,
                personalityId = e.personalityId,
                traitId = e.traitId,
                traitId2 = e.traitId2,
                petName = e.petName,
                appearanceKey = e.appearanceKey,
                x = e.x,
                y = e.y,
                scale = e.scale > 0.01f ? e.scale : 1f,
                adventureEndsAtUtc = e.adventureEndsAtUtc,
                adventureEventId = e.adventureEventId,
                adventureRegionId = e.adventureRegionId,
                adventureBackgroundId = e.adventureBackgroundId,
                lastAdventureEventId = e.lastAdventureEventId,
                lastAdventureRegionId = e.lastAdventureRegionId,
                lastAdventureTitle = e.lastAdventureTitle,
                lastAdventureGold = e.lastAdventureGold,
                lastAdventureEndedAtUtc = e.lastAdventureEndedAtUtc,
                adventureDayKey = e.adventureDayKey,
                adventureTripsToday = e.adventureTripsToday,
                adventureExitX = e.adventureExitX
            };
            CopyJournalFromSave(e, data);
            return data;
        }

        public DesktopPetLubyEntry ToSaveEntry()
        {
            LubyJournalService.EnsureLists(this);
            var e = new DesktopPetLubyEntry
            {
                instanceId = instanceId,
                templateId = templateId,
                personalityId = personalityId,
                traitId = traitId,
                traitId2 = traitId2,
                petName = petName,
                appearanceKey = appearanceKey,
                x = x,
                y = y,
                scale = scale,
                adventureEndsAtUtc = adventureEndsAtUtc,
                adventureEventId = adventureEventId,
                adventureRegionId = adventureRegionId,
                adventureBackgroundId = adventureBackgroundId,
                lastAdventureEventId = lastAdventureEventId,
                lastAdventureRegionId = lastAdventureRegionId,
                lastAdventureTitle = lastAdventureTitle,
                lastAdventureGold = lastAdventureGold,
                lastAdventureEndedAtUtc = lastAdventureEndedAtUtc,
                adventureDayKey = adventureDayKey,
                adventureTripsToday = adventureTripsToday,
                adventureExitX = adventureExitX,
                journalEntries = new List<LubyJournalEntry>(),
                journalLikes = new List<LubyJournalLike>()
            };
            CopyJournalToSave(this, e);
            return e;
        }

        private static void CopyJournalFromSave(DesktopPetLubyEntry e, LubyInstanceData data)
        {
            LubyJournalService.EnsureLists(data);
            data.journalEntries.Clear();
            data.journalLikes.Clear();
            if (e == null)
                return;
            LubyJournalService.AppendJournalCopies(
                e.journalEntries, e.journalLikes, data.journalEntries, data.journalLikes);
        }

        private static void CopyJournalToSave(LubyInstanceData data, DesktopPetLubyEntry e)
        {
            if (data == null || e == null)
                return;
            LubyJournalService.AppendJournalCopies(
                data.journalEntries, data.journalLikes, e.journalEntries, e.journalLikes);
        }
    }
}
