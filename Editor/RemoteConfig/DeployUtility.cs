using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor;

namespace Kott.Configs
{
    /// <summary>Deploys files through the Deployment window of the Unity Services Deployment package.</summary>
    public static class DeployUtility
    {
        /// <summary>Deploys the files at <paramref name="paths" /> (asset paths) to the current environment.</summary>
        public static async Task<DeploymentResult<IDeploymentItem>> DeployAsync(IReadOnlyList<string> paths)
        {
            using TempDeploymentWindowContainer windowContainer = new();
            return await windowContainer.Window.Deploy(paths);
        }

        private sealed class TempDeploymentWindowContainer : IDisposable
        {
            private readonly EditorWindow _editorWindow;
            private readonly int _progressID;

            public TempDeploymentWindowContainer()
            {
                _progressID = Progress.Start("Using deployment window", options: Progress.Options.Indefinite);

                _editorWindow = Deployments.Instance.DeploymentWindow.OpenWindow();
                Window = Deployments.Instance.DeploymentWindow;
            }

            public IDeploymentWindow Window { get; }

            public void Dispose()
            {
                _editorWindow.Close();
                Progress.Remove(_progressID);
            }
        }
    }
}
