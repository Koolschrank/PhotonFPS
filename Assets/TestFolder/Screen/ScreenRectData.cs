using System;
using UnityEngine;

namespace SimpleFPS
{
	[Serializable]
	public struct ScreenRectValues
	{
		public float x;
		public float y;
		public float width;
		public float height;
		public float FOVMultiplier;
		//public int targetDisplay;

		public ScreenRectValues(float x, float y, float width, float height, float FOVMultiplier)
		{
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
			this.FOVMultiplier = FOVMultiplier;
		}

	}

	[Serializable]
	public struct ScreenRectArray
	{
		public ScreenRectValues[] screenRectValues;
		public ScreenRectArray(ScreenRectValues[] screenRectValues)
		{
			this.screenRectValues = screenRectValues;


		}
	}

}
