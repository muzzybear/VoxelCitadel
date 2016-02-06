using System;

public class Chunk
{
	private readonly BlockType[,,] _data = new BlockType[16,16,16];

	private bool _empty = true;
	public bool Empty { get { return _empty; }}

	private readonly Vector3i _key;
	/// Location of the chunk in chunk coordinates
	public Vector3i Key { get { return _key; } }

	private readonly World _world;
	public World Owner { get { return _world; } }

	public BlockType GetBlock(Vector3i localPosition) {
		return GetBlock(localPosition.x, localPosition.y, localPosition.z);
	}

	public BlockType GetBlock(int x, int y, int z) {
		if (x >= 0 && x < 16 &&	y >= 0 && y < 16 &&	z >= 0 && z < 16) {
			return _data[x, y, z];
		} else {
			return _world.GetBlock(_key*16 + new Vector3i(x,y,z));
		}
	}

	public void SetBlock(Vector3i localPosition, BlockType block) {
		SetBlock(localPosition.x, localPosition.y, localPosition.z, block);
	}

	public void SetBlock(int x, int y, int z, BlockType block) {
		if (x >= 0 && x < 16 &&	y >= 0 && y < 16 &&	z >= 0 && z < 16) {
			if (block != BlockType.Empty)
				_empty = false;
			
			_world.RWLock.AcquireWriterLock(System.Threading.Timeout.Infinite);
			_data[x, y, z] = block;
			_world.RWLock.ReleaseWriterLock();
			// TODO Think ... there better not be any race conditions because of this :P
			_world.ChunkModified(this);
		} else {
			_world.SetBlock(_key*16 + new Vector3i(x,y,z), block);
		}
	}

	public void LoadBlocks(IChunkLoader loader) {
		_world.RWLock.AcquireWriterLock(System.Threading.Timeout.Infinite);
		loader.LoadChunk(_key, _data);
		_world.RWLock.ReleaseWriterLock();
		_world.ChunkModified(this);

	}

	public bool IsBlockFaceVisible(int x, int y, int z, BlockFace face) {
		Vector3i normal = CubeBuilder.cubeNormals[(int)face];
		return GetBlock(x+normal.x, y+normal.y, z+normal.z).isTransparent();
	}

	public bool IsBlockFaceVisible(Vector3i position, BlockFace face) {		
		Vector3i adjacent = position + CubeBuilder.cubeNormals[(int)face];
		return GetBlock(adjacent).isTransparent();
	}

	public bool IsBlockVisible(Vector3i position) {
		return IsBlockVisible(position.x, position.y, position.z);
	}

	public bool IsBlockVisible(int x, int y, int z) {
		for(int i=0; i<6; i++)
			if (IsBlockFaceVisible(x,y,z, (BlockFace)i)) return true;
		return false;
	}

	internal Chunk (Vector3i key, World world)
	{
		_key = new Vector3i(key);
		_world = world;
	}
}
