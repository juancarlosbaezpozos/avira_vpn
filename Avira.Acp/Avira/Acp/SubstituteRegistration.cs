namespace Avira.Acp
{
    internal class SubstituteRegistration
    {
        public ResourceLocation ResourceLocation { get; private set; }

        public IResourceProvider ResourceProvider { get; private set; }

        public string RegistrationId { get; set; }

        public SubstituteRegistration(ResourceLocation resourceLocation, IResourceProvider resourceProvider)
        {
            ResourceLocation = resourceLocation;
            ResourceProvider = resourceProvider;
        }
    }
}