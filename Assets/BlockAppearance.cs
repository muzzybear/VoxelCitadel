using System;
using UnityEngine;

// TODO maybe struct?
public class BlockAppearance
{
	readonly public Color[] Colors = new Color[6] {Color.white, Color.white, Color.white, Color.white, Color.white, Color.white, };
	readonly public Rect[] UVRects = new Rect[6];

	public BlockAppearance ()
	{
	}
}
