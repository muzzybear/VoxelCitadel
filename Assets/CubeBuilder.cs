using UnityEngine;
//using System.Collections;

public static class CubeBuilder
{
	public static readonly Vector3i[] cubeNormals = {
		new Vector3i(+1, 0, 0),
		new Vector3i(-1, 0, 0),
		new Vector3i( 0,+1, 0),
		new Vector3i( 0,-1, 0),
		new Vector3i( 0, 0,+1),
		new Vector3i( 0, 0,-1),
	};

	private static readonly Vector3i[] cubeVertices = {
		new Vector3i( 0, 0, 0),
		new Vector3i( 1, 0, 0),
		new Vector3i( 0, 1, 0),
		new Vector3i( 1, 1, 0),
		new Vector3i( 0, 0, 1),
		new Vector3i( 1, 0, 1),
		new Vector3i( 0, 1, 1),
		new Vector3i( 1, 1, 1),
	};

	/* quad vertices diagram
	 * 
	 * 2-3
	 * | |
	 * 0-1
	 * 
	 * Triangles: {1,2,3}, {2,1,0}
	 * 
	 **/

	private static readonly Vector2[] UVs = {
		new Vector2(0, 0),
		new Vector2(1, 0),
		new Vector2(0, 1),
		new Vector2(1, 1),
	};

	private static readonly int[,] cubeFaces = {
		{1, 5, 3, 7},
		{4, 0, 6, 2},
		{2, 3, 6, 7},
		{4, 5, 0, 1},
		{5, 4, 7, 6},
		{0, 1, 2, 3},
	};

	public static void buildCube(MeshBuilder mb, Vector3i pos, Chunk chunk, ChunkLightmap lightmap) {
		// FIXME foo...
		Color brown = new Color(0.87f, 0.65f, 0.47f) * 0.75f;
		Color[] foo = {brown*0.8f, brown*0.8f, Color.green, brown*0.5f, brown, brown};

		for(int i=0; i<6; i++) {
			if (!chunk.IsBlockFaceVisible(pos, (BlockFace)i))
				continue;
				
			Vector3i normal = cubeNormals[i];

			int[] ao = new int[4];

			for(int v=0; v<4; v++) {
				Vector3i vertex = cubeVertices[cubeFaces[i,v]];
				/*
				if (i==0 || i==1) {
					// x
					ao[v] = lights[normal.x+1, vertex.y, vertex.z] +
						lights[normal.x+1, vertex.y+1, vertex.z] +
						lights[normal.x+1, vertex.y, vertex.z+1] +
						lights[normal.x+1, vertex.y+1, vertex.z+1];
				} else if (i==2 || i==3) {
					// y
					ao[v] = lights[vertex.x, normal.y+1, vertex.z] +
						lights[vertex.x+1, normal.y+1, vertex.z] +
						lights[vertex.x, normal.y+1, vertex.z+1] +
						lights[vertex.x+1, normal.y+1, vertex.z+1];
				} else {
					// z
					ao[v] = lights[vertex.x, vertex.y, normal.z+1] +
						lights[vertex.x+1, vertex.y, normal.z+1] +
						lights[vertex.x, vertex.y+1, normal.z+1] +
						lights[vertex.x+1, vertex.y+1, normal.z+1];
				}
				*/

				// for sake of simplicity, get diagonal light value too even though it could be blocked by the two adjacent blocks
				if (i==0 || i==1) {
					// x
					ao[v] = lightmap.GetLight(pos + new Vector3i(-1+normal.x+1, -1+vertex.y, -1+vertex.z)) +
						lightmap.GetLight(pos + new Vector3i(-1+normal.x+1, -1+vertex.y+1, -1+vertex.z)) +
						lightmap.GetLight(pos + new Vector3i(-1+normal.x+1, -1+vertex.y, -1+vertex.z+1)) +
						lightmap.GetLight(pos + new Vector3i(-1+normal.x+1, -1+vertex.y+1, -1+vertex.z+1));
				} else if (i==2 || i==3) {
					// y
					ao[v] = lightmap.GetLight(pos + new Vector3i(-1+vertex.x, -1+normal.y+1, -1+vertex.z)) +
						lightmap.GetLight(pos + new Vector3i(-1+vertex.x+1, -1+normal.y+1, -1+vertex.z)) +
						lightmap.GetLight(pos + new Vector3i(-1+vertex.x, -1+normal.y+1, -1+vertex.z+1)) +
						lightmap.GetLight(pos + new Vector3i(-1+vertex.x+1, -1+normal.y+1, -1+vertex.z+1));
				} else {
					// z
					ao[v] = lightmap.GetLight(pos + new Vector3i(-1+vertex.x, -1+vertex.y, -1+normal.z+1)) +
						lightmap.GetLight(pos + new Vector3i(-1+vertex.x+1, -1+vertex.y, -1+normal.z+1)) +
						lightmap.GetLight(pos + new Vector3i(-1+vertex.x, -1+vertex.y+1, -1+normal.z+1)) +
						lightmap.GetLight(pos + new Vector3i(-1+vertex.x+1, -1+vertex.y+1, -1+normal.z+1));
				}
				vertex += pos;

				mb.Vertices.Add((Vector3)vertex);
				mb.Normals.Add((Vector3)normal);
				mb.UVs.Add(UVs[v]);
				// ...

				float aoFactor = Mathf.Clamp(ao[v], 0, 4*15);
				aoFactor /= 4*15;

				float ambient = 0.15f;

				Color color = foo[i] * (ambient + aoFactor*(1f-ambient));

				mb.Colors32.Add(color);
			}
			// prefer the bright edge in the middle, suitable for outdoors but bad for drastic light level transitions into darkness
			if (ao[0] + ao[3] < ao[1] + ao[2]) {
				mb.AddTriangle(-2, -3, -4);
				mb.AddTriangle(-3, -2, -1);
			} else {
				mb.AddTriangle(-2, -1, -4);
				mb.AddTriangle(-1, -3, -4);
			}
		}
	}
}
