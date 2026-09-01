using System;
using System.Collections.Generic;
using System.Text;
using DesktopPet;
using DesktopPet.Adventure;
using DesktopPet.Decor;
using DesktopPet.Save;
using DesktopPet.Shop;

namespace DesktopPet.Luby
{
    /// <summary>旁路日志：行为结束后写短句 + 喜好计数；不驱动 PetAI。</summary>
    public static class LubyJournalService
    {
        public const int MaxEntries = 8;
        public const int MaxLikes = 8;
        public const int DisplayEntryCount = 4;

        private const double CooldownRadio = 15 * 60;
        private const double CooldownWell = 15 * 60;
        /// <summary>出发可短冷却；归来每趟必记（见 RecordAdventureBack）。</summary>
        private const double CooldownAdventureGo = 25;
        private const double CooldownCoin = 10 * 60;
        private const double CooldownGreet = 12 * 60;
        private const double CooldownIdle = 6 * 60 * 60;

        public static void RecordDecor(LubyInstanceComponent luby, DecorInteractable decor)
        {
            if (luby?.Data == null || decor == null)
                return;

            PlacedDecor placed = decor.GetComponent<PlacedDecor>();
            ShopItemDefinition item = placed != null ? placed.Definition : null;
            string itemId = item != null ? item.itemId : null;
            if (string.IsNullOrEmpty(itemId))
                return;

            string kind;
            if (itemId == "decor_radio")
                kind = LubyJournalKinds.Radio;
            else if (itemId == "decor_well")
                kind = LubyJournalKinds.Well;
            else
                return;

            Record(
                luby.Data,
                kind,
                itemId,
                likeKey: "decor:" + itemId,
                likeScore: kind == LubyJournalKinds.Radio ? 2 : 1);
        }

        public static void RecordAdventureGo(LubyInstanceData data, string regionDisplayName, bool persist = true)
        {
            Record(data, LubyJournalKinds.AdventureGo, null, "act:adventure", 2, persist: false);

            if (!string.IsNullOrEmpty(regionDisplayName)
                && data?.journalEntries != null
                && data.journalEntries.Count > 0)
            {
                LubyJournalEntry last = data.journalEntries[data.journalEntries.Count - 1];
                if (last != null && last.kind == LubyJournalKinds.AdventureGo && !string.IsNullOrEmpty(last.text))
                    last.text = last.text.Replace("{region}", regionDisplayName);
            }

            if (persist)
                DesktopPetSaveMgr.PersistActive();
        }

        /// <summary>探险归来：写入事件文案 + 金币占位；每趟必记（不受近况冷却阻挡）。</summary>
        public static void RecordAdventureBack(
            LubyInstanceData data,
            AdventureEventDefinition evt,
            int gold,
            bool softCapped,
            string regionDisplayName,
            bool persist = true)
        {
            if (data == null)
                return;

            EnsureLists(data);
            double now = LubyInstanceData.UtcNowUnixSeconds();

            string kind = softCapped
                ? LubyJournalKinds.AdventureTired
                : (evt != null && !string.IsNullOrEmpty(evt.eventId)
                    ? evt.eventId
                    : LubyJournalKinds.AdventureBack);

            string title = evt != null && !string.IsNullOrEmpty(evt.title) ? evt.title : "探险";
            string region = string.IsNullOrEmpty(regionDisplayName) ? string.Empty : regionDisplayName;
            string text = LubyJournalLineTable.Pick(kind, data, null);
            if (string.IsNullOrEmpty(text) || text == LubyJournalLineTable.FallbackLine)
            {
                if (kind != LubyJournalKinds.AdventureBack && kind != LubyJournalKinds.AdventureTired)
                    text = LubyJournalLineTable.Pick(LubyJournalKinds.AdventureBack, data, null);
            }

            if (string.IsNullOrEmpty(text) || text == LubyJournalLineTable.FallbackLine)
            {
                text = softCapped
                    ? $"今天腿有点酸，还是出门晃了晃（{title}，+{gold} 金）。"
                    : string.IsNullOrEmpty(region)
                        ? $"探险回来了：{title}，带回 +{gold} 金。"
                        : $"从{region}回来了：{title}，带回 +{gold} 金。";
            }

            text = text
                .Replace("{title}", title)
                .Replace("{gold}", gold.ToString())
                .Replace("{region}", region);

            data.journalEntries.Add(new LubyJournalEntry
            {
                utcSeconds = now,
                kind = softCapped ? LubyJournalKinds.AdventureTired : LubyJournalKinds.AdventureBack,
                refId = evt != null ? evt.eventId ?? string.Empty : string.Empty,
                text = text
            });

            while (data.journalEntries.Count > MaxEntries)
                data.journalEntries.RemoveAt(0);

            AddLike(data, "act:adventure", softCapped ? 1 : 2);

            if (persist)
                DesktopPetSaveMgr.PersistActive();
        }

        public static void RecordCoin(LubyInstanceComponent luby, bool persist = true)
        {
            if (luby?.Data == null)
                return;
            Record(luby.Data, LubyJournalKinds.Coin, null, "act:coin", 1, persist: persist);
        }

