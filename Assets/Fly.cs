using UnityEngine;
using System.Collections;

public class Fly : MonoBehaviour {

	public float panningSpeed = 180f;
	public float moveSpeed = 10f;

	private float _rx = 0f;
	private float _ry = 0f;

	void Start () {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
	
	void Update () {
		_rx += Input.GetAxis("Mouse X") * panningSpeed * Time.deltaTime;
		_ry += Input.GetAxis("Mouse Y") * panningSpeed * Time.deltaTime;
		_ry = Mathf.Clamp(_ry, -90, 90);

		transform.localRotation = Quaternion.AngleAxis(_rx, Vector3.up);
		transform.localRotation *= Quaternion.AngleAxis(_ry, Vector3.left);

		float effectiveMoveSpeed = moveSpeed;

		if (Input.GetKey(KeyCode.LeftShift)) {
			effectiveMoveSpeed *= 2.5f;
		}

		transform.position += transform.forward * effectiveMoveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
		transform.position += transform.right * effectiveMoveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;

		// TODO some key to toggle fly mode on/off ...
	}
}
