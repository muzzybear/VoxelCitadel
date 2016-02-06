using System;
using UnityEngine;

public class ChunkGenerator : IChunkLoader
{
	private SimplexNoiseGenerator _simplex;

	public ChunkGenerator ()
	{
		_simplex = new SimplexNoiseGenerator(new int[]{1,2,3,4,5,6,7,8});
	}

	public void LoadChunk(Vector3i key, BlockType[,,] output) {

		for (int x=0; x<16; x++) {
			for (int z=0; z<16; z++) {
				int wx = key.x*16 + x;
				int wz = key.z*16 + z;
				float foo;
				//float foo = Mathf.PerlinNoise(wx*0.1f, wz*0.1f);
				//foo = Mathf.Cos(foo*Mathf.PI)*0.5f + 0.5f;
				//foo *= foo;
				//foo *= 0.15f;

				foo = Mathf.PerlinNoise(123 + wx*0.02f, 456 + wz*0.02f)*0.8f;
				foo *= 64;
				foo += 64;

				for (int y=0; y<16; y++) {
					BlockType block;
					int wy = key.y*16 + y;
					if (wy > foo) {
						// Air above ground level
						block = BlockType.Empty;
					} else {						
						block = new BlockType(1);
						if (wy < (foo-1)*0.95f) {
							block = new BlockType(2);
						}
					}
					if (block != BlockType.Empty) {
						
						float caves = _simplex.noise(wx*0.05f,wy*0.05f,wz*0.05f);
						if (caves>0.05f) {
							// hole
							block = BlockType.Empty;
						}
					}
					output[x,y,z] = block;
				}
			}

		}

	}
}

