using System;
using System.Collections.Generic;
using System.Threading;

// TODO should probably move this out for sake of cleaniness
using UnityEngine;

public class World
{
	// we aren't going to have infinite world...
	private readonly int _sizeX, _sizeY, _sizeZ;

	// sizes returned are in chunks, not blocks
	public int SizeX { get { return _sizeX; }}
	public int SizeY { get { return _sizeY; }}
	public int SizeZ { get { return _sizeZ; }}

	private Dictionary<Vector3i, Chunk> _chunks = new Dictionary<Vector3i, Chunk>();
	public Dictionary<Vector3i, Chunk> Chunks { get { return _chunks; } }

	private Dictionary<Vector3i, ChunkLightmap> _lightmaps = new Dictionary<Vector3i, ChunkLightmap>();
	public Dictionary<Vector3i, ChunkLightmap> Lightmaps { get { return _lightmaps; } }

	private SunlightHeightmap _sunlight;
	public SunlightHeightmap Sunlight { get { return _sunlight; } }

	// counter to support nested batches
	private int _batchMode = 0;
	private HashSet<Chunk> _modifiedChunks = new HashSet<Chunk>();

	private ReaderWriterLock _rwlock = new ReaderWriterLock();
	public ReaderWriterLock RWLock { get { return _rwlock; }}

	public World (int chunksX, int chunksY, int chunksZ)
	{
		_sizeX = chunksX;
		_sizeY = chunksY;
		_sizeZ = chunksZ;
		_sunlight = new SunlightHeightmap(this);
	}


	public Chunk CreateChunk(Vector3i key, IChunkLoader loader = null) {
		// TODO check that chunk doesn't exist yet
		// TODO check that chunk is within given size bounds
		var chunk = new Chunk(key, this);
		_chunks.Add(chunk.Key, chunk);

		if (loader != null) {
			BeginBatchUpdate();
			chunk.LoadBlocks(loader);
			EndBatchUpdate();
		}

		return chunk;
	}

	public Chunk GetChunk(Vector3i key) {
		Chunk chunk;
		_chunks.TryGetValue(key, out chunk);
		return chunk;
	}

	/// Used to suspend lightmap updates etc
	public void BeginBatchUpdate() {
		_rwlock.AcquireWriterLock(Timeout.Infinite);
		_batchMode++;
	}

	public void EndBatchUpdate() {
		if (_batchMode == 0)
			throw new System.InvalidOperationException("EndBatchUpdate called without matching BeginBatchUpdate");
		_batchMode--;
		bool DoneWithBatch = _batchMode == 0;
		_rwlock.ReleaseWriterLock();

		if (DoneWithBatch) {
			// fire queued modification events
			foreach (Chunk chunk in _modifiedChunks) {
				// TODO fire event
			}
		}
	}
		
	public void ChunkModified(Chunk chunk) {
		if (_batchMode == 0) {
			// TODO fire modification events
		} else {
			// set chunk dirty to queue modification event
			_modifiedChunks.Add(chunk);
		}
	}

	public void RecalculateLightsAt(Chunk chunk, Vector3i localposition) {
		if (_batchMode > 0) {
			// TODO flag chunk for update
		} else {
			// TODO recalculate single cell

		}
	}

	public Chunk GetChunkAt(Vector3i worldPosition) {
		if (worldPosition.x < 0 || worldPosition.y < 0 || worldPosition.z < 0)
			return null;
		Chunk tmp;
		Vector3i key = worldPosition / 16;
		_chunks.TryGetValue(key, out tmp);
		// might return null
		return tmp;
	}

	public ChunkLightmap GetLightmap(Vector3i key) {
		ChunkLightmap lightmap;
		_lightmaps.TryGetValue(key, out lightmap);
		if (lightmap != null)
			return lightmap;

		// no lightmap found? use the chunk lookup which will create it
		Chunk chunk;
		_chunks.TryGetValue(key, out chunk);
		// FIXME add error check
		return GetLightmapFor(chunk);
	}

	public ChunkLightmap GetLightmapFor(Chunk chunk) {
		ChunkLightmap lightmap;
		if (!_lightmaps.TryGetValue(chunk.Key, out lightmap)) {
			lightmap = new ChunkLightmap(chunk);
			_lightmaps.Add(chunk.Key, lightmap);
		}
		return lightmap;
	}

