using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneHelper : EditorWindow
{
	[MenuItem("Scene/Island")]
	static void LoadSplash(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/Island.unity");
	}
	[MenuItem("Scene/Puzzle")]
	static void LoadOr(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/Puzzle.unity");
	}
	[MenuItem("Scene/Stairs")]
	static void LoadStairs(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/Stairs.unity");
	}
	[MenuItem("Scene/Mobs")]
	static void LoadOpening(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/Gamefeel.unity");
	}
	[MenuItem("Scene/Boss")]
	static void LoadIcu(){
		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.OpenScene("Assets/Scenes/Boss.unity");
	}
}
