namespace ThreeLCS.Services.Interfaces
{
    /// <summary>
    /// Implemented by ViewModels that own an IsBusy/StatusText pair.
    /// Used by <see cref="IBackgroundJobRunner.RunAsCommandAsync"/> to manage busy state.
    /// </summary>
    public interface IBusyHost
    {
        bool IsBusy { set; }
        string StatusText { set; }
    }
}
