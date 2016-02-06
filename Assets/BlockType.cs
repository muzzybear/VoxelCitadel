using System;

public struct BlockType
{
	public static readonly BlockType Empty = new BlockType(0);

	private ushort _raw;
	public ushort Raw { get { return _raw; } }

	public bool isTransparent() { return _raw==0; }

	public BlockType (ushort raw)
	{
		_raw = raw;
	}

	public override int GetHashCode()
	{
		return _raw;
	}

	public override bool Equals(object other)
	{
		if(!(other is BlockType))
		{
			return false;
		}
		BlockType b = (BlockType)other;
		return _raw == b._raw;
	}

	public override string ToString()
	{
		return string.Format("BlockType({0})", _raw);
	}

	public static bool operator ==(BlockType a, BlockType b)
	{
		return a._raw == b._raw;
	}

	public static bool operator !=(BlockType a, BlockType b)
	{
		return a._raw != b._raw;
	}
}
