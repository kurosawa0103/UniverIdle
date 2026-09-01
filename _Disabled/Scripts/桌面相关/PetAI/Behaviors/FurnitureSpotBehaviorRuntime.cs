using DesktopPet.Decor;
using DesktopPet.Luby;
using UnityEngine;

namespace DesktopPet.AI
{
    /// <summary>
    /// 走近指定 <see cref="DecorFurnitureKind"/> → 到位抬高再表演；无点/叠放则原地表演。
    /// Sleep(Bed) / 日后 Sit(Chair) 共用，子类只填 Kind 与速度参数。
    /// </summary>
    public abstract class FurnitureSpotBehaviorRuntime : TimedPetBehaviorRuntime
    {
        private enum Phase
        {
            UsingInPlace,
            Approaching,
            UsingOnSpot
        }

        private Phase _phase;
        private PetLocomotion _loco;
        private LubyInstanceComponent _luby;
        private PlacedDecor _spot;
        private string _claimedId;
        private float _targetX;
        private bool _useAnimApplied;

        protected FurnitureSpotBehaviorRuntime(PetBehaviorDefinition definition) : base(definition)
        {
        }

        protected abstract DecorFurnitureKind FurnitureKind { get; }
        protected abstract float MoveSpeed { get; }
        protected abstract float StopDistance { get; }
        protected virtual float ApproachTimeoutSeconds => 20f;

        public override bool WantsExit =>
            (_phase == Phase.UsingInPlace || _phase == Phase.UsingOnSpot) && Elapsed >= Duration;

        protected override void OnEnterInternal(PetBehaviorContext context)
        {
            _loco = context.Agent != null ? context.Agent.Locomotion : null;
            _luby = context.Agent != null
                ? context.Agent.GetComponent<LubyInstanceComponent>()
                : null;
            _spot = null;
            _claimedId = null;
            _useAnimApplied = false;

            if (_loco != null &&
                DecorFurnitureSpot.TryClaimNearest(_luby, FurnitureKind, out PlacedDecor spot, out float standX))
            {
                _spot = spot;
                _claimedId = spot.InstanceId;
                _targetX = standX;
                _phase = Phase.Approaching;
                PlayApproachAnim(context);
                _loco.SetMoveTarget(_targetX, MoveSpeed);
                return;
            }

            BeginUseInPlace(context, playUseAnimIfNeeded: true, resetElapsed: true);
        }

        protected override void OnTickInternal(PetBehaviorContext context)
        {
            if (_phase == Phase.UsingOnSpot)
            {
                if (_spot != null && SpotBlocked(_spot))
                    BeginUseInPlace(context, playUseAnimIfNeeded: false, resetElapsed: false);
                return;
            }

            if (_phase != Phase.Approaching)
                return;

            if (Elapsed >= ApproachTimeoutSeconds)
            {
                BeginUseInPlace(context, playUseAnimIfNeeded: true, resetElapsed: true);
                return;
            }

            if (_loco == null || _spot == null || !_spot.isActiveAndEnabled || SpotBlocked(_spot))
            {
                BeginUseInPlace(context, playUseAnimIfNeeded: true, resetElapsed: true);
                return;
            }

            _targetX = DecorFurnitureSpot.ResolveStandX(_spot);
            _loco.SetMoveTarget(_targetX, MoveSpeed);

            if (Mathf.Abs(_loco.transform.position.x - _targetX) > StopDistance)
                return;

            if (SpotBlocked(_spot))
            {
                BeginUseInPlace(context, playUseAnimIfNeeded: true, resetElapsed: true);
                return;
            }

            _loco.Stop();
            _phase = Phase.UsingOnSpot;
            Elapsed = 0f;
            _loco.SetSurfaceLiftToFeetY(DecorFurnitureSpot.ResolveFeetY(_spot));
            PlayUseAnim(context);
            _useAnimApplied = true;
            OnArrivedAtSpot(context, _spot);
        }

        protected override void OnExitInternal(PetBehaviorContext context)
        {
            _loco?.ClearSurfaceLift();
            _loco?.Stop();
            ReleaseClaim();
            _spot = null;
        }

        /// <summary>到位并抬高后；默认已播 Use 动画。</summary>
        protected virtual void OnArrivedAtSpot(PetBehaviorContext context, PlacedDecor spot)
        {
        }

        protected virtual void PlayApproachAnim(PetBehaviorContext context)
        {
            PetAnimatorDriver driver = context.Agent != null ? context.Agent.AnimatorDriver : null;
            if (driver == null)
                return;
            driver.SetTrigger("Walk");
            driver.SetFloat("Speed", 1f);
        }

        protected virtual void PlayUseAnim(PetBehaviorContext context)
        {
            PetAnimatorDriver driver = context.Agent != null ? context.Agent.AnimatorDriver : null;
            if (driver == null || Definition == null)
                return;
            if (!string.IsNullOrEmpty(Definition.animTrigger))
                driver.SetTrigger(Definition.animTrigger);
            if (!string.IsNullOrEmpty(Definition.animSpeedParam))
                driver.SetFloat(Definition.animSpeedParam, Definition.animSpeedValue);
        }

        /// <summary>
        /// 放弃当前家具落点，改原地表演。
        /// playUseAnimIfNeeded：尚未播过 Use 时补播；resetElapsed：走近失败时重计时长，睡着被叠则不重计。
        /// </summary>
        private void BeginUseInPlace(PetBehaviorContext context, bool playUseAnimIfNeeded, bool resetElapsed)
        {
            ReleaseClaim();
            _spot = null;
            _phase = Phase.UsingInPlace;
            _loco?.ClearSurfaceLift();
            _loco?.Stop();
            if (playUseAnimIfNeeded && !_useAnimApplied)
            {
                PlayUseAnim(context);
                _useAnimApplied = true;
            }

            if (resetElapsed)
                Elapsed = 0f;
        }

        private void ReleaseClaim()
        {
            if (string.IsNullOrEmpty(_claimedId))
                return;
            DecorFurnitureSpot.Release(_claimedId, _luby);
            _claimedId = null;
        }

        private static bool SpotBlocked(PlacedDecor spot)
        {
            DecorWorld world = DesktopPetServices.DecorWorld;
            return world != null && world.HasStackedChildren(spot);
        }
    }
}
