using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.ApplicationBlock;
using DotNetOpenAuth.OAuth2;
using System.Net;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
using DotNetOpenAuth.Messaging;
using System.Configuration;
using DotNetOpenAuth.OpenId.RelyingParty;
using DataAvail.Mvc.Account;

namespace DataAvail.Mvc.Account
{
    /// <summary>
    /// 1. Derive your current account controller with this one.
    ///     /// a. Override GetErrorCodeFromString() method.
    ///     /// b. In LogOn Action add base.BeforeLogOn() before return View.
    /// 2. Add da.openid.css to _Layout page
    /// 2. Add @Html.Partial("_LogOnOAuth") to the LogOn view.
    /// 3. Add @Html.Partial("_LogInOAuth") to the LogIn view.
    /// 4. Define Twitter and Facebook keys in the config file.
    /// 5. In the web.conig file add to the membership profile tag  - inherits="YOUR_NAMESPACE.AccountProfile" 
    /// After this the tag should look like this <profile defaultProvider="DefaultProfileProvider" inherits="YOUR_NAMESPACE.AccountProfile">
    /// 6. Add <link href="@Url.Content("~/Content/da.openid.css")" rel="stylesheet" type="text/css" /> to _Layout.html
    /// </summary>
    public abstract class OAuthAccountController : Controller
    {
        public OAuthAccountController()
        {
            OAuthMembershipService = new OAuthMembershipService();
        }

        #region OpenId and OAuth

        private IOAuthMembershipService OAuthMembershipService { get; set; }

        static private readonly OpenIdRelyingParty Openid = new OpenIdRelyingParty();

        protected virtual string OAuthGetErrorMessage(IAuthenticationResponse AuthenticationResponse)
        {
            return AuthenticationResponse.Exception != null ? AuthenticationResponse.Exception.Message : OAuthDefaultErrorMessage;
        }

        protected virtual string OAuthDefaultErrorMessage
        {
            get
            {
                return "Authorization error";
            }
        }


        private string OAuthPrepareReturnUrl()
        {
            string returnUrl = null;

            var val = ValueProvider.GetValue("returnUrl");

            if (val != null)
            {
                returnUrl = val.AttemptedValue;
            }

            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = "/Home/Index";
            }

            return returnUrl;
        }

        private static bool OAuthIsExtWindow(OAuthServiceType ServiceType)
        {
            return true;
        }

        private ActionResult OAuthGetExtWindowSuccessResult(string ReturnUrl)
        {
            if (string.IsNullOrEmpty(ReturnUrl))
            {
                ReturnUrl = "window.opener.location.href";
            }

            return Content(string.Format("<!HTML><header></header><dody><script>window.opener.location.href = '{0}'; window.close();</script></body>", ReturnUrl), "text/html");
        }

        private ActionResult GetSuccessResult(OAuthServiceType ServiceType, string ReturnUrl)
        {
            if (OAuthIsExtWindow(ServiceType))
            {
                return OAuthGetExtWindowSuccessResult(ReturnUrl);
            }
            else
            {
                return Redirect(ReturnUrl);
            }
        }

        private ActionResult OAuthGetErrorResult(string ErrorMessage = null)
        {
            if (string.IsNullOrEmpty(ErrorMessage))
                ErrorMessage = OAuthDefaultErrorMessage;

            ViewData.ModelState.AddModelError(string.Empty, ErrorMessage);

            return View("OAuthError");
        }

