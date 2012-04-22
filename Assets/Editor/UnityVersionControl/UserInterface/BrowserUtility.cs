// File Browser Editor Window Functionality
// BrowserUtility.cs
// Unity Version Control
//  
// Authors:
//       Josh Montoute <josh@thinksquirrel.com>
// 
// Copyright (c) 2012, Thinksquirrel Software, LLC
//
// This file is part of Unity Version Control.
//
//    Unity Version Control is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    Unity Version Control is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with Unity Version Control.  If not, see <http://www.gnu.org/licenses/>.
//
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThinksquirrelSoftware.UnityVersionControl.Core;

namespace ThinksquirrelSoftware.UnityVersionControl.UserInterface
{
	/// <summary>
	/// Provides functionality for the browser editor window.
	/// </summary>
	/// <remarks>
	/// This seperates interface functionality from design/display.
	/// </remarks>
	/// TODO: (Git) Files that are staged and then modified show up twice - these need to only show up in the staged area
	public static class BrowserUtility
	{	
		#region Member fields
		/// The frame count and update rate for updates
		private static int mFrameCount;
		private const int mUpdateRate = 600;
		
		/// The repository location
		private static string mRepositoryLocation;
		
		// Staged files and working tree
		private static Dictionary<string, VCFile> mStagedFiles = new Dictionary<string, VCFile>();
		private static Dictionary<string, VCFile> mWorkingTree = new Dictionary<string, VCFile>();
		private static bool mStagedFileSelected;
		private static bool mWorkingTreeSelected;
		private static bool mAnyFileSelected;
		
		// Selected files
		private static VCFile[] mSelectedFileCache;
		
		// Diff
		private static string mDiffString = string.Empty;
		
		// Stats
		private static int mModifiedFileCount;
		private static int mAddedFileCount;
		private static int mDeletedFileCount;
		private static int mRenamedFileCount;
		private static int mCopiedFileCount;
		private static int mUnmergedFileCount;
		private static int mUntrackedFileCount;
		#endregion
		
		#region Public properties
		public static string repositoryLocation
		{
			get
			{
				return mRepositoryLocation;
			}
		}
		public static Dictionary<string, VCFile> stagedFiles
		{
			get
			{
				return mStagedFiles;
			}
		}
		public static Dictionary<string, VCFile> workingTree
		{
			get
			{
				return mWorkingTree;
			}
		}
		public static VCFile[] selectedFileCache
		{
			get
			{
				return mSelectedFileCache;
			}
		}
		public static bool stagedFileSelected
		{
			get
			{
				return mStagedFileSelected;
			}
		}
		public static bool workingTreeSelected
		{
			get
			{
				return mWorkingTreeSelected;
			}
		}
		public static bool anyFileSelected
		{
			get
			{
				return mAnyFileSelected;
			}
		}
		public static string diffString
		{
			get
			{
				return mDiffString;
			}
		}
		public static int modifiedFileCount
		{
			get
			{
				return mModifiedFileCount;
			}
		}
		public static int addedFileCount
		{
			get
			{
				return mAddedFileCount;
			}
		}
		public static int deletedFileCount
		{
			get
			{
				return mDeletedFileCount;
			}
		}
		public static int renamedFileCount
		{
			get
			{
				return mRenamedFileCount;
			}
		}
		public static int copiedFileCount
		{
			get
			{
				return mCopiedFileCount;
			}
		}
		public static int unmergedFileCount
		{
			get
			{
				return mUnmergedFileCount;
			}
		}
		public static int untrackedFileCount
		{
			get
			{
				return mUntrackedFileCount;
			}
		}
		#endregion
		
		#region Public methods
		/// <summary>
		/// Runs on EditorWindow.OnEnable().
		/// </summary>
		public static void OnEnable()
		{
			mFrameCount = 0;
			mRepositoryLocation = Git.RepositoryLocation();
		}
		
		/// <summary>
		/// Runs on EditorWindow.Update().
		/// </summary>
		public static void Update()
		{
			if (mFrameCount % mUpdateRate == 0)
			{
				UpdateTrees();	
			}
			
			mFrameCount++;
		}
		
		/// <summary>
		/// Forces an update.
		/// </summary>
		public static void ForceUpdate()
		{
			mFrameCount = 0;
			Update();
		}
		
		/// <summary>
		/// Raises the Init Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Init(UVCBrowser browser)
		{
			browser.OnProcessStart();
			UVCProcessPopup.Init(VersionControl.Initialize(CommandLine.EmptyHandler), true, true, browser.OnProcessStop);
		}
		
		/// <summary>
		/// Raises the Commit Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Commit(UVCBrowser browser)
		{
			browser.OnProcessStart();
			UVCCommitPopup.Init(browser);
		}
		
		/// <summary>
		/// Raises the Checkout Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Checkout(UVCBrowser browser)
		{
		}
		
		/// <summary>
		/// Raises the Reset Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Reset(UVCBrowser browser)
		{
		}
		
