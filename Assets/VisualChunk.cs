﻿using System;
using UnityEngine;

public class VisualChunk
{
	private readonly Chunk _chunk;
	public Chunk Chunk { get { return _chunk; } }

	private readonly World _world;

	private Mesh _mesh;
	private GameObject _object;

	public VisualChunk (Chunk chunk, World world, GameObject parentObject)
	{
		_chunk = chunk;
		_world = world;

		_mesh = new Mesh();
		// DEBUG foofoo
		_mesh.MarkDynamic();

		_object = new GameObject(
			string.Format("chunk_{0}_{1}_{2}", _chunk.Key.x, _chunk.Key.y, _chunk.Key.z),
			new Type[] { typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider) }
		);
		
		// TODO maybe we should've just passed Transform to begin with?
		_object.transform.parent = parentObject.transform;

		var meshFilter = _object.GetComponent<MeshFilter>();
		var meshcollider = _object.GetComponent<MeshCollider>();
		var renderer = _object.GetComponent<MeshRenderer>();

        Material mat = parentObject.GetComponent<WorldController>().VoxelMaterial;

		mat.mainTexture = _atlas.Texture;
		renderer.sharedMaterial = mat;

		//_meshcollider.sharedMaterial = ... do we need physics material?

		// position things
		_object.transform.localPosition = (Vector3) (_chunk.Key * 16);

		for (int i=0; i<6; i++) {
			Chunk neighbor = null;
			world.Chunks.TryGetValue(_chunk.Key + CubeBuilder.cubeNormals[i], out neighbor);
			if (neighbor != null) {
				int opposite = i^1;
				world.GetLightmapFor(neighbor).OnFaceChanged[opposite] += delegate() {
					_dirty = true;
				};
			}
		}

		RebuildMesh();

		// TODO is it a good idea to use meshcollider for terrain?
		meshcollider.sharedMesh = null;
		meshcollider.sharedMesh = _mesh;
		meshFilter.sharedMesh = _mesh;
	}

	// flagging self dirty if rebuild needed, e.g. neighbour edge lights change
	private bool _dirty = false;
	public bool Dirty { get { return _dirty; }}

	// Unity isn't threading the userscripts, so it's safe to reuse the same builder and avoid some GC hits
	private static MeshBuilder _meshBuilder = new MeshBuilder();

	// this better not get constructed too early
	private static BlockAtlas _atlas = new BlockAtlas();

	public void RebuildMesh() {
		ChunkLightmap lightmap = _world.GetLightmap(_chunk.Key);

		var appearances = new System.Collections.Generic.Dictionary<BlockType, BlockAppearance>();
		// air
		appearances.Add(BlockType.Empty, new BlockAppearance());
		// stone
		BlockAppearance tmp = new BlockAppearance();
		for(int i=0; i<6; i++) {
			tmp.UVRects[i] = _atlas.Rects[0];
			tmp.Colors[i] = new Color(0.4f, 0.4f, 0.4f);
		}
		appearances.Add(new BlockType(2), tmp);
		// dirt
		tmp = new BlockAppearance();
		for (int i=0; i<6; i++) {
			if (i != (int)BlockFace.Top) {
				tmp.UVRects[i] = _atlas.Rects[1];
			} else {
				tmp.UVRects[i] = _atlas.Rects[3];
			}
		}
		appearances.Add(new BlockType(1), tmp);
		// grass
		tmp = new BlockAppearance();
		for (int i=0; i<6; i++) {
			tmp.UVRects[i] = _atlas.Rects[2];
		}
		tmp.UVRects[(int)BlockFace.Top] = _atlas.Rects[4];
		tmp.UVRects[(int)BlockFace.Bottom] = _atlas.Rects[1];
	
		appearances.Add(new BlockType(3), tmp);

		Profiler.BeginSample("RebuildMesh");
		for (int x=0; x<16; x++) {
			for (int y=0; y<16; y++) {
				for (int z=0; z<16; z++) {
					BlockType block = _chunk.GetBlock(x,y,z);
					if (block.isTransparent() == false && _chunk.IsBlockVisible(x,y,z)) {

						BlockAppearance appearance;
						if (!appearances.TryGetValue(block, out appearance))
							appearance = new BlockAppearance();

						CubeBuilder.buildCube(_meshBuilder, new Vector3i(x, y, z), _chunk, lightmap, appearance);
					}
				}
			}
		}

		_mesh.Clear();
		_meshBuilder.BuildMesh(_mesh);
		_meshBuilder.Clear();
		_dirty = false;
		Profiler.EndSample();
	}
}
