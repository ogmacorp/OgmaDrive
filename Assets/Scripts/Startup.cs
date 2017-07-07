// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using UnityEngine;
//using UnityEditor;
using System.Collections;

//[InitializeOnLoad]
public class Startup
{
	static Startup()
	{
		string currentPath = System.Environment.GetEnvironmentVariable("PATH", System.EnvironmentVariableTarget.Process);
		string dllPath = Application.dataPath + System.IO.Path.DirectorySeparatorChar;

		#if UNITY_EDITOR_32
		dllPath += "Editor" + System.IO.Path.DirectorySeparatorChar + "x86";
		#elif UNITY_EDITOR_64
		dllPath += "Editor" + System.IO.Path.DirectorySeparatorChar + "x86_64";
		#else // Player
		dllPath += "Plugins";
		#endif

		Debug.Log("Current PATH = " + currentPath);
		if (currentPath != null && currentPath.Contains(dllPath) == false)
		{
			currentPath += System.IO.Path.PathSeparator + dllPath;

			Debug.Log("Adding Plugins directory to PATH (" + dllPath + ")");
			System.Environment.SetEnvironmentVariable("PATH", currentPath, System.EnvironmentVariableTarget.Process);
		}
		else
		{
			Debug.Log("Plugins directory exists in PATH (" + dllPath + ")");
		}
	}
}