		/// <summary>
		/// Raises the Add Button event.
		/// </summary>
		public static void OnButton_Add(UVCBrowser browser)
		{
			browser.OnProcessStart();
			UVCProcessPopup.Init(VersionControl.Add(CommandLine.EmptyHandler, mSelectedFileCache), true, true, browser.OnProcessStop);
		}
		
		/// <summary>
		/// Raises the Remove Button event.
		/// </summary>
		/// TODO: <<IMPORTANT>> Implement confirmation before removing modified files from the index
		public static void OnButton_Remove(UVCBrowser browser)
		{
			browser.OnProcessStart();
			if (VersionControl.versionControlType == VersionControlType.Git && mStagedFileSelected)
			{
				// Git only - Clicking the remove button for a staged file will unstage it
				UVCProcessPopup.Init(VersionControl.Reset(CommandLine.EmptyHandler, "HEAD", mSelectedFileCache), true, true, browser.OnProcessStop);
			}
			else
			{
				bool dialog = false;
				var modFiles = new System.Text.StringBuilder();
				
				foreach(var file in mSelectedFileCache)
				{
					if (file.fileState2 == FileState.Modified)
					{
						dialog = true;
						if (string.IsNullOrEmpty(file.path2))
						{
							modFiles.Append(file.path1).Append('\n');
						}
						else
						{
							modFiles.Append(file.path2).Append('\n');
						}
					}
				}
				
				if (dialog)
				{
					if (EditorUtility.DisplayDialog(
						"Confirm Remove Modified or Untracked Files?",
			            "The following files contain changes or information which is not in source control, and will be irretrievably lost if you remove them:\n" + 
						modFiles.ToString(0, modFiles.Length -1), "Ok", "Cancel"))
					{
						UVCProcessPopup.Init(VersionControl.Remove(CommandLine.EmptyHandler, mSelectedFileCache), true, true, browser.OnProcessStop);
					}
					else
					{
						browser.OnProcessStop(-9999);
					}
				}
				else
				{
					UVCProcessPopup.Init(VersionControl.Remove(CommandLine.EmptyHandler, mSelectedFileCache), true, true, browser.OnProcessStop);
				}
				
			}
		}
		
		/// <summary>
		/// Raises the Fetch Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Fetch(UVCBrowser browser)
		{
		}
		
		/// <summary>
		/// Raises the Pull Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Pull(UVCBrowser browser)
		{
		}
		
		/// <summary>
		/// Raises the Push Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Push(UVCBrowser browser)
		{
		}
		
		/// <summary>
		/// Raises the Branch Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Branch(UVCBrowser browser)
		{
		}
		
		/// <summary>
		/// Raises the Tag Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Tag(UVCBrowser browser)
		{
		}
		
		/// <summary>
		/// Raises the Settings Button event.
		/// </summary>
		/// TODO: Implement button
		public static void OnButton_Settings(UVCBrowser browser)
		{
		}
		
		/// <summary>
		/// Validates the selection.
		/// </summary>
		public static void ValidateSelection(VCFile file, bool toggle)
		{
			if (file != null)
				file.selected = toggle;
					
			foreach(var f in stagedFiles.Values)
			{
				if (f.selected)
				{
					mStagedFileSelected = true;
					break;
				}
			}
			
			foreach(var f in workingTree.Values)
			{
				if (f.selected)
				{
					mWorkingTreeSelected = true;
					break;
				}
			}
			
			// Only run if selecting a file
			if (file != null && toggle)
			{
				// Selecting a staged file with working tree files selected (not allowed)
				if (stagedFiles.ContainsValue(file) && mWorkingTreeSelected)
				{
					foreach(var f in workingTree.Values)
					{
						f.selected = false;
					}
					mWorkingTreeSelected = false;
				}
				// Selecting a working tree file with staged files selected (not allowed)
				else if (workingTree.ContainsValue(file) && mStagedFileSelected)
				{
					foreach(var f in stagedFiles.Values)
					{
						f.selected = false;
					}
					mStagedFileSelected = false;
				}
			}
			
			mAnyFileSelected = mStagedFileSelected || mWorkingTreeSelected;
			
			CacheSelectedFiles();
			UpdateDiffPanel();
		}
		#endregion
		
		#region Private methods
		private static void CacheSelectedFiles()
		{
			var fileList = new List<VCFile>();
			
			foreach(var file in workingTree.Values)
			{
				if (file.selected)
					fileList.Add(file);
			}
			
			foreach(var file in stagedFiles.Values)
			{
				if (file.selected)
					fileList.Add(file);
			}
			
			mSelectedFileCache = fileList.ToArray();
		}
		
		/// Updates all tree listings by running three git commands.
		private static void UpdateTrees()
		{
			if (string.IsNullOrEmpty(mRepositoryLocation))
			{
				mRepositoryLocation = Git.RepositoryLocation();
			}
			
			// Get list of files
			VersionControl.FindFiles(OnFindFiles);
		}
		
