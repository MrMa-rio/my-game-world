namespace MyGameWorld.Client.ApplicationFlow
{
    public readonly struct DeveloperSceneEntry
    {
        public DeveloperSceneEntry(SceneId sceneId, string title, string category, string description, bool enabled)
        {
            SceneId = sceneId;
            Title = title;
            Category = category;
            Description = description;
            Enabled = enabled;
        }

        public SceneId SceneId { get; }
        public string Title { get; }
        public string Category { get; }
        public string Description { get; }
        public bool Enabled { get; }
    }
}
