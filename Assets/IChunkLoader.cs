using System;

public interface IChunkLoader
{
	void LoadChunk(Vector3i key, BlockType[,,] output);
}
