using System;

namespace DigitalPlague.Core
{
    /// <summary>An undirected, fictional connection between two location IDs.</summary>
    public readonly struct NetworkLink
    {
        public string From { get; }
        public string To { get; }

        public NetworkLink(string from, string to)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || from == to)
                throw new ArgumentException("A link needs two distinct location IDs.");
            From = from;
            To = to;
        }
    }
}
