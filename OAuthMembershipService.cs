using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DataAvail.Mvc.Account
{
    public interface IOAuthMembershipService
    {
        bool IsRegistered(OAuthServiceLogOnIdentifyer ServiceIdentifyer);

        MembershipCreateStatus Register(string UserName, OAuthServiceLogOnIdentifyer ServiceIdentifyer);

        void LogOn(string UserName, OAuthServiceLogOnIdentifyer ServiceIdentifyer);
    }

    public struct OAuthServiceLogOnIdentifyer
    {
        public OAuthServiceLogOnIdentifyer(OAuthServiceType ServiceType, string ServiceKey)
        {
            this.ServiceType = ServiceType;

            this.ServiceKey = ServiceKey;

        }

        public OAuthServiceType ServiceType;
            
        public string ServiceKey;

        public override string ToString()
        {
            return string.Format("{0}:{1}", ServiceType, ServiceKey);
        }

        public Guid ToGuid()
        {
            return this.ToString().ToGuid();
        }
    }

    public class OAuthUserIdentifyer
    {
        private static Dictionary<string, OAuthServiceType> WellKnownOpenIdProviderIdentifyers = new Dictionary<string, OAuthServiceType>{ 
                    {"{0}.livejournal.com", OAuthServiceType.LiveJournal},
                    {"{0}@yandex.ru",OAuthServiceType.Yandex},
                    {"{0}@mail.ru",OAuthServiceType.MailRu},
                    {"{0}@rambler.ru", OAuthServiceType.Rambler},
                    {"http://www.liveinternet.ru/users/{0}/", OAuthServiceType.LiveInternet},
                    {"{0}.blogspot.com", OAuthServiceType.Blogger}
        };


        public string UserName { get; set; }

        public OAuthServiceIdentifyer ServiceIdentifyer { get; set; }

        public static OAuthUserIdentifyer CurrentsUserIdentifyer
        {
            get
            {
                return HttpContext.Current.Request.GetUserIdentifyer();
            }
        }

        public override string ToString()
        {
            var userIdentifyer = OAuthUserIdentifyer.CurrentsUserIdentifyer;

            if (userIdentifyer != null)
            {
                if (userIdentifyer.ServiceIdentifyer == null)
                {
                    return UserName;
                }
                else
                {
                    return string.Format("{0}:{1}:{2}", userIdentifyer.ServiceIdentifyer.ServiceType, DisplayUserName, userIdentifyer.ServiceIdentifyer.ServiceKey);
                }
            }

            return null;
        }

        public string DisplayUserName
        {
            get
            {
                if (ServiceIdentifyer != null)
                {
                    return GetDisplayName(UserName);
                }
                else
                {
                    return null;
                }
            }
        }

        public static string GetDisplayName(string UserName)
        {
            foreach (var prrIdent in WellKnownOpenIdProviderIdentifyers.Keys)
            {
                var match = Regex.Match(UserName, string.Format(prrIdent, @"(\w+)"));

                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return UserName;
        }
         
        public OAuthUserDisplay Display
        {
            get
            {
                if (ServiceIdentifyer != null)
                {
                    if (ServiceIdentifyer.ServiceType == OAuthServiceType.OpenId)
                    {
                        return GetUserDisplayByOpenId(UserName);
                    }
                    else
                    {
                        var userDisplay = GetUserDisplayByOpenId(UserName);

                        if (userDisplay.ServiceType != OAuthServiceType.OpenId) return userDisplay;

                        //Which case it's not OpenId but OAuth, because openID should be filtered in preceeded condition

                        return new OAuthUserDisplay(UserName, ServiceIdentifyer.ServiceType);
                    }
                }
                else
                {
                    return new OAuthUserDisplay(UserName, OAuthServiceType.Local);
                }
            }
        }

        public static OAuthServiceType GetOpenIdServiceType(string OpenIdIdentifyer)
        {
            foreach (var prrIdent in WellKnownOpenIdProviderIdentifyers)
            {
                var match = Regex.Match(OpenIdIdentifyer, string.Format(prrIdent.Key, @"(\w+)"));

                if (match.Success)
                {
                    return prrIdent.Value;
                }
            }

            return OAuthServiceType.OpenId;
        
        }

        /// <summary>
        /// Get user's display data by ones full name
        /// </summary>
        /// <param name="FullName">String returned from ToString method of UserIdentifyer</param>
        /// <returns></returns>
        public static OAuthUserDisplay GetUserDisplayByFullName(string FullName)
        {
            var r = FullName.Split(':');

            if (r.Length == 1)
            {
                return new OAuthUserDisplay(FullName, OAuthServiceType.Local);
            }
            else
            {
                var serviceType = (OAuthServiceType)System.Enum.Parse(typeof(OAuthServiceType), r[0]);
                var userName = OAuthUserIdentifyer.GetDisplayName(r[1]);
                if (serviceType == OAuthServiceType.OpenId)
                {
                    serviceType = OAuthUserIdentifyer.GetOpenIdServiceType(r[1]);
                }

                return new OAuthUserDisplay(userName, serviceType);
            }

        }

        public static OAuthUserDisplay GetUserDisplayByOpenId(string OpenIdIdentifyer)
        {
            foreach (var prrIdent in WellKnownOpenIdProviderIdentifyers)
            {
                var match = Regex.Match(OpenIdIdentifyer, string.Format(prrIdent.Key, @"(\w+)"));

                if (match.Success)
                {
                    return new OAuthUserDisplay(match.Groups[1].Value, prrIdent.Value);
                }
            }

            return new OAuthUserDisplay(OpenIdIdentifyer, OAuthServiceType.OpenId);
        }

        public struct OAuthUserDisplay
        {
            public OAuthUserDisplay(string Name, OAuthServiceType ServiceType)
            {
                this.Name = Name;

                this.ServiceType = ServiceType;
            }

            public string Name;

            public OAuthServiceType ServiceType;
        }

     }
    
    public class OAuthServiceIdentifyer
    {
        public static implicit operator OAuthServiceIdentifyer(OAuthServiceLogOnIdentifyer ServiceLogOnIdentifyer)
        {
            return new OAuthServiceIdentifyer { ServiceType = ServiceLogOnIdentifyer.ServiceType, ServiceKey = ServiceLogOnIdentifyer.ToGuid() };
        }

        public OAuthServiceType ServiceType { get; set; }

        public Guid ServiceKey { get; set; }
    
    }

    public enum OAuthServiceType
    {
        Local,
        OpenId,
        Facebook,
        Twitter,
        VKontakte,
        Google,
        Blogger,
        Yandex,
        LiveJournal,
        MailRu,
        Rambler,
        LiveInternet
    }


    public class OAuthMembershipService : IOAuthMembershipService
    {
        public bool IsRegistered(OAuthServiceLogOnIdentifyer ServiceLogOnIdentifyer)
        {
            return GetUserByServiceKey(ServiceLogOnIdentifyer) != null;
        }

        public MembershipCreateStatus Register(string UserName, OAuthServiceLogOnIdentifyer ServiceLogOnIdentifyer)
        {
            MembershipCreateStatus createStatus;

            var guid = Guid.NewGuid();

            var userName = string.Format("<user-{0}>", guid);

            var serviceIdent = (OAuthServiceIdentifyer)ServiceLogOnIdentifyer;

            var user = Membership.CreateUser(userName, guid.ToString().ToGuid().ToString(), "defaultEmail@email.com", null, null, true, serviceIdent.ServiceKey, out createStatus);

            OAuthAccountProfile.SetUserIdntifyer(userName, serviceIdent);

            return createStatus;
        }

        public void LogOn(string UserName, OAuthServiceLogOnIdentifyer ServiceLogOnIdentifyer)
        {
            var servIdent = (OAuthServiceIdentifyer)ServiceLogOnIdentifyer;

            MembershipUser user = GetUserByServiceKey(servIdent.ServiceKey);

            if (user == null) throw new Exception(string.Format("User with ServiceKey = {0} not found", ServiceLogOnIdentifyer.ToString()));

            HttpContext.Current.Response.SetUserIdentifyer(new OAuthUserIdentifyer { UserName = UserName, ServiceIdentifyer = servIdent });
        }
        
        private MembershipUser GetUserByServiceKey(OAuthServiceLogOnIdentifyer ServiceIdentifyer)
        {
            return GetUserByServiceKey(ServiceIdentifyer.ToGuid());
        }

        private MembershipUser GetUserByServiceKey(Guid ServiceKey)
        {
            return Membership.GetUser(ServiceKey, true);
        }
    }

}