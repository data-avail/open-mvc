Introduction
------------
The library allow to login user through externl open id or oauth providers also sustains native registration/logon mechanism.
You must use library api to get current user identifyer in order to coherent work as for external users as for the locals.

The library is built upon DotNetOpenAuth library version 3.5 since this version only currently supports facebook authentication.
If you project already has installed another version of DotNetOpenAuth, please uininstall it or at leasr remove reference.


After install
-------------

Add oauth service keys to web.config files:

Set up keys of your application for oauth services. AppId and AppSecret pair usaually depends on application url, this way it is needed to
set up diffrent ones for diffrent application enviroments, usually developing, staging and production (becacause they have diffrent urls)
There is also brief notices how to make keys work for localhost (developing enviroment)

###[Twitter sign-up](https://twitter.com/oauth_clients): 

&lt;add key="twitterConsumerKey" value="XXX" /&gt;
&lt;add key="twitterConsumerSecret" value="XXX" /&gt;

notice for localhost:
	WebSite : any (not empty)
	Callback Url : any (not empty)

###[Facebook sign-up](http://developers.facebook.com/setup/): 
&lt;add key="facebookAppID" value="XXX" /&gt;
&lt;add key="facebookAppSecret" value="XXX" /&gt;

notice for localhost:
	Site -> Site Url : http://localhost:port_number/
					   port_number - port where your site running, it is important
	App domain : empty

###[VKontakte sign-up](http://vkontakte.ru/editapp?act=create&site=1): 
&lt;add key="vkontakteAppID" value="XXX" /&gt;
&lt;add key="vkontakteAppSecret" value="XXX" /&gt;

notice for localhost:
	noway becides modifying etc\hosts file.

Under the hoods
---------------

>abbriviations:
>ext-user - external user, user registered to your site through an open id or oauth provider.

When ext-user login to your site, the library is check whether he already registered and if not register it in your membership provider.
The new ext-user registered (in membership provider) with following attributes:
###Id 
MD5 hash of ServiceType:ServiceKey
where ServiceType - on of the prdefined service types "Twitter", "Facebook", "Google", "OpenId"
ServiceKey - id of user returned from oauth service or open id url of user.
When your application know service type and id of user in the service your always can find one in your membership provider.
###Name
&lt;user-GUID&gt;
where guid arbitrary generated guid. The user name in this case is not intended to display to user and must be used internally by library.
###Password
MD5 hash of Name GUID
###ServiceType 
(see Id.ServiceType) the parameter store to the profile provider with name ServiceType 
###UserName 
the display friendly name of the user in the external system, the parameter store to the profile provider with name UserName

###Security considerations 
Since by user name, password could be implyed, it is important not give it away from the application in the case of external users. Also even if some villan would know
the membership user name (GUID) he can't login through standard login system of your site, because membership name contents '<' - which prevent controller to invoke
logon action with this parameter, since angle brakets prohibited character.

###Identifyer of external user
Identifyer of existent ext-user consist of following fields:
1.Service Type
2.Display user name
3.Service Key
In common scenario these parameters are sufficient to : display type of service through which user was loginned, display friendly user name and by service key you will
be able to retrieve user from your membership provider (see Id parameter)

To retrive current user identifyer:

OAuthUserIdentifyer.CurrentUserIdentifyer

>notice:
If user was registered/logined as local user (registered through native app registration mechanism, ServiceType will be Local, user name - name of the user while registred,
ServiceKey will be null (generally by this property you can determine if this user local or external)

API
---


	
