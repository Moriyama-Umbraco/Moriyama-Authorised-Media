using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Moriyama.AuthorisedMedia.Application;
using Moriyama.AuthorisedMedia.Models;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Security;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedCache;

namespace Moriyama.AuthorisedMedia.Controllers
{
    public class AuthorisedMediaSurfaceController : SurfaceController
    {
        
        private bool IsUserLoggedIn(FormsAuthenticationTicket authenticationTicket)
        {
            return ApplicationContext.Services.UserService.GetByUsername(authenticationTicket.Name) != null;
        }

        private FileStreamResult GetFileStreamResult(string url)
        {
            IFileSystem fs = FileSystemProviderManager.Current.GetUnderlyingFileSystemProvider("media");

            if (!fs.FileExists(url))
            {
                throw new FileNotFoundException(url);
            }

            string fileName = fs.GetFileName(url);
            string mimeType = MimeMapping.GetMimeMapping(fs.GetFileName(url));
            FileStreamResult result = new FileStreamResult(fs.OpenFile(url), mimeType);

            if (mimeType.StartsWith("application/"))
            {
                result.FileDownloadName = fileName;
            }
            
            return result;
        }

        public ActionResult Index(string url)
        {
            UrlInformation result = AuthorisedMediaManager.Instance.GetUrlInformation(url);

            if (!result.IsMedia)
            {
                // Not a valid media URL - so can't be returned via this controller
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            int parentId = AuthorisedMediaManager.Instance.ParentId(result.MediaFolderId);

            if (parentId < 1)
            {
                // No record of a protected parent folder, so return the file
                return GetFileStreamResult(url);
            }

            IPublishedContent parent = Umbraco.TypedMedia(parentId);

            if (parent == null)
            {
                // No parent media item, so it can't be protected
                return GetFileStreamResult(url);
            }

            if (parent.ContentType.Alias != "protectedFolder" || !parent.HasProperty("memberGroups") || !parent.HasValue("memberGroups"))
            {
                // Not a protected folder - with a value for membergroups
                return GetFileStreamResult(url);
            }

            FormsAuthenticationTicket authenticationTicket = new HttpContextWrapper(System.Web.HttpContext.Current).GetUmbracoAuthTicket();
            bool memberLoggedIn = Members.IsLoggedIn();
            bool hasUserAuthTicket = authenticationTicket != null;

            if (hasUserAuthTicket && IsUserLoggedIn(authenticationTicket))
            {
                // All users can access media
                return GetFileStreamResult(url);
            }

            if (memberLoggedIn)
            {
                MemberPublishedContent member = (MemberPublishedContent)Members.GetCurrentMember();

                if (member == null)
                {
                    // Can't get the current member - so return a 404
                    return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                }
                
                string[] allowedMemberGroups = parent.GetPropertyValue<string>("memberGroups").Split(',');
                string[] memberRoles = Roles.GetRolesForUser(member.UserName);

                foreach (string role in memberRoles)
                {
                    if (allowedMemberGroups.Contains(role))
                    {
                        // Member is in an appropriate group. allow access
                        return GetFileStreamResult(url);
                    }
                }
            }
            
            // No Member logged in - so return a 404
            return new HttpStatusCodeResult(HttpStatusCode.NotFound);
        }
    }
}