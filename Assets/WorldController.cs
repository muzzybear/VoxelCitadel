using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorldController : MonoBehaviour {

	private World _world = new World(12, 8, 12);

	private Dictionary<Vector3i, VisualChunk> _vis = new Dictionary<Vector3i, VisualChunk>();

	private Queue<Chunk> _foofoo = new Queue<Chunk>();

	// ....
	private Transform _targeting;


	void Start () {
		// DEBUG foofoo testing...
		_targeting = GameObject.Find("Targeting").transform;

		ChunkGenerator chunkgen = new ChunkGenerator();

		var sw = new System.Diagnostics.Stopwatch();
		sw.Start();
		for (int cx=0; cx<_world.SizeX; cx++) {
			for (int cy=0; cy<_world.SizeY; cy++) {
				for (int cz=0; cz<_world.SizeZ; cz++) {
					var chunk = _world.CreateChunk(new Vector3i(cx,cy,cz), chunkgen);
				}
			}
		}
		Debug.Log("World generation took "+sw.ElapsedMilliseconds+ " ms");
		sw.Reset();
		sw.Start();

		// make some dummy content
		_world.BeginBatchUpdate();
		for (int x=0; x<_world.SizeX*16; x++) {
			int z = 91;

			_world.SetBlock(new Vector3i(x, 85, z-2), new BlockType(2));
			_world.SetBlock(new Vector3i(x, 85, z-1), new BlockType(2));
			_world.SetBlock(new Vector3i(x, 85, z), new BlockType(2));
			_world.SetBlock(new Vector3i(x, 85, z+1), new BlockType(2));
			_world.SetBlock(new Vector3i(x, 85, z+2), new BlockType(2));

			_world.SetBlock(new Vector3i(x, 86, z), new BlockType(0));
			_world.SetBlock(new Vector3i(x, 87, z), new BlockType(0));
			_world.SetBlock(new Vector3i(x, 88, z), new BlockType(0));
			_world.SetBlock(new Vector3i(x, 86, z-1), new BlockType(0));
			_world.SetBlock(new Vector3i(x, 87, z-1), new BlockType(0));
			_world.SetBlock(new Vector3i(x, 86, z+1), new BlockType(0));
			_world.SetBlock(new Vector3i(x, 87, z+1), new BlockType(0));


			// pillars
			if (x%6 == 3) {
				for(int y=84; y>0 && _world.GetBlock(new Vector3i(x,y,z)).isTransparent(); y--) {
					_world.SetBlock(new Vector3i(x, y, z), new BlockType(2));
				}
			}
		}
		_world.EndBatchUpdate();

		_world.Sunlight.CalculateAll();

		// Do initial direct lighting
		for (int cy=_world.SizeY-1; cy>=0; cy--) {
			for (int cx=0; cx<_world.SizeX; cx++) {
				for (int cz=0; cz<_world.SizeZ; cz++) {
					ChunkLightmap lightmap = _world.GetLightmap(new Vector3i(cx,cy,cz));
					lightmap.ApplyDirectSunlight();
				}
			}
		}

		// spread the sunlight all around the place
		for (int cy=_world.SizeY-1; cy>=0; cy--) {
			for (int cx=0; cx<_world.SizeX; cx++) {
				for (int cz=0; cz<_world.SizeZ; cz++) {
					ChunkLightmap lightmap = _world.GetLightmap(new Vector3i(cx,cy,cz));
					lightmap.PropagateSunlight();
				}
			}
		}

		Debug.Log("Sunlight initialization took "+sw.ElapsedMilliseconds+ " ms");
		sw.Reset();
		sw.Start();


		foreach (Chunk chunk in _world.Chunks.Values)
			_foofoo.Enqueue(chunk);

		StartCoroutine(CheckLighting());
	}

	// FIXME if lighting affects gameplay, it is unacceptable to delay it!!!
	// mesh rebuilds can be delayed, it's just representation

	IEnumerator CheckLighting() {
		// first wait for previous work to finish, then check once per second
		do {
			yield return new WaitForSeconds(1.0f);
		} while (_foofoo.Count > 0);

		// queue new work
		foreach (Chunk chunk in _world.Chunks.Values) {
			ChunkLightmap lightmap = _world.GetLightmapFor(chunk);
			if (lightmap.NeedsPropagate()) {
				_foofoo.Enqueue(chunk);
			}
		}
		foreach (VisualChunk vis in _vis.Values) {
			if (vis.Dirty)
				_foofoo.Enqueue(vis.Chunk);
		}
	}

	void Update () {
		Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));
		RaycastHit hit;

		//Debug.DrawLine(ray.origin, ray.direction*100);

		if (Physics.Raycast(ray, out hit)) {
			//Debug.Log("HIT!");
			_targeting.transform.localPosition = hit.point;
		}

		// TODO any better way to not waste time? threads? :P
		var sw = new System.Diagnostics.Stopwatch();
		sw.Start();

		// process visual chunks (rebuild meshes if world has changed etc)
		while(_foofoo.Count > 0) {
			// spend a maximum of 10ms on this nonsense
			if (sw.ElapsedMilliseconds > 10)
				break;
			
			Chunk chunk = _foofoo.Dequeue();
			VisualChunk vis;
			_vis.TryGetValue(chunk.Key, out vis);
			if (vis != null) {
				ChunkLightmap lightmap = _world.GetLightmapFor(chunk);
				lightmap.Propagate();
				vis.RebuildMesh();
			} else {
				vis = new VisualChunk(chunk, _world, gameObject);
				_vis.Add(chunk.Key, vis);
			}
		}
	}
}
