using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Profile;
using System.Web.Security;

namespace DataAvail.Mvc.Account
{
    public class OAuthAccountProfile : ProfileBase
    {
        static public OAuthAccountProfile GetUserProfile(string MembershipUserName)
        {
            return (OAuthAccountProfile)(ProfileBase.Create(MembershipUserName));
        }

        static public void SetUserIdntifyer(string MembershipUserName, OAuthServiceIdentifyer ServiceIdentifyer)
        {
            var p = GetUserProfile(MembershipUserName);
            p.SetServiceIdentifyer(ServiceIdentifyer);
        }


        public Guid ServiceKey
        {
            get { return ((Guid)(base["ServiceKey"])); }
        }

        public OAuthServiceType ServiceType
        {
            get { return base["ServiceType"] == null ? OAuthServiceType.Local : (OAuthServiceType)base["ServiceType"]; }
        }


        private void SetServiceIdentifyer(OAuthServiceIdentifyer ServiceIdentifyer)
        {
            base["ServiceType"] = ServiceIdentifyer.ServiceType;
            base["ServiceKey"] = ServiceIdentifyer.ServiceKey;

            Save();
        }
    }
}