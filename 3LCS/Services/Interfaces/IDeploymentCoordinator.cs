namespace ThreeLCS.Services.Interfaces
{
    /// <summary>
    /// Provides RDP launch scenarios with the minimum surface needed to start and monitor
    /// a cloud-hosted environment without taking a direct dependency on <see cref="ViewModels.MainViewModel"/>.
    /// </summary>
    public interface IDeploymentCoordinator
    {
        /// <summary>
        /// Sends a start request for the given environment as a named background task and
        /// immediately forces the deployment polling loop to begin.
        /// </summary>
        void StartCheEnv(ViewModels.EnvironmentViewModel env);

        /// <summary>
        /// Starts deployment polling if no polling loop is currently active.
        /// Safe to call even when polling is already running — in that case it is a no-op.
        /// </summary>
        void EnsureDeploymentPolling();
    }
}
