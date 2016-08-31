﻿Imports Newtonsoft.Json
Imports System.Net
Imports Newtonsoft.Json.Linq

Public Class ServerSideManager
    Public Shared requestURL As String = "http://www.dfdfx.club"
    Public Shared launcherUpdateURL As String = "http://www.kongkongmao.club/mc/launcher"
    Public Shared guid As String = ""
    Public Shared curMdpVer As String = "none"
    Public Shared fetchingInfoNotif As New Toast(I18n.translate("info.wait"),
                                                 I18n.translate("info.server.fetchingInfo"), Nothing, True, Toast.ToastLength.Short)
    Public Shared needUpdateNotif As New Toast(I18n.translate("info.server.updateAvail"),
                                               I18n.translate("info.server.updateAvailDesc"), Nothing, True, Toast.ToastLength.Short)
    Public Shared updateErrNotif As New Toast(I18n.translate("err"),
                                              I18n.translate("err.network"), Nothing, True, Toast.ToastLength.Long) With {.backColor = Color.Red}
    Public Const appID As String = "2FCE29CF-6FC7-4975-B93C-860C738A6496"
    Public Shared cachedMods As New List(Of MCMod)
    Private Const threadName As String = "serverSideManager"

    Shared Sub New()
        Threads.createThread(threadName)
    End Sub

    Public Shared Sub fetchFromServer()
        Threads.addScheduledTask(threadName, "selfUpdateCheck", AddressOf SelfUpdate.checkUpdate)
        Threads.addScheduledTask(threadName, "fetch", AddressOf _fetchFromServer)
    End Sub

    Private Shared Sub _fetchFromServer()
        Dim json As String = Post(requestURL & "/api/mc/getVersion", "", "", "GET")
        If json = "ERR" Then ToastRenderer.addToast(updateErrNotif) : Return
        Dim obj As ServerJSONs.MainJSON = JsonConvert.DeserializeObject(Of ServerJSONs.MainJSON)(json)
        If obj.data.version <> curMdpVer Then
            ToastRenderer.addToast(needUpdateNotif)
            _resolveModsJSON(obj, requestURL)
        End If
    End Sub

    Private Shared Sub _resolveModsJSON(obj As ServerJSONs.MainJSON, alt As String)
        Dim json As String = Post(If(obj.data.cdn, requestURL) & $"/api/mc/{obj.data.version}/mods.json", "", "", "GET")
        If json = "ERR" Then ToastRenderer.addToast(updateErrNotif) : Return
        Dim mods As ServerJSONs.ModsJSON = JsonConvert.DeserializeObject(Of ServerJSONs.ModsJSON)(json)

    End Sub

    Public Shared Sub updateConfig()
        ConfigManager.ConfigValue.serverUrl = requestURL
        ConfigManager.ConfigValue.curMdpVer = curMdpVer
        ConfigManager.ConfigValue.guid = guid
        ConfigManager.ConfigValue.cachedMods = JsonConvert.SerializeObject(cachedMods)
    End Sub

    Public Shared Sub readConfig()
        requestURL = ConfigManager.ConfigValue.serverUrl
        curMdpVer = ConfigManager.ConfigValue.curMdpVer
        guid = ConfigManager.ConfigValue.guid
        If guid = "" Then
            guid = System.Guid.NewGuid.ToString
        End If
    End Sub

    Public Shared Function Post(url As String, payload As String, contentType As String, method As String) As String
        Dim req As HttpWebRequest = CType(HttpWebRequest.Create(url), HttpWebRequest)
        If method = "GET" Then req.Accept = "*/*"
        If method <> "GET" Then
            Dim reqobj As JObject = JObject.Parse(payload)
            reqobj.Add("appid", appID)
            reqobj.Add("ClientId", guid)
            req.ContentType = contentType
            req.ContentLength = reqobj.ToString.Length
            req.GetRequestStream.Write(Text.Encoding.ASCII.GetBytes(reqobj.ToString), 0, reqobj.ToString.Length)
        End If
        req.Method = method
        Try
            Dim res As HttpWebResponse = CType(req.GetResponse, HttpWebResponse)
            Dim ret As String = (New IO.StreamReader(res.GetResponseStream)).ReadToEnd
            res.Close()
            Console.WriteLine(ret)
            Return ret
        Catch e As WebException
            Logger.log(e.Message, Logger.LogLevel.ERR)
            Logger.log(New IO.StreamReader(e.Response.GetResponseStream).ReadToEnd, Logger.LogLevel.ERR)
            Return "ERR"
        End Try
    End Function

    Public Class ServerJSONs
        Public Class DataHolderJSON
            Public connecturl As String
        End Class

        Public Class MainJSON
            Public Class VersionJSON
                Public version As String
                Public mcVer As String
                Public forgeVer As String
                Public cdn As String
            End Class
            Public data As VersionJSON
        End Class

        <Obsolete> Public Class OldMainJSON
            Public result As String
            Public code As Integer
            Public Class ServerPagesJSON
                Public news_page As String
                Public mods As String
                Public customlogin As Boolean
                Public customloginapi As String
            End Class
            Public data As ServerPagesJSON
        End Class

        Public Class ModsJSON
            Public Class ModHolder
                Public modid As String
                Public display As String
                Public filename As String
                Public md5 As String
                Public Enum ModType
                    Core
                    Optimization
                    [Optional]
                End Enum
                Public type As String
                Public Function getModType() As ModType
                    Return If(type.ToLower = "core", ModType.Core, If(type.ToLower = "optimization", ModType.Optimization, ModType.Optional))
                End Function
            End Class
            Public mods As List(Of ModHolder)
        End Class

        <Obsolete> Public Class CustomLoginJSON
            Public method As String
            Public url As String
            Public Class Argument
                Public param As String
                Public value As String
                Public mode As String
            End Class
            Public args As List(Of Argument)
            Public Class Condition
                Public Class IfCondition
                    Public root As String
                    Public func As String
                    Public param As String
                    <JsonProperty("operator")> Public op As String
                    Public value As String
                End Class
                <JsonProperty("if")> Public ifcond As IfCondition
                Public Class ThenCondition
                    Public func As String
                End Class
                <JsonProperty("then")> Public thencond As ThenCondition
            End Class
            <JsonProperty("return")> Public condlist As List(Of Condition)
        End Class
    End Class

    Public Class MCMod
        Public modid As String
        Public display As String
        Public type As ServerJSONs.ModsJSON.ModHolder.ModType
    End Class

    Public Shared Sub downloadModsFromProvider(ver As String, dir As String)

    End Sub
End Class