		// TODO: Handle errors
		private static void OnFindFiles(object sender, System.EventArgs e)
		{
			var process = sender as System.Diagnostics.Process;
			
			var files = VersionControl.ParseFiles(process.StandardOutput.ReadToEnd());
			
			#region removal
			var toRemove = new List<string>();
			
			// Staged files
			foreach(var kvp in stagedFiles)
			{
				bool keep = false;
				foreach(var file in files)
				{
					if (file.fileState1 != FileState.Unmodified && file.fileState1 != FileState.Untracked && file.fileState1 != FileState.Ignored)
					{
						if (kvp.Key.Equals(file.path1 + file.path2))
						{
							// Found a match
							keep = true;
							break;
						}
					}
				}
				
				// Add to removal queue
				if (!keep)
					toRemove.Add(kvp.Key);
			}
			
			// Perform removal
			foreach(var str in toRemove)
			{
				stagedFiles.Remove(str);
			}
			
			toRemove.Clear();
			
			// Working tree
			foreach(var kvp in workingTree)
			{
				bool keep = false;
				foreach(var file in files)
				{
					if ((file.fileState2 != FileState.Unmodified && file.fileState2 != FileState.Untracked && file.fileState2 != FileState.Ignored) ||
						(file.fileState1 == FileState.Untracked && file.fileState2 == FileState.Untracked) ||
						(file.fileState1 == FileState.Ignored && file.fileState2 == FileState.Ignored))
					{
						if (kvp.Key.Equals(file.path1 + file.path2))
						{
							// Found a match
							keep = true;
							break;
						}
					}
				}
				
				// Add to removal queue
				if (!keep)
					toRemove.Add(kvp.Key);
			}
			
			// Perform removal
			foreach(var str in toRemove)
			{
				workingTree.Remove(str);
			}
			#endregion
			
			// Addition
			foreach(var file in files)
			{
				// Staged files
				if (file.fileState1 != FileState.Unmodified && file.fileState1 != FileState.Untracked && file.fileState1 != FileState.Ignored)
				{
					// Check for duplicate
					if (!stagedFiles.ContainsKey(file.path1 + file.path2))
					{
						// Add value
						stagedFiles.Add(file.path1 + file.path2, file);
					}
				}
				
				// Working tree
				if (file.fileState2 != FileState.Unmodified && file.fileState2 != FileState.Untracked && file.fileState2 != FileState.Ignored)
				{
					// Check for duplicate
					if (!workingTree.ContainsKey(file.path1 + file.path2))
					{
						// Add value
						workingTree.Add(file.path1 + file.path2, file);
					}
				}
				
				// Untracked (added to working tree)
				if (file.fileState1 == FileState.Untracked && file.fileState2 == FileState.Untracked)
				{
					// Check for duplicate
					if (!workingTree.ContainsKey(file.path1 + file.path2))
					{
						// Add value
						workingTree.Add(file.path1 + file.path2, file);
					}
				}
				
				// Ignored (added to working tree)
				if (file.fileState1 == FileState.Ignored && file.fileState2 == FileState.Ignored)
				{
					// Check for duplicate
					if (!workingTree.ContainsKey(file.path1 + file.path2))
					{
						// Add value
						workingTree.Add(file.path1 + file.path2, file);
					}
				}
			}
			
			// Validate selection
			ValidateSelection(null, false);
			
			// Update stats
			UpdateStats();
		}
		
		// TODO: "Pretty" diff parsing
		// TODO: Handle errors
		private static void OnGetDiff(object sender, System.EventArgs e)
		{
			mDiffString = (sender as System.Diagnostics.Process).StandardOutput.ReadToEnd();
		}
		
		private static void UpdateStats()
		{
			mAddedFileCount = 0;
			mCopiedFileCount = 0;
			mDeletedFileCount = 0;
			mModifiedFileCount = 0;
			mRenamedFileCount = 0;
			mUnmergedFileCount = 0;
			mUntrackedFileCount = 0;
			
			foreach(var file in mWorkingTree.Values)
			{
				switch(file.fileState2)
				{
				case FileState.Added:
					mAddedFileCount++;
					break;
				case FileState.Copied:
					mCopiedFileCount++;
					break;
				case FileState.Deleted:
					mDeletedFileCount++;
					break;
				case FileState.Modified:
					mModifiedFileCount++;
					break;
				case FileState.Renamed:
					mRenamedFileCount++;
					break;
				case FileState.Unmerged:
					mUnmergedFileCount++;
					break;
				case FileState.Untracked:
					mUntrackedFileCount++;
					break;
				}
			}
		}
		
		private static void UpdateDiffPanel()
		{
			mDiffString = string.Empty;
			
			var fileList = new List<VCFile>();
			
			foreach(var file in mWorkingTree.Values)
			{
				if (file.selected)
				{
					fileList.Add(file);
				}
			}
			
			if (fileList.Count > 0)
				VersionControl.GetDiff(OnGetDiff, fileList.ToArray());
		}

		#endregion
	}
}