	public ushort GetLight(Vector3i worldPosition) {
		// FIXME basically, this shouldn't be used because repeated lookups are bad, but it's easier to have for prototyping
		Chunk chunk = GetChunkAt(worldPosition);
		// TODO refactor - returning darkness for out of bounds
		if (chunk == null)
			return 0;
		ChunkLightmap lightmap = GetLightmapFor(chunk);
		Vector3i localpos = worldPosition % 16;
		return lightmap.GetLight(localpos);
	}

	public BlockType GetBlock(Vector3i worldPosition) {
		Chunk chunk = GetChunkAt(worldPosition);
		if (chunk == null) {
			return BlockType.Empty;
		}
		Vector3i localpos = worldPosition % 16;
		return chunk.GetBlock(localpos);
	}

	public void SetBlock(Vector3i worldPosition, BlockType block) {
		Chunk chunk = GetChunkAt(worldPosition);
		if (chunk == null)
			throw new System.ArgumentException("given coordinates don't yield a chunk");
		Vector3i localpos = worldPosition % 16;
		chunk.SetBlock(localpos, block);
	}

    private float intbound(float s, float ds) {
        // Find the smallest positive t such that s+t*ds is an integer.
        if (ds < 0) {
            return intbound(-s, -ds);
        } else {
            s = s % 1.0f;
            // problem is now s+t*ds = 1
            return (1-s)/ds;
        }
    }

    public bool Raycast(UnityEngine.Ray ray) {
        var bounds = new UnityEngine.Bounds();
        bounds.SetMinMax(new UnityEngine.Vector3(0,0,0), new UnityEngine.Vector3(_sizeX*16, _sizeY*16, _sizeZ*16));

        // if we're outside world bounds, first intersect the world's bounding box
        if (! bounds.Contains(ray.origin)) {
            float dist;
            if (!bounds.IntersectRay(ray, out dist))
                return false;
            ray.origin += ray.direction*dist;
        }
        // inside or at world edge, time to voxel trace

        // From "A Fast Voxel Traversal Algorithm for Ray Tracing"
        // by John Amanatides and Andrew Woo, 1987
        // <http://www.cse.yorku.ca/~amana/research/grid.pdf>
        // <http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.42.3443>

        // initialize to initial voxel coordinate
        // TODO verify that this works reliable even if we started outside the world bounds
        int x = Mathf.FloorToInt(ray.origin.x);
        int y = Mathf.FloorToInt(ray.origin.y);
        int z = Mathf.FloorToInt(ray.origin.z);

        // TODO determine initial distances to voxel edges
        float tMaxX = intbound(ray.origin.x, ray.direction.x);
        float tMaxY = intbound(ray.origin.y, ray.direction.y);
        float tMaxZ = intbound(ray.origin.z, ray.direction.z);

        // determine stepping direction
        int stepX = (int) Mathf.Sign(ray.direction.x);
        int stepY = (int) Mathf.Sign(ray.direction.y);
        int stepZ = (int) Mathf.Sign(ray.direction.z);

        // determine distance (in ray units) between voxel edges
        float tDeltaX = stepX/ray.direction.x;
        float tDeltaY = stepY/ray.direction.y;
        float tDeltaZ = stepZ/ray.direction.z;

        int wx = _sizeX*16;
        int wy = _sizeY*16;
        int wz = _sizeZ*16;

        while (
            (stepX > 0 ? x < wx : x >= 0) &&
            (stepY > 0 ? y < wy : y >= 0) &&
            (stepZ > 0 ? z < wz : z >= 0)) {

            // lalala
            // TODO WE NOW HAVE A VOXEL AT X,Y,Z

            // walk...
            if (tMaxX < tMaxY) {
                if (tMaxX < tMaxZ) {
                    x += stepX;
                    tMaxX += tDeltaX;
                } else {
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                }
            } else {
                if (tMaxY < tMaxZ) {
                    y += stepY;
                    tMaxY += tDeltaY;
                } else {
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                }
            }
          
        }


        return false;
    }
}
