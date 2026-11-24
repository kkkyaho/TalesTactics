if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEditor.EditorApplication.isCompiling||UnityEditor.BuildPipeline.isBuildingPlayer)throw new System.InvalidOperationException("Stopped idle Editor required");
UnityEngine.Application.runInBackground=true;
var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
var game=UnityEditor.EditorWindow.GetWindow(type);game.Show();game.Focus();game.Repaint();
return new{utc=System.DateTime.UtcNow.ToString("o"),playing=UnityEditor.EditorApplication.isPlaying,background=UnityEngine.Application.runInBackground,focused=UnityEditor.EditorWindow.focusedWindow?.GetType().Name};