        public static void RecordGreet(LubyInstanceComponent self, LubyInstanceComponent peer)
        {
            if (self?.Data == null || peer?.Data == null)
                return;
            if (self.InstanceId == peer.InstanceId)
                return;

            string peerName = peer.PetName;
            if (string.IsNullOrEmpty(peerName))
                peerName = "一位伙伴";

            Record(
                self.Data,
                LubyJournalKinds.Greet,
                peer.InstanceId,
                "luby:" + peer.InstanceId,
                2,
                peerDisplayName: peerName);
        }

        /// <summary>打开信息面板时：今日尚无条目则补一条想法。</summary>
        public static void MaybeIdleThought(LubyInstanceData data)
        {
            if (data == null)
                return;

            EnsureLists(data);
            double now = LubyInstanceData.UtcNowUnixSeconds();
            if (HasEntryToday(data, now))
                return;
            if (IsOnCooldown(data, LubyJournalKinds.IdleThought, now, CooldownIdle))
                return;

            Record(data, LubyJournalKinds.IdleThought, null, null, 0, persist: true);
        }

        public static string FormatSummary(LubyInstanceData data, LubyTemplateCatalog catalog)
        {
            if (data == null)
                return string.Empty;

            EnsureLists(data);
            var sb = new StringBuilder(160);

            int shown = 0;
            for (int i = data.journalEntries.Count - 1; i >= 0 && shown < DisplayEntryCount; i--)
            {
                LubyJournalEntry e = data.journalEntries[i];
                if (e == null || string.IsNullOrEmpty(e.text))
                    continue;
                if (shown == 0)
                    sb.Append("近况\n");
                sb.Append("· ").Append(e.text).Append('\n');
                shown++;
            }

            string likesLine = FormatLikesLine(data, catalog);
            if (!string.IsNullOrEmpty(likesLine))
            {
                if (shown > 0)
                    sb.Append('\n');
                sb.Append(likesLine);
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatLikesLine(LubyInstanceData data, LubyTemplateCatalog catalog)
        {
            if (data.journalLikes == null || data.journalLikes.Count == 0)
                return string.Empty;

            SortLikesDesc(data.journalLikes);
            var parts = new List<string>(2);
            for (int i = 0; i < data.journalLikes.Count && parts.Count < 2; i++)
            {
                LubyJournalLike like = data.journalLikes[i];
                if (like == null || like.score <= 0 || string.IsNullOrEmpty(like.key))
                    continue;
                string label = ResolveLikeLabel(like.key, catalog);
                if (string.IsNullOrEmpty(label))
                    continue;
                if (like.key.StartsWith("luby:", StringComparison.Ordinal))
                    parts.Add(LubyJournalLineTable.PickLikeFormat("like_luby", label));
                else if (like.key.StartsWith("decor:", StringComparison.Ordinal))
                    parts.Add(LubyJournalLineTable.PickLikeFormat("like_decor", label));
                else if (like.key == "act:adventure")
                    parts.Add(LubyJournalLineTable.PickLikeFormat("like_adventure", label));
                else if (like.key == "act:coin")
                    parts.Add(LubyJournalLineTable.PickLikeFormat("like_coin", label));
                else
                    parts.Add(LubyJournalLineTable.PickLikeFormat("like_decor", label));
            }

            if (parts.Count == 0)
                return string.Empty;
            string header = LubyJournalLineTable.PickLikeFormat("like_header", string.Empty);
            return header + string.Join("；", parts);
        }

        private static string ResolveLikeLabel(string key, LubyTemplateCatalog catalog)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            if (key.StartsWith("decor:", StringComparison.Ordinal))
            {
                string itemId = key.Substring("decor:".Length);
                ShopManager shop = DesktopPetServices.Shop;
                ShopItemDefinition item = shop != null && shop.Catalog != null
                    ? shop.Catalog.FindById(itemId)
                    : null;
                if (item != null && !string.IsNullOrEmpty(item.displayName))
                    return item.displayName;
                return "某件装饰";
            }

            if (key.StartsWith("luby:", StringComparison.Ordinal))
            {
                string id = key.Substring("luby:".Length);
                LubyWorld world = DesktopPetServices.LubyWorld;
                if (world != null)
                {
                    LubyInstanceComponent live = world.FindDeskById(id);
                    if (live != null)
                        return live.PetName;

                    LubyInstanceData wh = world.FindWarehouseById(id);
                    if (wh != null)
                        return LubyDisplayNames.ResolvePetName(wh, catalog ?? world.Catalog);
                }

                return "一位老朋友";
            }

            if (key == "act:adventure") return "探险";
            if (key == "act:coin") return "捡金币";
            return key;
        }

        private static void Record(
            LubyInstanceData data,
            string kind,
            string refId,
            string likeKey,
            int likeScore,
            string peerDisplayName = null,
            bool persist = true)
        {
            if (data == null || string.IsNullOrEmpty(kind))
                return;

            EnsureLists(data);
            double now = LubyInstanceData.UtcNowUnixSeconds();
            double cd = CooldownFor(kind);
            if (IsOnCooldown(data, kind, now, cd))
                return;

            string text = LubyJournalLineTable.Pick(kind, data, peerDisplayName);
            data.journalEntries.Add(new LubyJournalEntry
            {
                utcSeconds = now,
                kind = kind,
                refId = refId ?? string.Empty,
                text = text
            });

            while (data.journalEntries.Count > MaxEntries)
                data.journalEntries.RemoveAt(0);

            if (!string.IsNullOrEmpty(likeKey) && likeScore > 0)
                AddLike(data, likeKey, likeScore);

            if (persist)
                DesktopPetSaveMgr.PersistActive();
        }

        private static double CooldownFor(string kind)
        {
            switch (kind)
            {
                case LubyJournalKinds.Radio: return CooldownRadio;
                case LubyJournalKinds.Well: return CooldownWell;
                case LubyJournalKinds.AdventureGo: return CooldownAdventureGo;
                case LubyJournalKinds.AdventureBack:
                case LubyJournalKinds.AdventureTired: return 0;
                case LubyJournalKinds.Coin: return CooldownCoin;
                case LubyJournalKinds.Greet: return CooldownGreet;
                case LubyJournalKinds.IdleThought: return CooldownIdle;
                default: return 10 * 60;
            }
        }

        private static bool IsOnCooldown(
            LubyInstanceData data,
            string kind,
            double now,
            double cooldownSeconds)
        {
            if (data.journalEntries == null)
                return false;
            for (int i = data.journalEntries.Count - 1; i >= 0; i--)
            {
                LubyJournalEntry e = data.journalEntries[i];
                if (e == null || e.kind != kind)
                    continue;
                return now - e.utcSeconds < cooldownSeconds;
            }

            return false;
        }

        private static bool HasEntryToday(LubyInstanceData data, double nowUtc)
        {
            DateTime today = DateTime.UtcNow.Date;
            for (int i = 0; i < data.journalEntries.Count; i++)
            {
                LubyJournalEntry e = data.journalEntries[i];
                if (e == null)
                    continue;
                var t = DateTime.UnixEpoch.AddSeconds(e.utcSeconds);
                if (t.Date == today)
                    return true;
            }

            return false;
        }

        private static void AddLike(LubyInstanceData data, string key, int score)
        {
            for (int i = 0; i < data.journalLikes.Count; i++)
            {
                LubyJournalLike like = data.journalLikes[i];
                if (like != null && like.key == key)
                {
                    like.score += score;
                    TrimLikes(data);
                    return;
                }
            }

            data.journalLikes.Add(new LubyJournalLike { key = key, score = score });
            TrimLikes(data);
        }

        private static void TrimLikes(LubyInstanceData data)
        {
            if (data.journalLikes.Count <= MaxLikes)
                return;
            SortLikesDesc(data.journalLikes);
            while (data.journalLikes.Count > MaxLikes)
                data.journalLikes.RemoveAt(data.journalLikes.Count - 1);
        }

        private static void SortLikesDesc(List<LubyJournalLike> likes)
        {
            likes.Sort((a, b) =>
            {
                int sa = a != null ? a.score : 0;
                int sb = b != null ? b.score : 0;
                return sb.CompareTo(sa);
            });
        }

        public static void EnsureLists(LubyInstanceData data)
        {
            if (data == null)
                return;
            if (data.journalEntries == null)
                data.journalEntries = new List<LubyJournalEntry>();
            if (data.journalLikes == null)
                data.journalLikes = new List<LubyJournalLike>();
        }

        public static void CopyJournal(LubyInstanceData from, LubyInstanceData to)
        {
            if (to == null)
                return;
            EnsureLists(to);
            to.journalEntries.Clear();
            to.journalLikes.Clear();
            AppendJournalCopies(from?.journalEntries, from?.journalLikes, to.journalEntries, to.journalLikes);
        }

        public static void AppendJournalCopies(
            IList<LubyJournalEntry> fromEntries,
            IList<LubyJournalLike> fromLikes,
            List<LubyJournalEntry> toEntries,
            List<LubyJournalLike> toLikes)
        {
            if (toEntries != null && fromEntries != null)
            {
                for (int i = 0; i < fromEntries.Count; i++)
                {
                    LubyJournalEntry e = fromEntries[i];
                    if (e == null)
                        continue;
                    toEntries.Add(new LubyJournalEntry
                    {
                        utcSeconds = e.utcSeconds,
                        kind = e.kind,
                        refId = e.refId,
                        text = e.text
                    });
                }
            }

            if (toLikes != null && fromLikes != null)
            {
                for (int i = 0; i < fromLikes.Count; i++)
                {
                    LubyJournalLike like = fromLikes[i];
                    if (like == null)
                        continue;
                    toLikes.Add(new LubyJournalLike
                    {
                        key = like.key,
                        score = like.score
                    });
                }
            }
        }
    }
}
