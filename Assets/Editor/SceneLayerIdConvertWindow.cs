﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class SceneLayerIdConvertWindow : LayerIdConvertWindowBase
{
	protected override string AssetType => "Scene";

	[MenuItem("Tools/LayerIdConverter/Scene")]
	private static void OpenScene()
	{
		SceneLayerIdConvertWindow window = GetWindow<SceneLayerIdConvertWindow>();
		window.titleContent = new GUIContent("SceneLayerIdConverter");
		window.minSize = new Vector2(300f, 300f);
	}


	protected override void Execute(List<string> pathList, ConvertData convertSettings, bool isChangeChildren)
	{
		EditorSceneManager.SaveOpenScenes();
		string currentScenePath = SceneManager.GetActiveScene().path;

		List<GeneralEditorIndicator.Task> tasks = new List<GeneralEditorIndicator.Task>();
		foreach (string path in pathList) {
			try {
				tasks.Add(new GeneralEditorIndicator.Task(() => { this.ChangeLayer(path, convertSettings, isChangeChildren); }, path));
			}
			catch {
				EditorSceneManager.OpenScene(currentScenePath);
				throw;
			}
		}

		GeneralEditorIndicator.Show(
			"SceneLayerIdConverter",
			tasks,
			() => {
				EditorSceneManager.OpenScene(currentScenePath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		);
	}


	private void ChangeLayer(string path, ConvertData convertSettings, bool isChangeChildren)
	{
		EditorSceneManager.OpenScene(path);
		Scene scene = SceneManager.GetSceneByPath(path);
		List<GameObject> gameObjects = scene.GetRootGameObjects().ToList();
		List<string> results = new List<string>();

		foreach (GameObject target in gameObjects) {
			bool isChanged = false;
			if (isChangeChildren) {
				Utility.ScanningChildren(
					target,
					(child, layerName) => {
						List<string> result = this.ChangeLayer(layerName, child, convertSettings);
						if (result != null && result.Count > 0) {
							results.AddRange(result);
							isChanged = true;
						}
					}
				);
			}
			else {
				List<string> result = this.ChangeLayer(target.name, target, convertSettings);
				if (result != null && result.Count > 0) {
					results.AddRange(result);
					isChanged = true;
				}
			}
			if (isChanged) {
				EditorUtility.SetDirty(target);
			}
		}

		if (results.Count > 0) {
			Debug.Log(string.Format(
				"[SceneLayerIdConverter] {0}, Change Children = {1}\n{2}",
				path,
				isChangeChildren,
				string.Join("\n", results)
			));
		}

		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
	}
}