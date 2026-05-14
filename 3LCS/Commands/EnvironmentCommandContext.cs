using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeLCS.Services.Interfaces;
using ThreeLCS.ViewModels;

namespace ThreeLCS.Commands
{
    public sealed class EnvironmentCommandContext
    {
        public required IBusyHost BusyHost { get; init; }
        public required int SelectedTabIndex { get; init; }
        public required EnvironmentViewModel? ActiveCheRow { get; init; }
        public required EnvironmentViewModel? SelectedCheRow { get; init; }
        public required EnvironmentViewModel? SelectedSaasRow { get; init; }
        public required IReadOnlyList<EnvironmentViewModel> SelectedCheRows { get; init; }
        public required IReadOnlyList<EnvironmentViewModel> SelectedSaasRows { get; init; }
        public required Func<EnvironmentViewModel?, IDisposable?> BeginProjectScope { get; init; }
        public required Func<Task> RefreshAsync { get; init; }
    }
}
