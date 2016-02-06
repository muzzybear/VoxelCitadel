using System;
using UnityEngine;

public class ChunkGenerator : IChunkLoader
{
	private SimplexNoiseGenerator _simplex;

	public ChunkGenerator ()
	{
		_simplex = new SimplexNoiseGenerator(new int[]{1,2,3,4,5,6,7,8});
	}

	public void LoadChunk(Chunk chunk) {
		int cx = chunk.Key.x;
		int cy = chunk.Key.y;
		int cz = chunk.Key.z;
		BlockType[,,] output = new BlockType[16,16,16];

		for (int x=0; x<16; x++) {
			for (int z=0; z<16; z++) {
				int wx = cx*16 + x;
				int wz = cz*16 + z;
				//float foo = Mathf.PerlinNoise(wx*0.1f, wz*0.1f);
				//foo = Mathf.Cos(foo*Mathf.PI)*0.5f + 0.5f;
				//foo *= foo;
				//foo *= 0.15f;
				float foo = Mathf.PerlinNoise(123 + wx*0.02f, 456 + wz*0.02f)*0.8f;
				foo *= 64;
				foo += 64;

				for (int y=0; y<16; y++) {
					int wy = cy*16 + y;
					if (cy*16 + y > foo) {
						// Air above ground level
						output[x,y,z] = BlockType.Empty;
					} else {
						float caves = _simplex.coherentNoise(wx,wy,wz);
						if (caves>0.05f) {
							// hole
							output[x,y,z] = BlockType.Empty;
						} else {
							// solid ground alright
							output[x,y,z] = new BlockType(1);
						}
					}
				}
			}
		}

		chunk.SetBlocks(output);
	}
}

