#if UNITY_EDITOR || UNITY_ANDROID
using System.Collections;
using Google.Play.AppUpdate;
using UnityEngine;

public sealed class GooglePlayInAppUpdate : MonoBehaviour
{
    private IEnumerator Start()
    {
        var updateManager = new AppUpdateManager();
        var infoRequest = updateManager.GetAppUpdateInfo();
        yield return infoRequest;

        if (!infoRequest.IsSuccessful)
        {
            Debug.LogWarning($"Google Play update check failed: {infoRequest.Error}");
            yield break;
        }

        AppUpdateInfo info = infoRequest.GetResult();
        if (info == null) yield break;

        if (info.AppUpdateStatus == AppUpdateStatus.Downloaded)
        {
            yield return CompleteUpdate(updateManager);
            yield break;
        }

        if (info.UpdateAvailability != UpdateAvailability.UpdateAvailable) yield break;

        AppUpdateOptions options = AppUpdateOptions.FlexibleAppUpdateOptions();
        if (!info.IsUpdateTypeAllowed(options)) yield break;

        AppUpdateRequest updateRequest = updateManager.StartUpdate(info, options);
        yield return updateRequest;

        if (updateRequest.Error != AppUpdateErrorCode.NoError)
        {
            Debug.LogWarning($"Google Play in-app update failed: {updateRequest.Error}");
            yield break;
        }

        if (updateRequest.Status == AppUpdateStatus.Downloaded)
            yield return CompleteUpdate(updateManager);
    }

    private static IEnumerator CompleteUpdate(AppUpdateManager updateManager)
    {
        var completion = updateManager.CompleteUpdate();
        yield return completion;

        if (!completion.IsSuccessful)
            Debug.LogWarning($"Google Play update installation failed: {completion.Error}");
    }
}
#endif
