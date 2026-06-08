using System;
using System.Collections.Generic;
using Sekai.UI;
using UnityEngine;

namespace Sekai
{
	public static class DialogUtility
	{
		public static SubWindowDialog ShowCommonSubWindowDialog(string messageBody, Action onClose = null)
		{
			return ScreenManager.Instance?.ShowSubWindowDialog<SubWindowDialog>(
				messageBody,
				onClose,
				true,
				DialogType.SubWindowDialog,
				DisplayLayerType.Layer_Dialog);
		}

		public static CommonMultiButtonDialog ShowCommon3ButtonDialog(
			string messageBodyKey,
			Dictionary<string, string> labelKeyDic,
			Dictionary<string, Action> actionDic,
			DialogSize dialogSize = DialogSize.Manual,
			bool allowCloseExternal = true)
		{
			return ScreenManager.Instance?.ShowMultiButtonDialog<CommonMultiButtonDialog>(
				DialogType.Common3ButtonDialog,
				messageBodyKey,
				labelKeyDic,
				actionDic,
				DisplayLayerType.Layer_Dialog,
				dialogSize,
				allowCloseExternal);
		}
	}
}