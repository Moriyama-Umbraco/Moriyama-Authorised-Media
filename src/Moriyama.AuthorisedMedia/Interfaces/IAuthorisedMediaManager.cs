using Moriyama.AuthorisedMedia.Models;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Moriyama.AuthorisedMedia.Interfaces
{
    public interface IAuthorisedMediaManager
    {
        UrlInformation GetUrlInformation(string path);

        int ParentId(int mediaFolderId);

        bool IsProtected(int mediaFolderId);

        void MediaServiceSaved(IMediaService sender, SaveEventArgs<IMedia> eventArgs);

    }
}
