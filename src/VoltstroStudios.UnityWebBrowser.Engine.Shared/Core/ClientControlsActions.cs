// UnityWebBrowser (UWB)
// Copyright (c) 2021-2022 Voltstro-Studios
// 
// This project is under the MIT license. See the LICENSE.md file for more details.

using System;
using VoltstroStudios.UnityWebBrowser.Shared.Core;
using VoltRpc.Communication;
using VoltstroStudios.UnityWebBrowser.Shared.Js;

#nullable enable
namespace VoltstroStudios.UnityWebBrowser.Engine.Shared.Core;

/// <summary>
///     This is a wrapper around <see cref="IClientControls" />. It checks if we are connected first before firing an event.
///     <para>The reason why we do the check here is that VoltRpc will throw an exception, rather then not do anything.</para>
/// </summary>
internal class ClientControlsActions : IClientControls, IDisposable
{
    private Client? client;
    private IClientControls? clientActions;

    //Ziva patch: VoltRpc's Client.IsConnected does not detect a dead peer, so the guards below
    //pass on a severed connection (e.g. the Unity editor reloaded its app domain) and the
    //proxy call throws. These events fire from inside native CEF callbacks, where an unhandled
    //exception FailFasts the entire engine (observed SIGABRT via OnVirtualKeyboardRequested ->
    //InputFocusChange). Events are fire-and-forget notifications: when the channel is gone,
    //dropping them is correct — killing the browser is not.
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
            Console.WriteLine("[UWB] client event channel lost; dropping further engine->client events");
        }
    }

    public void UrlChange(string url)
    {
        SafeInvoke(() => clientActions?.UrlChange(url));
    }

    public void LoadStart(string url)
    {
        SafeInvoke(() => clientActions?.LoadStart(url));
    }

    public void LoadFinish(string url)
    {
        SafeInvoke(() => clientActions?.LoadFinish(url));
    }

    public void TitleChange(string title)
    {
        SafeInvoke(() => clientActions?.TitleChange(title));
    }

    public void ProgressChange(double progress)
    {
        SafeInvoke(() => clientActions?.ProgressChange(progress));
    }

    public void Fullscreen(bool fullScreen)
    {
        SafeInvoke(() => clientActions?.Fullscreen(fullScreen));
    }

    public void InputFocusChange(bool focused)
    {
        SafeInvoke(() => clientActions?.InputFocusChange(focused));
    }

    public void Ready()
    {
        SafeInvoke(() => clientActions?.Ready());
    }

    public void ExecuteJsMethod(ExecuteJsMethod executeJsMethod)
    {
        SafeInvoke(() => clientActions?.ExecuteJsMethod(executeJsMethod));
    }

    public void Dispose()
    {
        ReleaseResources();
        GC.SuppressFinalize(this);
    }

    internal void SetIpcClient(Client ipcClient)
    {
        client = ipcClient ?? throw new NullReferenceException();
        clientActions = new ClientControls(client);
        channelDead = false; //Ziva patch: a fresh client is a fresh channel
    }

    ~ClientControlsActions()
    {
        ReleaseResources();
    }

    private void ReleaseResources()
    {
        client?.Dispose();
    }
}