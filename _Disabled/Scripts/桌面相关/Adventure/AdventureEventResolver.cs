using System;
using System.Collections.Generic;
using DesktopPet;
using DesktopPet.Environment;
using DesktopPet.Luby;
using UnityEngine;

namespace DesktopPet.Adventure
{
    /// <summary>探险区域/事件抽取与金币结算（无 MonoBehaviour）。</summary>
    public static class AdventureEventResolver
    {
        private static readonly System.Random Rng = new System.Random();

        public sealed class TripPick
        {
            public AdventureRegionDefinition Region;
            public AdventureEventDefinition Event;
        }

        public static string TodayKeyLocal()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        public static void EnsureDayCounters(LubyInstanceData data)
        {
            if (data == null)
                return;
            string today = TodayKeyLocal();
            if (data.adventureDayKey == today)
                return;
            data.adventureDayKey = today;
            data.adventureTripsToday = 0;
        }

        public static bool IsSoftCapped(LubyInstanceData data, AdventureEventCatalog catalog)
        {
            EnsureDayCounters(data);
            int cap = catalog != null ? Mathf.Max(1, catalog.dailySoftCapTrips) : 6;
            return data != null && data.adventureTripsToday >= cap;
        }

        /// <summary>先抽区域，再在该区事件池内抽事件。</summary>
        public static TripPick PickTrip(AdventureEventCatalog catalog, LubyInstanceComponent luby)
        {
            var pick = new TripPick();
            if (catalog?.regions != null && catalog.regions.Count > 0)
            {
                pick.Region = PickRegion(catalog, luby);
                if (pick.Region != null)
                    pick.Event = PickEventInPool(pick.Region.events, luby);
            }

            if (pick.Event == null)
                pick.Event = PickEventInPool(catalog?.events, luby);

            return pick;
        }

        private static AdventureRegionDefinition PickRegion(
            AdventureEventCatalog catalog,
            LubyInstanceComponent luby)
        {
            if (catalog?.regions == null || catalog.regions.Count == 0)
                return null;

            ResolveTraitFlags(luby, out string personalityId, out bool hasCoin, out bool hasSleepy,
                out bool hasRain, out bool hasFoodie, out bool hasNight);

            bool isNight = IsNightNow();
            bool isRain = IsRainNow();

            float total = 0f;
            List<AdventureRegionDefinition> pool = new List<AdventureRegionDefinition>(catalog.regions.Count);
            List<float> weights = new List<float>(catalog.regions.Count);

            for (int i = 0; i < catalog.regions.Count; i++)
            {
                AdventureRegionDefinition r = catalog.regions[i];
                if (r == null || string.IsNullOrEmpty(r.regionId) || r.weight <= 0f)
                    continue;
                if (r.requireNight && !isNight)
                    continue;
                if (r.requireRain && !isRain)
                    continue;

                float w = r.ResolveWeight(
                    personalityId, hasCoin, hasSleepy, hasRain, hasFoodie, hasNight);
                if (w <= 0f)
                    continue;

                pool.Add(r);
                weights.Add(w);
                total += w;
            }

            if (pool.Count == 0 || total <= 0f)
                return catalog.regions.Find(r => r != null && r.weight > 0f);

            double roll = Rng.NextDouble() * total;
            double acc = 0d;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += weights[i];
                if (roll <= acc)
                    return pool[i];
            }

            return pool[pool.Count - 1];
        }

        private static AdventureEventDefinition PickEventInPool(
            IList<AdventureEventDefinition> source,
            LubyInstanceComponent luby)
        {
            if (source == null || source.Count == 0)
                return null;

            ResolveTraitFlags(luby, out string personalityId, out bool hasCoin, out bool hasSleepy,
                out bool hasRain, out bool hasFoodie, out bool hasNight);

            bool isNight = IsNightNow();
            bool isRain = IsRainNow();

            float total = 0f;
            List<AdventureEventDefinition> pool = new List<AdventureEventDefinition>(source.Count);
            List<float> weights = new List<float>(source.Count);

            for (int i = 0; i < source.Count; i++)
            {
                AdventureEventDefinition e = source[i];
                if (e == null || string.IsNullOrEmpty(e.eventId) || e.weight <= 0f)
                    continue;
                if (e.requireNight && !isNight)
                    continue;
                if (e.requireRain && !isRain)
                    continue;

                float w = e.ResolveWeight(
                    personalityId, hasCoin, hasSleepy, hasRain, hasFoodie, hasNight);
                if (w <= 0f)
                    continue;

                pool.Add(e);
                weights.Add(w);
                total += w;
            }

            if (pool.Count == 0 || total <= 0f)
            {
                for (int i = 0; i < source.Count; i++)
                {
                    AdventureEventDefinition e = source[i];
                    if (e == null || e.requireNight || e.requireRain || e.weight <= 0f)
                        continue;
                    return e;
                }

                for (int i = 0; i < source.Count; i++)
                {
                    if (source[i] != null)
                        return source[i];
                }

                return null;
            }

            double roll = Rng.NextDouble() * total;
            double acc = 0d;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += weights[i];
                if (roll <= acc)
                    return pool[i];
            }

