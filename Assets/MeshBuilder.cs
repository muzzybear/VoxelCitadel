using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshBuilder
{
	private List<Vector3> _vertices = new List<Vector3>();
	public List<Vector3> Vertices { get { return _vertices; } }

	private List<Vector3> _normals = new List<Vector3>();
	public List<Vector3> Normals { get { return _normals; } }

	private List<Vector2> _uvs = new List<Vector2>();
	public List<Vector2> UVs { get { return _uvs; } }

	private List<Color32> _colors32 = new List<Color32>();
	public List<Color32> Colors32 { get { return _colors32; } }

	private List<int> _indices = new List<int>();

	public void Clear() {
		_vertices.Clear();
		_normals.Clear();
		_uvs.Clear();
		_colors32.Clear();
		_indices.Clear();
	}

	private void AddIndex(int index) {
		_indices.Add(index >= 0 ? index : _vertices.Count+index);
	}

	public void AddTriangle(int i0, int i1, int i2) {
		AddIndex(i0);
		AddIndex(i1);
		AddIndex(i2);
	}

	public void BuildMesh(Mesh mesh) {
		if (mesh == null)
			throw new System.ArgumentNullException("mesh");

		mesh.vertices = _vertices.ToArray();
		mesh.triangles = _indices.ToArray();

		if (_normals.Count > 0) {
			if (_normals.Count == _vertices.Count)
				mesh.normals = _normals.ToArray();
			else
				throw new System.InvalidOperationException("Normals count does not match vertices count");
		}

		if (_uvs.Count > 0) {
			if (_uvs.Count == _vertices.Count)
				mesh.uv = _uvs.ToArray();
			else
				throw new System.InvalidOperationException("UVs count does not match vertices count");
		}

		if (_colors32.Count > 0) {
			if (_colors32.Count == _vertices.Count)
				mesh.colors32 = _colors32.ToArray();
			else
				throw new System.InvalidOperationException("Colors count does not match vertices count");
		}

		mesh.RecalculateBounds();
	}
}