        //[AllowAnonymous] (MVC 4 only)
        public virtual ActionResult OpenId(string ServiceUrl)
        {
            string returnUrl = OAuthPrepareReturnUrl();

            OAuthServiceType servType = OAuthServiceType.OpenId;

            if (!string.IsNullOrEmpty(ServiceUrl))
            {
                switch (ServiceUrl.ToLower())
                { 
                    case "google":
                        ServiceUrl = "https://www.google.com/accounts/o8/id";
                        servType = OAuthServiceType.Google;
                        break;
                    default:
                        servType = OAuthUserIdentifyer.GetOpenIdServiceType(ServiceUrl);
                        break;
                }
            }

            var response = Openid.GetResponse();
            if (response == null)
            {
                // User submitting Identifier
                Identifier id;
                if (Identifier.TryParse(ServiceUrl, out id))
                {
                    try
                    {
                        var request = Openid.CreateRequest(id);
                        
                        var fetch = new FetchRequest();
                        fetch.Attributes.AddRequired(WellKnownAttributes.Name.First);
                        fetch.Attributes.AddRequired(WellKnownAttributes.Name.Last);
                        request.AddExtension(fetch);
                        
                        return request.RedirectingResponse.AsActionResult();
                    }
                    catch (ProtocolException ex)
                    {
                        return OAuthGetErrorResult(ex.Message); 
                    }
                }
                return OAuthGetErrorResult(); 
            }

            // OpenID Provider sending assertion response
            switch (response.Status)
            {
                case AuthenticationStatus.Authenticated:
                    var fetch = response.GetExtension<FetchResponse>();
                    string firstName = null;
                    string lastName = null;
                    if (fetch != null)
                    {
                        firstName = fetch.GetAttributeValue(WellKnownAttributes.Name.First);
                        lastName = fetch.GetAttributeValue(WellKnownAttributes.Name.Last);
                    }
                    var userName = response.FriendlyIdentifierForDisplay;
                    if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                    {

                        if (firstName != lastName)
                            userName = firstName + " " + lastName;
                        else if (!string.IsNullOrEmpty(firstName))
                            userName = firstName;
                        else
                            userName = lastName;
                    }
                    OAuthRegisterOrLoginExternalUser(userName, new OAuthServiceLogOnIdentifyer(servType, response.ClaimedIdentifier));
                    return GetSuccessResult(servType, returnUrl);
                case AuthenticationStatus.Canceled:
                case AuthenticationStatus.Failed:
                    return OAuthGetErrorResult(response.Exception != null ? response.Exception.Message : null);
            }
            return OAuthGetErrorResult();
        }


        private static readonly FacebookClient FacebookClient = new FacebookClient
        {
            ClientIdentifier = ConfigurationManager.AppSettings["facebookAppID"],
            ClientSecret = ConfigurationManager.AppSettings["facebookAppSecret"]
        };

        private static readonly WebServerClient VKontakteClient = new WebServerClient(
            new AuthorizationServerDescription(),
            ConfigurationManager.AppSettings["vkontakteAppID"],
            ConfigurationManager.AppSettings["vkontakteAppSecret"]);

