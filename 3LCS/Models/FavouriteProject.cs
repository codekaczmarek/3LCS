using System.Collections.Generic;

namespace ThreeLCS.Models
{
    public class FavouriteProject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ProjectTypeId { get; set; }

        /// <summary>True when the user explicitly starred this project. False-only projects are
        /// kept as long as they contain at least one favourite environment.</summary>
        public bool Favourite { get; set; }

        /// <summary>Accordion expand state — persisted so the UI survives restarts.</summary>
        public bool Expanded { get; set; } = true;

        public string? FriendlyName { get; set; }

        public List<FavouriteEnvironment> Environments { get; set; } = new();
    }
}
