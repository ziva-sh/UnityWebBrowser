// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VoltstroStudios.UnityWebBrowser.Shared.Popups;
using VoltRpc.Communication;

#nullable enable
namespace VoltstroStudios.UnityWebBrowser.Engine.Shared.Popups;

/// <summary>
///     Manager for popups on the engine side
/// </summary>
public class EnginePopupManager : IPopupClientControls
{
    private Client? client;
    private IPopupEngineControls? engineControls;

    private readonly List<EnginePopupInfo> popups;

    /// <summary>
    ///     Creates a new <see cref="EnginePopupManager"/> instance
    /// </summary>
    public EnginePopupManager()
    {
        popups = new List<EnginePopupInfo>();
    }

    /// <summary>
    ///     Call when you have a popup
    /// </summary>
    /// <param name="enginePopupInfo"></param>
    public void OnPopup(EnginePopupInfo enginePopupInfo)
    {
        popups.Add(enginePopupInfo);
        SafeInvoke(() => engineControls?.OnPopup(enginePopupInfo.PopupGuid));
    }

    /// <summary>
    ///     Call when you want to destroy a popup
    /// </summary>
    /// <param name="enginePopupInfo"></param>
    public void OnPopupDestroy(EnginePopupInfo enginePopupInfo)
    {
        popups.Remove(enginePopupInfo);
        SafeInvoke(() => engineControls?.OnPopupDestroy(enginePopupInfo.PopupGuid));
    }

    //Ziva patch: same barrier as ClientControlsActions — IsConnected cannot detect a dead
    //peer, and a throw out of a CEF callback FailFasts the engine. See that class for details.
    private bool channelDead;

    private void SafeInvoke(Action action)
    {
        if (channelDead || client is not { IsConnected: true })
            return;
        try
        {
            action();
        }
        catch (Exception)
        {
            channelDead = true;
            Console.WriteLine("[UWB] popup event channel lost; dropping further popup events");
        }
    }
    
    /// <inheritdoc />
    public void PopupClose(Guid guid)
    {
        EnginePopupInfo popupInfo = popups.First(x => x.PopupGuid == guid);
        popups.Remove(popupInfo);
        _ = Task.Run(popupInfo.Dispose);
    }

    /// <inheritdoc />
    public void PopupExecuteJs(Guid guid, string js)
    {
        EnginePopupInfo popupInfo = popups.First(x => x.PopupGuid == guid);
        popupInfo.ExecuteJs(js);
    }
    
    /// <summary>
    ///     Setup everything for IPC
    /// </summary>
    /// <param name="ipcClient"></param>
    internal void SetIpcClient(Client ipcClient)
    {
        client = ipcClient;
        engineControls = new PopupEngineControls(client);
        channelDead = false; //Ziva patch: a fresh client is a fresh channel
    }
}