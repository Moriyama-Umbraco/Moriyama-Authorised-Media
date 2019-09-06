using Moriyama.AuthorisedMedia.Application;
using Umbraco.Core;
using Umbraco.Core.Services;

namespace Moriyama.AuthorisedMedia.Events
{
    public class AuthorisedMediaApplicationEventHandler : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            MediaService.Saving += AuthorisedMediaManager.Instance.MediaServiceSaved;
        }
    }
}