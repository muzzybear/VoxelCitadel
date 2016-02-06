﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorldController : MonoBehaviour {

	private World _world = new World(12, 8, 12);

	private Dictionary<Vector3i, VisualChunk> _vis = new Dictionary<Vector3i, VisualChunk>();

	private Queue<Chunk> _foofoo = new Queue<Chunk>();

	void Start () {

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

		// TODO do proper lighting...
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
				// FIXME do we need to rebuild lights?
				vis.RebuildMesh();
			} else {
				vis = new VisualChunk(chunk, _world, gameObject);
				_vis.Add(chunk.Key, vis);
			}
		}
	}
}
