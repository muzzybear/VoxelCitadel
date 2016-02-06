using System;
using System.Collections.Generic;
//using UnityEngine;

public class ChunkLightmap
{
	// TODO it's going to hold rbg lights in it
	private readonly ushort[,,] _data = new ushort[16,16,16];

	private readonly Chunk _chunk;

	private readonly Queue<Vector3i> _work_propagate = new Queue<Vector3i>();

	public bool NeedsPropagate() { return _work_propagate.Count > 0; }

	// invoked when lighting calculations have been finished
	public Action<ChunkLightmap> OnLightingChanged;

	// ...
	public Action[] OnFaceChanged = new Action[6];

	public ChunkLightmap(Chunk chunk) {
		_chunk = chunk;
	}

	bool[] _faceChanged = new bool[6];

	private void resetFaceChanged() {
		for(int i=0; i<6; i++) _faceChanged[i]=false;
	}

	private void write(int x, int y, int z, ushort raw) {
		_data[x,y,z] = raw;
		if (x==15) _faceChanged[0] = true;
		else if (x==0) _faceChanged[1] = true;
		if (y==15) _faceChanged[2] = true;
		else if (y==0) _faceChanged[3] = true;
		if (z==15) _faceChanged[4] = true;
		else if (z==0) _faceChanged[5] = true;
	}

	public ushort GetLight(Vector3i localPosition) {
		return GetLight(localPosition.x, localPosition.y, localPosition.z);
	}

	public ushort GetLight(int x, int y, int z) {
		if (x >= 0 && x < 16 &&	y >= 0 && y < 16 &&	z >= 0 && z < 16) {
			return _data[x, y, z];
		} else {
			return _chunk.Owner.GetLight(_chunk.Key*16 + new Vector3i(x,y,z));
		}
	}

	private void attemptPropagate(Vector3i target, ushort sourceLight) {
		if (sourceLight <= 1)
			return;
		if (target.x >= 0 && target.x < 16 && target.y >= 0 && target.y < 16 && target.z >= 0 && target.z < 16) {
			if (_data[target.x, target.y, target.z] < sourceLight-1) {
				if (_chunk.GetBlock(target).isTransparent()) {
					write(target.x, target.y, target.z, (ushort) (sourceLight-1));
					_work_propagate.Enqueue(target);
				}
			}
		} else {
			// pass into the neighbor chunk
			Chunk chunk = _chunk.Owner.GetChunkAt(_chunk.Key * 16 + target);
			if (chunk != null) {
				ChunkLightmap lightmap = _chunk.Owner.GetLightmap(chunk.Key);
				lightmap.attemptPropagate(_chunk.Key * 16 + target - chunk.Key*16, sourceLight);
			}
		}
	}

	// using a lambda makes this around 5% slower but it's totally worth it
	public void EnumerateSunlight(Action<int,int,int> handleSunlight) {
		for (int x=0; x<16; x++) {
			for (int z=0; z<16; z++) {
				int sunlightHeight = _chunk.Owner.Sunlight.GetSunlightAt(_chunk.Key.x, _chunk.Key.z, x, z);
				int cy = _chunk.Key.y;
				// translate sunlightHeight into local coordinates
				int sy = sunlightHeight - cy*16;
				// see if sunlight breaks before reaching this chunk
				if (sy >= 16)
					continue;

				for(int y=15; y>=0 && y>=sy; y--) {
					handleSunlight(x,y,z);
				}
			}
		}
	}

	public void ApplyDirectSunlight() {
		// initialize first to sunlight only
		EnumerateSunlight((x,y,z) => write(x,y,z, 15) );
	}

	public void PropagateSunlight() {
		BlockFace[] directions = new BlockFace[] {BlockFace.Left, BlockFace.Right, BlockFace.Back, BlockFace.Front};

		EnumerateSunlight((x,y,z) => {
			foreach (BlockFace direction in directions) {
				attemptPropagate(new Vector3i(x,y,z) + CubeBuilder.cubeNormals[(int)direction], 15);
			}
		});

		Propagate();
	}

	public void Propagate() {
		// propagate the rest of the work
		while (_work_propagate.Count > 0) {
			Vector3i source = _work_propagate.Dequeue();
			ushort sourceLight = _data[source.x, source.y, source.z];
			if (sourceLight > 1) {
				for(int i=0; i<6; i++) {
					attemptPropagate(source + CubeBuilder.cubeNormals[i], sourceLight);
				}
			}
		}

		// invoke delegates
		if (OnLightingChanged != null)
			OnLightingChanged(this);
		
		for(int i=0; i<6; i++) {
			if (_faceChanged[i] && OnFaceChanged[i] != null)
				OnFaceChanged[i]();
		}
		resetFaceChanged();
	}
}