        //[AllowAnonymous] (MVC 4 only)
        public virtual ActionResult OAuth(string serviceName)
        {
            
            string returnUrl = OAuthPrepareReturnUrl();

            if (serviceName == "Twitter")
            {
                string screenName;
                int userId;
                if (TwitterConsumer.TryFinishSignInWithTwitter(out screenName, out userId))
                {
                    var err = OAuthRegisterOrLoginExternalUser(screenName, new OAuthServiceLogOnIdentifyer(OAuthServiceType.Twitter, userId.ToString()));

                    if (err == null)
                    {
                        return GetSuccessResult(OAuthServiceType.Twitter, returnUrl);
                    }

                }
                else
                {
                    TwitterConsumer.StartSignInWithTwitter(false).Send();
                }
            }
            else if (serviceName == "Facebook")
            {

                IAuthorizationState authorization = FacebookClient.ProcessUserAuthorization();
                if (authorization == null)
                {
                    // Kick off authorization request                   
                    // This not work for MVC! Request should be snd only from html page!
                    //FacebookClient.RequestUserAuthorization();
                    //Just error

                    return OAuthGetErrorResult();
                }
                else
                {
                    var request = WebRequest.Create("https://graph.facebook.com/me?access_token=" + Uri.EscapeDataString(authorization.AccessToken));
                    using (var response = request.GetResponse())
                    {
                        using (var responseStream = response.GetResponseStream())
                        {
                            var graph = DotNetOpenAuth.ApplicationBlock.Facebook.FacebookGraph.Deserialize(responseStream);

                            var err = OAuthRegisterOrLoginExternalUser(graph.Name, new OAuthServiceLogOnIdentifyer(OAuthServiceType.Facebook, graph.Id.ToString()));

                            if (err == null)
                            {
                                return GetSuccessResult(OAuthServiceType.Facebook, returnUrl);
                            }

                        }
                    }
                }
            }
            else if (serviceName == "VKontakte")
            {
                var sig = ValueProvider.GetAttemptedValue("sig");

                //It is no sence to check all neccessar values, just check almost first and almost last
                var userName = ValueProvider.GetAttemptedValue("user[first_name]") + " " + ValueProvider.GetAttemptedValue("user[last_name]");

                if (!string.IsNullOrEmpty(sig) && !string.IsNullOrEmpty(userName))
                {
                    var expire = ValueProvider.GetAttemptedValue("expire");
                    var mid = ValueProvider.GetAttemptedValue("mid");

                    var r = string.Format("expire={0}mid={1}secret={2}sid={3}{4}", expire,
                        mid, ValueProvider.GetAttemptedValue("secret"), ValueProvider.GetAttemptedValue("id"), VKontakteClient.ClientSecret);

                    if (r.GetMD5Hash() == sig && expire.ConvertFromUnixTimestamp() > System.DateTime.Now)
                    {
                        var err = OAuthRegisterOrLoginExternalUser(userName, new OAuthServiceLogOnIdentifyer(OAuthServiceType.VKontakte, mid));

                        if (err == null)
                        {
                            return Json(new { success = true });
                        }
                    }
                }

                return Json(new { error = "something wrong with vkontakt authorization" });
            }

            return OAuthGetErrorResult();
        }

        private static string OAuthGetFacebookLogOnUrl(string UrlAuthority, string ReturnUrl)
        {
            return string.Format("http://www.facebook.com/dialog/oauth/?scope=user_about_me&client_id={0}&redirect_uri={1}",
                Uri.EscapeDataString(FacebookClient.ClientIdentifier),
                Uri.EscapeDataString(string.Format("http://{0}/Account/OAuth?serviceName=Facebook&returnUrl={1}", UrlAuthority, ReturnUrl)));
        }

        #endregion

        //ServiceId - OpenId or some unique identifier from OAuth service
        private string OAuthRegisterOrLoginExternalUser(string UserName, OAuthServiceLogOnIdentifyer ServiceIdentifyer)
        {
            if (!OAuthMembershipService.IsRegistered(ServiceIdentifyer))
            {
                var err = OAuthMembershipService.Register(UserName, ServiceIdentifyer);

                if (err != MembershipCreateStatus.Success) return GetErrorCodeFromString(err);
            }

            OAuthMembershipService.LogOn(UserName, ServiceIdentifyer);

            return null;
        }


        /// <summary>
        /// Invoke this method in LogOn action before return LogOn View!
        /// There is initialized important data for _LogOnOAuth view.
        /// </summary>
        protected void OAuthBeforeLogOn()
        {
            var returnUrl = OAuthPrepareReturnUrl();

            ViewBag.ReturnUrl = returnUrl;

            ViewBag.FacebookOAuthUrl = string.Format("http://www.facebook.com/dialog/oauth/?scope=user_about_me&client_id={0}&redirect_uri={1}",
                Uri.EscapeDataString(FacebookClient.ClientIdentifier),
                Uri.EscapeDataString(string.Format("http://{0}/Account/OAuth?serviceName=Facebook&returnUrl={1}", Request.Url.Authority, returnUrl)));

            ViewBag.VKontakteAppId = VKontakteClient.ClientIdentifier;
        }

        protected virtual string GetErrorCodeFromString(MembershipCreateStatus createStatus) { return createStatus.ToString(); }
    }
}
