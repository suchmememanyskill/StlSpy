using System;
using Avalonia.Controls;
using StlSpy.Service;
using StlSpy.Views;

namespace StlSpy.Utils;

public static class Buttons
{
    public static Button DownloadButton(PostView postView, LocalStorage storage, Action<PostView>? onRefresh = null)
    {
        return new()
        {
            Content = storage.AreFilesCached(postView.Post.UniversalId) ? "Delete Model" : "Download Model",
            Command = new LambdaCommand(_ => HandleDownloadButton(postView, storage, onRefresh))
        };
    }

    private static async void HandleDownloadButton(PostView postView, LocalStorage storage, Action<PostView>? onRefresh)
    {
        postView.SetCustomisableButtonsStatus(false);
        
        if (storage.AreFilesCached(postView.Post.UniversalId))
            storage.DeleteLocalPost(postView.Post.UniversalId);
        else
            await storage.GetFilesPath(postView.Post);

        onRefresh?.Invoke(postView);
    }
}