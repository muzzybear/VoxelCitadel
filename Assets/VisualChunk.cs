using System;
using UnityEngine;

public class VisualChunk
{
	private readonly Chunk _chunk;
	public Chunk Chunk { get { return _chunk; } }

	private readonly World _world;

	//private MeshFilter _meshFilter;
	//private MeshCollider _meshcollider;

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
		Material mat = new Material(Shader.Find("Unlit/VoxelShader"));
		/*
		var tex = Resources.Load<Texture2D>("test-64x64");
		if (tex == null) {
			Debug.Log("Failed to load test texture :(");
		}
		*/
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
		mat.mainTexture = tex;
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

	public void RebuildMesh() {
		ChunkLightmap lightmap = _world.GetLightmap(_chunk.Key);

		Profiler.BeginSample("RebuildMesh");
		for (int x=0; x<16; x++) {
			for (int y=0; y<16; y++) {
				for (int z=0; z<16; z++) {
					if (_chunk.GetBlock(x,y,z).isTransparent() == false && _chunk.IsBlockVisible(x,y,z)) {
						CubeBuilder.buildCube(_meshBuilder, new Vector3i(x, y, z), _chunk, lightmap);
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
