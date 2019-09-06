using System;
using System.Web;
using Moriyama.AuthorisedMedia.Models;
using Umbraco.Core;

namespace Moriyama.AuthorisedMedia.Application
{
    public class AuthorisedMediaHttpModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += ContextBeginRequest;
        }

        private void ContextBeginRequest(object sender, EventArgs e)
        {
            HttpContext context = ((HttpApplication)sender).Context;
            string url = context.Request.Path;
            
            // Determine whether URL is a media URL
            UrlInformation urlInformation = AuthorisedMediaManager.Instance.GetUrlInformation(url);

            if (!urlInformation.IsMedia)
            {
                return;
            }

            bool isProtected = AuthorisedMediaManager.Instance.IsProtected(urlInformation.MediaFolderId);

            if (!isProtected)
            {
                return;
            }

            // TODO : Maintain URL
            context.Response.Redirect("/umbraco/surface/authorisedmediasurface/index?url=" + HttpUtility.UrlEncode(url));
        }

        public void Dispose()
        {
            
        }
    }
}