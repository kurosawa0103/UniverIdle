namespace DesktopPet.Luby
{
    /// <summary>
    /// 统一“占用 Luby 并支持按 Luby 执行 End”的活动接口。
    /// 用在 DesktopPetServices.EndAllLubyActivities()，避免 Services 直接依赖具体系统类。
    /// </summary>
    public interface ILubyActivity
    {
        bool IsLubyBusy(LubyInstanceComponent luby);
        void EndAllForLuby(LubyInstanceComponent luby);
    }
}

