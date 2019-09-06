using System;
using System.Runtime.Caching;
using Moriyama.AuthorisedMedia.Interfaces;
using Moriyama.AuthorisedMedia.Models;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;

namespace Moriyama.AuthorisedMedia.Application
{
    //create table AuthorisedMedia(
    //MediaPathId int not null,
    //MediaParentId int not null
    //)

    //CREATE UNIQUE INDEX AuthorisedMediaIdx
    //ON AuthorisedMedia(MediaPathId, MediaParentId);

    public class AuthorisedMediaManager : IAuthorisedMediaManager
    {
        private static readonly AuthorisedMediaManager instance = new AuthorisedMediaManager();
        private MemoryCache _unProtectedFolders; 

        static AuthorisedMediaManager()
        {
        }

        private AuthorisedMediaManager()
        {
            this._unProtectedFolders = new MemoryCache("unProtectedMedia");
        }

        public static AuthorisedMediaManager Instance
        {
            get
            {
                return instance;
            }
        }

        public UrlInformation GetUrlInformation(string path)
        {
            if (!path.StartsWith("/media/"))
            {
                return new UrlInformation {IsMedia = false};
            }

            string[] segments = path.Split('/');

            if (segments.Length > 2
                && segments[1] == "media"
                && int.TryParse(segments[2], out var mediaFolderId))
            {
                return new UrlInformation { IsMedia = true, MediaFolderId = mediaFolderId };
            }

            // Not a URL in the form of "/media/{folderId}/{filename}";
            return new UrlInformation { IsMedia = false };
        }

        public int ParentId(int mediaFolderId)
        {
            return ApplicationContext.Current.DatabaseContext.Database.ExecuteScalar<int>(@"select top 1 MediaParentId from AuthorisedMedia where MediaPathId = @0", mediaFolderId);
        }

        public bool IsProtected(int mediaFolderId)
        {
            if (this._unProtectedFolders.Contains(mediaFolderId.ToString()))
            {
                // Item has recently been accessed as unprotected - so avoid db hit.
                return false;
            }

            bool result = ApplicationContext.Current.DatabaseContext.Database.ExecuteScalar<bool>(@"IF EXISTS(SELECT 1
                FROM AuthorisedMedia
                    WHERE  MediaPathId = @0)
                SELECT 1
                ELSE
                 SELECT 0", mediaFolderId);

            if (!result)
            {
                // Store unprotected media result in cache for a minute to avoid lots of queries
                CacheItemPolicy cip = new CacheItemPolicy()
                {
                    AbsoluteExpiration = new DateTimeOffset(DateTime.Now.AddSeconds(60))
                };
                this._unProtectedFolders.AddOrGetExisting(mediaFolderId.ToString(), false, cip);
            }

            return result;
        }

        public void MediaServiceSaved(IMediaService sender, SaveEventArgs<IMedia> eventArgs)
        {
            
            foreach (IMedia media in eventArgs.SavedEntities)
            {
                string url = media.GetValue<string>("umbracoFile");
                if (string.IsNullOrEmpty(url))
                {
                    // Media item doesn't have an umbracoFileProperty
                    continue;
                }

                UrlInformation result = GetUrlInformation(url);
                if (!result.IsMedia)
                {
                    // Unable to extract media information 
                    continue;
                }

                // Media is protected if it is in a protected folder
                IMedia parent = media.Parent();
                bool isProtected = parent != null && parent.ContentType.Alias == "protectedFolder"
                                                  && parent.HasProperty("memberGroups");

                using (Transaction transaction = ApplicationContext.Current.DatabaseContext.Database.GetTransaction())
                {
                    // Clear any previous protection
                    ApplicationContext.Current.DatabaseContext.Database.Execute(@"delete from AuthorisedMedia where MediaPathId = @0", result.MediaFolderId);
                    if (isProtected)
                    {
                        // Apply database record indicating protection
                        ApplicationContext.Current.DatabaseContext.Database.Execute(
                            @"insert into AuthorisedMedia (MediaPathId, MediaParentId) values (@0, @1)", result.MediaFolderId, parent.Id);
                    }

                    transaction.Complete();
                }
            }
        }
    }
}