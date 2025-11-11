using System;
using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;
using UnityEngine;

namespace Kott.Configs.Deploy
{
    public static class DeployUtility
    {
        private class TempDeploymentWindowContainer : IDisposable
        {
            private EditorWindow _editorWindow;
            private int _progressID;
            public readonly IDeploymentWindow Window;

            public TempDeploymentWindowContainer()
            {
                Progress.Options progressOptions = Progress.Options.Indefinite;
                _progressID = Progress.Start("Using deployment window", options: progressOptions);

                _editorWindow = Deployments.Instance.DeploymentWindow.OpenWindow();
                Window = Deployments.Instance.DeploymentWindow;
            }

            public void Dispose()
            {
                _editorWindow.Close();

                Progress.Remove(_progressID);
            }
        }

        public static async void DeployAll()
        {
            using (TempDeploymentWindowContainer windowContainer = new())
            {
                List<IDeploymentItem> deploymentItems = windowContainer.Window.GetAllDeploymentItems();
                DeploymentResult<IDeploymentItem> result = await windowContainer.Window.Deploy(deploymentItems);
                Debug.Log($"Deployed={result.Deployed}");
            }
        }
    }
}
