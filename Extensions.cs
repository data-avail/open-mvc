using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;

namespace DataAvail.Mvc.Account
{
    internal static class Extensions
    {
        internal static string AsJson(this object obj)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();
            
            return js.Serialize(obj);
        }

        internal static T FromJson<T>(this string str)
        {
            JavaScriptSerializer js = new JavaScriptSerializer();

            return js.Deserialize<T>(str);
        }

        internal static string GetAbsoluteUrl(this Controller Controller, string VirtualPath)
        {
            return string.Format("{0}://{1}/{2}", Controller.HttpContext.Request.Url.Scheme, Controller.HttpContext.Request.Url.Authority, VirtualPath ?? VirtualPath.Trim('/'));        
        }

        public static string GetMD5Hash(this string input)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            System.Text.StringBuilder s = new System.Text.StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            string password = s.ToString();
            return password;
        }

        public static DateTime ConvertFromUnixTimestamp(this string timestamp)
        {
            
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(long.Parse(timestamp));
        }

        public static DateTime ConvertFromUnixTimestamp(this double timestamp)
        {
            //http://codeclimber.net.nz/archive/2007/07/10/convert-a-unix-timestamp-to-a-.net-datetime.aspx
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }


        public static double ConvertToUnixTimestamp(this DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return Math.Floor(diff.TotalSeconds);
        }


        internal static Guid ToGuid(this string value)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5 md5Hasher = MD5.Create();
            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(value));

            return new Guid(data);
        }

        internal static int SetAuthCookie(this HttpResponse response, string name, bool rememberMe, string UserData)
        {
            //http://www.danharman.net/2011/07/07/storing-custom-data-in-forms-authentication-tickets/
            var cookie = FormsAuthentication.GetAuthCookie(name, rememberMe);
            var ticket = FormsAuthentication.Decrypt(cookie.Value);
            var newTicket = new FormsAuthenticationTicket(ticket.Version, ticket.Name,
                ticket.IssueDate, ticket.Expiration, ticket.IsPersistent,
                UserData, ticket.CookiePath);

            var encTicket = FormsAuthentication.Encrypt(newTicket);

            // Create the cookie.
            response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName,
                encTicket));

            return encTicket.Length;
        }

        internal static FormsAuthenticationTicket GetFormsAuthenticationTicket(this HttpRequest request)
        {
            if (request.IsAuthenticated)
            {
                var cookie = request.Cookies[FormsAuthentication.FormsCookieName];

                if (null == cookie)
                    return null;

                return FormsAuthentication.Decrypt(cookie.Value);

            }
            else
            {
                return null;
            }        
        }

        internal static string GetAuthCookieUserData(this HttpRequest request)
        {
            var ticket = request.GetFormsAuthenticationTicket();

            return ticket != null ? ticket.UserData : null;
        }

        internal static void SetUserIdentifyer(this HttpResponse response, OAuthUserIdentifyer UserIdentifyer)
        {
            HttpContext.Current.Response.SetAuthCookie(UserIdentifyer.UserName, true, UserIdentifyer.ServiceIdentifyer.AsJson());
        }

        internal static OAuthUserIdentifyer GetUserIdentifyer(this HttpRequest request)
        {
            var ticket = request.GetFormsAuthenticationTicket();

            if (ticket != null)
            {
                var userIdent = new OAuthUserIdentifyer() { UserName = ticket.Name };

                if (!string.IsNullOrEmpty(ticket.UserData))
                {
                    try
                    {
                        userIdent.ServiceIdentifyer = ticket.UserData.Split('|')[0].FromJson<OAuthServiceIdentifyer>();
                    }
                    catch (Exception e){
                        throw new Exception("Looks like service identity data in user authetication cookie in the wrong format", e);
                    }
                }

                return userIdent;
            }
            else
            {
                return null;
            }
        }

        public static string GetAttemptedValue(this IValueProvider ValueProvider, string Name)
        {
            var r = ValueProvider.GetValue(Name);

            return r != null ? r.AttemptedValue : null;
        }

        public static T? GetAttemptedValue<T>(this IValueProvider ValueProvider, string Name) where T : struct, IComparable
        {
            var s = ValueProvider.GetAttemptedValue(Name);

            return !string.IsNullOrEmpty(s) ? (T?)System.ComponentModel.TypeDescriptor.GetConverter(typeof(Type)).ConvertFrom(s) : null;
        }
    }
}