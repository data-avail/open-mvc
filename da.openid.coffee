$(document).ready ->
	console.log "ready"
	item = null
	links = null
	linkPhrase = $("#oa_action span").text()
	$(".logon-button").click (e)->
		servName = $(@).attr "data-serv-name"
		servUrl = null
		if (servName == "openid")
			 servUrl = "/Account/OpenId?ServiceUrl=#{$("#oi_prefix").text() + $("#oi_input").val() + $("#oi_postfix").text()}&returnUrl=#{$("#return_url").val()}"	
		else 
			servUrl = $(@).attr "data-serv-url"

		OAuthPopUp servUrl, 600, 400
		
		e.preventDefault()
	$(".oa-button").click (e) ->
		e.preventDefault()
		if item then item.removeClass "oa-selected"
		if links then links.hide()
		item = $(@).addClass "oa-selected"
		oiUrl = item.attr "data-oi-url"
		if oiUrl
			r = " #{oiUrl} ".split "$"
			$("#oi_prefix").text $.trim(r[0])
			$("#oi_postfix").text $.trim(r[1])
			links = $ "#oi_action"
		else
			servName = item.attr "data-serv-name"
			links = $ "#oa_action, #oa_action [data-serv-name = #{servName}]"
			link = $ links[1]
			$("#oa_action span").text(linkPhrase.replace "$", link.attr("data-serv-caption"))

		links.show()

	$("#vk_button").click -> VK.Auth.login VKAuth

	window.setTimeout (-> 
		console.log "vkontakte most wrecked oauth service ever concieved"
		VK.init apiId: $("#vk_app_id").val() ), 1000
	
OAuthPopUp = (url, width, height) ->
	if navigator.userAgent.toLowerCase().indexOf("opera") != -1
		w = document.body.offsetWidth
		h = document.body.offsetHeight
	else
		w = screen.width
		h = screen.height
	t = Math.floor (h - height)/2-14
	l = Math.floor (w - width)/2-5
	url += "&extWindow=true"
	window.open url, "", "status=no,scrollbars=yes,resizable=yes,width=#{width},height=#{height},top=#{t},left=#{l}"

VKAuth = (response) ->
    if (response.session) 
        data = response.session
        data.ServiceName = "VKontakte"
        $.post "/Account/OAuth", data, (ret) ->
            if (ret.success)
                window.location = $("#return_url").val()

