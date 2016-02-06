using System;

public class SunlightHeightmap
{
	private World _world;
	int[,] _heightmap;

	public SunlightHeightmap (World world)
	{
		_world = world;
		_heightmap = new int[_world.SizeX*16, _world.SizeZ*16];
		// TODO install some events to snoop world changes to keep sunlight tracking up to date
	}

	public int GetSunlightAt(int cx, int cz, int x, int z) {
		return GetSunlightAt(cx*16+x, cz*16+z);
	}

	public int GetSunlightAt(int wx, int wz) {
		return _heightmap[wx,wz];
	}

	public void CalculateAll() {
		for (int cx=0; cx<_world.SizeX; cx++) {
			for (int cz=0; cz<_world.SizeZ; cz++) {
				// get chunks in the column
				Chunk[] column = new Chunk[_world.SizeY];
				for (int cy=0; cy<_world.SizeY; cy++) {
					column[cy] = _world.GetChunk(new Vector3i(cx, cy, cz));
				}

				// walk through the air
				for (int x=0; x<16; x++) {
					for (int z=0; z<16; z++) {
						// beam down
						int sunlightHeight = _world.SizeY*16-1;
						while (sunlightHeight >= 0 && column[sunlightHeight/16].GetBlock(x, sunlightHeight%16, z).isTransparent()) {
							sunlightHeight--;
						}
						// first non-transparent block at sunlightHeight, or we crashed through the bottom
						// move up to the last transparent block
						sunlightHeight++;
						_heightmap[cx*16+x, cz*16+z] = sunlightHeight;
					}
				}
			}
		}
	}
}
