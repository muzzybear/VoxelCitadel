using System;
using UnityEngine;
using System.Collections.Generic;

public class BlockAtlas
{
	private Texture2D _atlas;
	public Texture2D Texture { get { return _atlas; } }
	private Rect[] _rects;
	public Rect[] Rects { get { return _rects; } }

	//public BlockAppearance appearances;

	public BlockAtlas ()
	{
		// TODO how large an atlas do we need?
		_atlas = new Texture2D(2048,2048, TextureFormat.ARGB32, false);

		// .. generating some dummy textures for now
		List<Texture2D> textures = new List<Texture2D>();

		var tex = new Texture2D(64, 64);
		tex.wrapMode = TextureWrapMode.Clamp;
		Color[] pixels = new Color[tex.width*tex.height];
		for (int y=0; y<tex.height; y++) {
			for (int x=0; x<tex.width; x++) {
				float v = UnityEngine.Random.value;
				v = (1f-v*v)*0.5f + 0.5f;
				pixels[y*tex.width + x] = new Color(v,v,v);
			}
		}
		tex.SetPixels(pixels);
		tex.Apply();

		textures.Add(tex);

		textures.Add(Resources.Load<Texture2D>("dirtblock-side"));
		textures.Add(Resources.Load<Texture2D>("grassblock-side"));
		textures.Add(Resources.Load<Texture2D>("dirtblock-top"));
		textures.Add(Resources.Load<Texture2D>("grassblock-top"));

		// pack atlas and make it unreadable to free the memory
		_rects = _atlas.PackTextures(textures.ToArray(), 0, 2048, true);
		_atlas.filterMode = FilterMode.Point;
		Debug.Log("Foo: "+_atlas.mipmapCount);
	}


}

