using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace ThreeLCS.Models
{
    public partial class ApiMonitorEntry : ObservableObject
    {
        public Guid Id { get; init; }
        public DateTime StartedAt { get; init; } = DateTime.Now;
        public string Method { get; init; } = "";
        public string Url { get; init; } = "";

        [ObservableProperty] private string _status = "⏳";
        [ObservableProperty] private long? _durationMs;

        public string ShortUrl => Uri.TryCreate(Url, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath
            : (Url.Length > 60 ? Url[..60] : Url);
    }
}
