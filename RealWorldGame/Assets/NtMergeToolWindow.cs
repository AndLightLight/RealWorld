/* 
**************************************
* Popo Acount: 
* Version: 1.0
* UnityVersion: 2017.1.2p2
* Date: 12/10/2018
* Description: 
**************************************
*/

using UnityEditor;
using UnityEngine;

/// <summary>
/// 自动合并工具
/// </summary>
public class NtMergeToolWindow : EditorWindow
{
	string defaultPath = "";

	//只输出log不改变任何文件？
	public bool OnlyLog = true;
	//Log输出文件路径
	public string LogPath = "./MergeToolLog.txt";
	//是否只文件夹对比不做文件详细合并
	public bool OnlyFoderCompare = false;
	//排除目录，在这个目录的不合并,分号分隔
	public string ExceptFolder = "";
	//指定目录，不在这个目录的不合并,分号分隔
	public string SpecifyFolder = "";
	//排除后缀
	public string ExceptExtension = "";
	//指定后缀
	public string SpecifyExtension = "";
	//向上容错行
	public int UpNeedNotDirtyRow = 3;
	//向下容错行
	public int DownNeedNotDirtyRow = 2;

	[MenuItem("NetEase/版本合并工具")]
	static void Open()
	{
		NtMergeToolWindow window = EditorWindow.GetWindow<NtMergeToolWindow>();
		window.Init();
		window.Show();
	}

	void Init()
	{
		position = new Rect(new Vector2(500, 200), new Vector2(500, 400));
		title = "版本合并工具";
		minSize = new Vector2(500, 400);
		maxSize = new Vector2(500, 400);
		MergeTool.Instance.CompareCallBack += CompareFunction;
		MergeTool.Instance.CompareDetailCallBack += CompareDetailFunction;
	}

	void CompareFunction(int count, int idx, string name)
	{
		EditorUtility.DisplayProgressBar(idx + " / " + count, name, (float)idx / count);
	}

	void CompareDetailFunction(int count, int idx, string name)
	{
		EditorUtility.DisplayProgressBar(idx + " / " + count, "Compare Detail ing....", (float)idx / count);
	}

	public void OnGUI()
	{
		defaultPath = GUILayout.TextField(defaultPath);
		if (GUILayout.Button("XML文件"))
		{
			defaultPath = EditorUtility.OpenFilePanel("XML文件", defaultPath, "xml");
		}
		OnlyLog = EditorGUILayout.Toggle("只输出log", OnlyLog, GUILayout.Width(500f));
		OnlyFoderCompare = EditorGUILayout.Toggle("只文件夹对比不做文件详细合并", OnlyFoderCompare, GUILayout.Width(500f));
		GUILayout.Label("Log输出文件路径：");
		LogPath = GUILayout.TextField(LogPath);
		GUILayout.Label("向上容错行：");
		UpNeedNotDirtyRow = int.Parse(GUILayout.TextField(UpNeedNotDirtyRow.ToString()));
		GUILayout.Label("向下容错行：");
		DownNeedNotDirtyRow = int.Parse(GUILayout.TextField(DownNeedNotDirtyRow.ToString()));
		GUILayout.Label("排除目录，在这个目录的不合并,分号分隔：");
		ExceptFolder = GUILayout.TextField(ExceptFolder);
		GUILayout.Label("指定目录，不在这个目录的不合并,分号分隔：");
		SpecifyFolder = GUILayout.TextField(SpecifyFolder);
		GUILayout.Label("排除后缀：");
		ExceptExtension = GUILayout.TextField(ExceptExtension);
		GUILayout.Label("指定后缀：");
		SpecifyExtension = GUILayout.TextField(SpecifyExtension);
		GUILayout.Space(50);
		if (GUILayout.Button("合并"))
		{
			MergeTool.Instance.OnlyLog = OnlyLog;
			MergeTool.Instance.OnlyFoderCompare = OnlyFoderCompare;
			MergeTool.Instance.LogPath = LogPath;
			MergeTool.Instance.ExceptFolder = ExceptFolder;
			MergeTool.Instance.SpecifyFolder = SpecifyFolder;
			MergeTool.Instance.ExceptExtension = ExceptExtension;
			MergeTool.Instance.SpecifyExtension = SpecifyExtension;
			MergeTool.Instance.Compare(defaultPath);
			EditorUtility.ClearProgressBar();
		}


	}
}
