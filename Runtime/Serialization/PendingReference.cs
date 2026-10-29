namespace Kott.Configs
{
    /// <summary>A template reference read from a payload, resolved once all layers are merged.</summary>
    internal readonly struct PendingReference
    {
        public PendingReference(Template owner, ITemplateReference reference, string targetId)
        {
            Owner = owner;
            Reference = reference;
            TargetId = targetId;
        }

        public Template Owner { get; }
        public ITemplateReference Reference { get; }
        public string TargetId { get; }
    }
}