            return pool[pool.Count - 1];
        }

        public static int ResolveGold(
            AdventureEventDefinition evt,
            AdventureEventCatalog catalog,
            LubyInstanceData data,
            LubyInstanceComponent luby,
            out bool softCapped)
        {
            softCapped = IsSoftCapped(data, catalog);
            int gold;
            if (softCapped)
            {
                int lo = catalog != null ? catalog.softCapGoldMin : 0;
                int hi = catalog != null ? catalog.softCapGoldMax : 1;
                if (hi < lo)
                    hi = lo;
                gold = lo >= hi ? lo : lo + Rng.Next(hi - lo + 1);
            }
            else if (evt != null)
            {
                gold = evt.RollGold(Rng);
            }
            else
            {
                gold = 0;
            }

            if (gold > 0 && HasCoinGreedy(data, luby))
            {
                float mul = catalog != null ? catalog.coinGreedyGoldMul : 1.2f;
                gold = Mathf.Max(1, Mathf.CeilToInt(gold * mul));
            }

            return Mathf.Max(0, gold);
        }

        /// <summary>回桌结算：查事件、发金币、记当日趟数。仓库态无 Component 也可。</summary>
        public static void Settle(
            AdventureEventCatalog catalog,
            LubyInstanceData data,
            out AdventureEventDefinition evt,
            out int gold,
            out bool softCapped)
        {
            evt = catalog != null && data != null
                ? catalog.FindById(data.adventureEventId)
                : null;
            gold = ResolveGold(evt, catalog, data, luby: null, out softCapped);
            if (gold > 0)
                DesktopPetServices.Shop?.Wallet?.Add(gold);

            EnsureDayCounters(data);
            if (data != null)
                data.adventureTripsToday = Mathf.Max(0, data.adventureTripsToday) + 1;
        }

        public static string ResolveRegionDisplayName(
            AdventureEventCatalog catalog,
            string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return string.Empty;
            AdventureRegionDefinition region = catalog?.FindRegionById(regionId);
            return region != null && !string.IsNullOrEmpty(region.displayName)
                ? region.displayName
                : regionId;
        }

        private static void ResolveTraitFlags(
            LubyInstanceComponent luby,
            out string personalityId,
            out bool hasCoin,
            out bool hasSleepy,
            out bool hasRain,
            out bool hasFoodie,
            out bool hasNight)
        {
            personalityId = luby != null ? luby.ResolvePersonalityId() : null;
            hasCoin = luby != null && luby.HasTrait("trait_coin_greedy");
            hasSleepy = luby != null && luby.HasTrait("trait_sleepy");
            hasRain = luby != null && luby.HasTrait("trait_rain_play");
            hasFoodie = luby != null && luby.HasTrait("trait_foodie");
            hasNight = luby != null && luby.HasTrait("trait_night_owl");
        }

        private static bool HasCoinGreedy(LubyInstanceData data, LubyInstanceComponent luby)
        {
            if (luby != null && luby.HasTrait("trait_coin_greedy"))
                return true;
            if (data == null)
                return false;
            return data.traitId == "trait_coin_greedy" || data.traitId2 == "trait_coin_greedy";
        }

        private static bool IsNightNow()
        {
            EnvironmentManager env = DesktopPetServices.Environment;
            return env?.DayNight != null && env.DayNight.CurrentPhase == DayNightPhase.Night;
        }

        private static bool IsRainNow()
        {
            EnvironmentManager env = DesktopPetServices.Environment;
            WeatherDefinition w = env?.Weather?.CurrentWeather;
            if (w == null || string.IsNullOrEmpty(w.weatherId))
                return false;
            return w.weatherId == "rainy" || w.weatherId == "stormy";
        }
    }
}
