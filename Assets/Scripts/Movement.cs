using System;
using UnityEngine;
using System.Collections.Generic;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public static class GravitySystem
{
	public static Vector3 GravityDir = Vector3.down;
	public static float GravityStrength;
	public static Vector3 Gravity => GravityDir * GravityStrength;
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Movement : MonoBehaviour
{
	public bool isColliding;
	[Space]
	public float maxRollVelocity = 15f;
	public float angularAcceleration = 75f;
	public float brakingAcceleration = 30f;
	public float airAcceleration = 5f;
	public float gravity = 20f;
	public float staticFriction = 1.1f;
	public float kineticFriction = 0.7f;
	public float bounceKineticFriction = 0.2f;
	public float maxDotSlide = 0.5f;
	public float jumpImpulse = 7.5f;
	public float maxForceRadius = 50f;
	public float minBounceVel = 0.1f;
	public float bounceRestitution = 0.5f;
	public float bounce = 0;

	private float remainingTime = 0.0f;

	public Vector3 marbleVelocity;
	public Vector3 marbleAngularVelocity;
	public Texture collidedTexture;

	private float marbleRadius =>
		sphereCollider.radius * Mathf.Max(
			transform.lossyScale.x,
			transform.lossyScale.y,
			transform.lossyScale.z
		);

	private Vector2 inputMovement => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) + _fakeInput;
	private Vector2 _fakeInput = Vector2.zero;

	private bool Jump => Input.GetButton("Jump");

	private Vector3 forwards = Vector3.forward;

	private bool bounceYet;
	private float bounceSpeed;
	private Vector3 bouncePos;
	private Vector3 bounceNormal;
	private float slipAmount;
	private float contactTime;
	private float rollVolume;

	private Vector3 surfaceVelocity;

	[SerializeField] private List<MeshCollider> colTests;

	class MeshData
	{
		public MeshCollider collider;
		public Mesh mesh;

		public Vector3[] localVertices;
		public int[] triangles;

		public Matrix4x4 localToWorld;
		public Matrix4x4 worldToLocal;

		public Vector3 lastPosition;
		public Quaternion lastRotation;
		public Vector3 lastScale; // NEW
	}


	private List<MeshData> meshes;

	private Rigidbody rigidBody;
	private SphereCollider sphereCollider;
	private int collisions;
	private float lastJump;
	private Vector3 lastNormal;

	class CollisionInfo
	{
		public Vector3 point;
		public Vector3 normal;
		public Vector3 velocity;
		public Collider collider;
		public float friction;
		public float restitution;
		public float penetration;
	}

	void Start()
	{
		rigidBody = gameObject.GetComponent<Rigidbody>();
		rigidBody.maxAngularVelocity = Mathf.Infinity;

		sphereCollider = GetComponent<SphereCollider>();

		GravitySystem.GravityStrength = gravity;

		meshes = new List<MeshData>();
		colTests = new List<MeshCollider>();

	}

	public void GenerateMeshData()
	{
		foreach (var item in FindObjectsOfType<MeshCollider>())
			colTests.Add(item);

		foreach (var mesh in colTests)
			GenerateMeshInfo(mesh);
	}

	void GenerateMeshInfo(MeshCollider mc)
	{
		Mesh m = mc.sharedMesh;

		meshes.Add(new MeshData
		{
			collider = mc,
			mesh = m,
			localVertices = m.vertices,
			triangles = m.triangles,
			localToWorld = mc.transform.localToWorldMatrix,
			worldToLocal = mc.transform.worldToLocalMatrix,
			lastPosition = mc.transform.position,
			lastRotation = mc.transform.rotation,
			lastScale = mc.transform.lossyScale // NEW
		});
	}


	public void AddMesh(MeshCollider _meshCollider)
	{
		colTests.Add(_meshCollider);
		GenerateMeshInfo(_meshCollider);
	}

	void FixedUpdate()
	{
		float _dt = Time.fixedDeltaTime;
		remainingTime += _dt;

		// Always subdivide into 0.005s slices (200 Hz physics)
		const float STEP_SIZE = 0.005f;

		while (remainingTime >= STEP_SIZE)
		{
			float _loopTime = STEP_SIZE;
			AdvancePhysics(ref _loopTime);
			remainingTime -= _loopTime;
		}
	}

	private void AdvancePhysics(ref float _dt)
	{
		Vector3 _pos = transform.position;
		Quaternion _rot = transform.rotation;
		Vector3 _velocity = marbleVelocity;
		Vector3 _omega = marbleAngularVelocity;

		bool usedSweep = false;
		List<CollisionInfo> _contacts = null;

		// ============================================================
		// 1. EARLY SWEEP (converted to flagged path)
		// ============================================================
		float _travelDist = _velocity.magnitude * _dt;
		if (_travelDist > marbleRadius)
		{
			if (Physics.SphereCast(_pos, marbleRadius, _velocity.normalized, out var _hit, _travelDist))
			{
				float _travelTime = _hit.distance / _velocity.magnitude;
				_dt = Mathf.Min(_dt, _travelTime);

				var _contact = new CollisionInfo
				{
					normal = _hit.normal,
					point = _hit.point,
					penetration = 0f,
					restitution = IsFloor(_hit.normal)
						? (_hit.collider.sharedMaterial?.bounciness ?? 0.5f)
						: 0f,
					friction = _hit.collider.sharedMaterial?.dynamicFriction ?? 0.5f,
					velocity = _hit.collider.attachedRigidbody
						? _hit.collider.attachedRigidbody.GetPointVelocity(_hit.point)
						: Vector3.zero,
					collider = _hit.collider
				};

				_contacts = new List<CollisionInfo> { _contact };
				usedSweep = true;
			}
		}

		// ============================================================
		// 2. CONTACT GENERATION (only if no sweep)
		// ============================================================
		if (!usedSweep)
		{
			_contacts = new List<CollisionInfo>();
			float _radius = marbleRadius + 0.0001f;

			for (int _index = 0; _index < colTests.Count; _index++)
			{
				MeshData _mesh = meshes[_index];
				MeshCollider _meshCollider = _mesh.collider;

				if (_mesh.mesh == null || !_meshCollider.enabled)
					continue;

				UpdateMeshTransform(_mesh);

				Vector3 _localPos = _pos;
				int _length = _mesh.triangles.Length;

				for (int _i = 0; _i < _length; _i += 3)
				{
					Vector3 _p0 = _mesh.localToWorld.MultiplyPoint3x4(
						_mesh.localVertices[_mesh.triangles[_i]]
					);
					Vector3 _p1 = _mesh.localToWorld.MultiplyPoint3x4(
						_mesh.localVertices[_mesh.triangles[_i + 1]]
					);
					Vector3 _p2 = _mesh.localToWorld.MultiplyPoint3x4(
						_mesh.localVertices[_mesh.triangles[_i + 2]]
					);

					Vector3 _normal = Vector3.Cross(_p1 - _p0, _p2 - _p0).normalized;
					Vector3 _closest = ClosestPointOnTriangle(_localPos, _p0, _p1, _p2);
					Vector3 _diff = _localPos - _closest;

					if (_diff.sqrMagnitude <= _radius * _radius &&
						Vector3.Dot(_diff, _normal) >= 0.0f)
					{
						float _penetration = _radius - _diff.magnitude;
						if (_penetration > 0)
							AddContact(_meshCollider, _closest, _diff.normalized,
								_penetration, _contacts, _mesh);
					}

					TestEdge(_localPos, _p0, _p1, _radius, _meshCollider, _contacts, _mesh);
					TestEdge(_localPos, _p1, _p2, _radius, _meshCollider, _contacts, _mesh);
					TestEdge(_localPos, _p2, _p0, _radius, _meshCollider, _contacts, _mesh);

					TestVertex(_localPos, _p0, _radius, _meshCollider, _contacts, _mesh);
					TestVertex(_localPos, _p1, _radius, _meshCollider, _contacts, _mesh);
					TestVertex(_localPos, _p2, _radius, _meshCollider, _contacts, _mesh);
				}
			}

			isColliding = _contacts.Count > 0;

			// ============================================================
			// 3. CONTACT FILTERING
			// ============================================================
			if (_contacts.Count > 1)
			{
				CollisionInfo _deepest = _contacts[0];
				for (int i = 1; i < _contacts.Count; i++)
					if (_contacts[i].penetration > _deepest.penetration)
						_deepest = _contacts[i];

				_contacts.Clear();
				_contacts.Add(_deepest);
			}

			if (_contacts.Count > 0)
			{
				CollisionInfo best = _contacts[0];
				float bestDot = Vector3.Dot(best.normal, -GravitySystem.Gravity.normalized);

				for (int i = 1; i < _contacts.Count; i++)
				{
					float d = Vector3.Dot(_contacts[i].normal,
						-GravitySystem.Gravity.normalized);
					if (d > bestDot)
					{
						bestDot = d;
						best = _contacts[i];
					}
				}

				_contacts.Clear();
				_contacts.Add(best);
			}
		}

		// ============================================================
		// 4. RESOLUTION + INTEGRATION (single exit path)
		// ============================================================
		UpdateMove(ref _dt, ref _velocity, ref _omega, _contacts);
		UpdateIntegration(_dt, ref _pos, ref _rot, _velocity, _omega);

		transform.position = _pos;
		transform.rotation = _rot;
		marbleVelocity = _velocity;
		marbleAngularVelocity = _omega;
	}


	private bool IsFloor(Vector3 n)
	{
		return Vector3.Dot(n, Vector3.up) > 0.7f;
	}

	void UpdateMeshTransform(MeshData data)
	{
		Transform t = data.collider.transform;

		if (t.position != data.lastPosition ||
			t.rotation != data.lastRotation ||
			t.lossyScale != data.lastScale) // NEW
		{
			data.localToWorld = t.localToWorldMatrix;
			data.worldToLocal = t.worldToLocalMatrix;

			data.lastPosition = t.position;
			data.lastRotation = t.rotation;
			data.lastScale = t.lossyScale; // NEW
		}
	}

	// Utility: Closest point on triangle (barycentric method)
	private Vector3 ClosestPointOnTriangle(Vector3 _p, Vector3 _a, Vector3 _b, Vector3 _c)
	{
		Vector3 _ab = _b - _a;
		Vector3 _ac = _c - _a;
		Vector3 _ap = _p - _a;

		float _d1 = Vector3.Dot(_ab, _ap);
		float _d2 = Vector3.Dot(_ac, _ap);
		if (_d1 <= 0f && _d2 <= 0f) return _a;

		Vector3 _bp = _p - _b;
		float _d3 = Vector3.Dot(_ab, _bp);
		float _d4 = Vector3.Dot(_ac, _bp);
		if (_d3 >= 0f && _d4 <= _d3) return _b;

		float _vc = _d1 * _d4 - _d3 * _d2;
		if (_vc <= 0f && _d1 >= 0f && _d3 <= 0f)
		{
			float _v = _d1 / (_d1 - _d3);
			return _a + _v * _ab;
		}

		Vector3 _cp = _p - _c;
		float _d5 = Vector3.Dot(_ab, _cp);
		float _d6 = Vector3.Dot(_ac, _cp);
		if (_d6 >= 0f && _d5 <= _d6) return _c;

		float _vb = _d5 * _d2 - _d1 * _d6;
		if (_vb <= 0f && _d2 >= 0f && _d6 <= 0f)
		{
			float _w = _d2 / (_d2 - _d6);
			return _a + _w * _ac;
		}

		float _va = _d3 * _d6 - _d5 * _d4;
		if (_va <= 0f && (_d4 - _d3) >= 0f && (_d5 - _d6) >= 0f)
		{
			float _w = (_d4 - _d3) / ((_d4 - _d3) + (_d5 - _d6));
			return _b + _w * (_c - _b);
		}

		float _denom = 1f / (_va + _vb + _vc);
		float _v2 = _vb * _denom;
		float _w2 = _vc * _denom;
		return _a + _ab * _v2 + _ac * _w2;
	}

	// Edge test
	void TestEdge(
		Vector3 _center,
		Vector3 _a,
		Vector3 _b,
		float _radius,
		Collider _col,
		List<CollisionInfo> _contacts,
		MeshData _mesh
	)
	{
		Vector3 _ab = _b - _a;
		float _t = Mathf.Clamp01(Vector3.Dot(_center - _a, _ab) / _ab.sqrMagnitude);
		Vector3 _closest = _a + _t * _ab;
		Vector3 _diff = _center - _closest;

		if (_diff.sqrMagnitude <= _radius * _radius)
		{
			float _penetration = _radius - _diff.magnitude;
			if (_penetration > 0)
				AddContact(_col, _closest, _diff.normalized, _penetration, _contacts, _mesh);
		}
	}


	// Vertex test
	// Vertex test
	void TestVertex(
		Vector3 _center,
		Vector3 _v,
		float _radius,
		Collider _col,
		List<CollisionInfo> _contacts,
		MeshData _mesh
	)
	{
		Vector3 _diff = _center - _v;
		if (_diff.sqrMagnitude <= _radius * _radius)
		{
			float _penetration = _radius - _diff.magnitude;
			if (_penetration > 0)
				AddContact(_col, _v, _diff.normalized, _penetration, _contacts, _mesh);
		}
	}


	// Contact builder
	void AddContact(
	Collider _col,
	Vector3 point,
	Vector3 normal,
	float penetration,
	List<CollisionInfo> contacts,
	MeshData mesh)
	{
		Vector3 colliderVelocity = Vector3.zero;

		if (_col.attachedRigidbody != null)
		{
			colliderVelocity = _col.attachedRigidbody.GetPointVelocity(point);
		}
		else if (mesh != null)
		{
			colliderVelocity =
				(mesh.collider.transform.position - mesh.lastPosition)
				/ Time.fixedDeltaTime;
		}

		contacts.Add(new CollisionInfo
		{
			point = point,
			normal = normal.normalized,
			penetration = penetration,
			restitution = _col.sharedMaterial?.bounciness ?? 0.5f,
			friction = _col.sharedMaterial?.dynamicFriction ?? 0.5f,
			velocity = colliderVelocity
		});
	}


	void ApplyCollisionState(List<CollisionInfo> _contacts)
	{
		if (_contacts.Count == 0)
		{
			collidedTexture = null;
			lastNormal = Vector3.up;
			return;
		}

		// Average normals
		Vector3 _avgNormal = Vector3.zero;
		foreach (var c in _contacts) _avgNormal += c.normal;
		_avgNormal.Normalize();

		lastNormal = _avgNormal;

		// Project velocity onto surface tangent
		marbleVelocity -= Vector3.Dot(marbleVelocity, _avgNormal) * _avgNormal;

		// Skin offset: lift slightly above surface
		transform.position += _avgNormal * 0.01f;
	}

	void UpdateIntegration(float _dt, ref Vector3 _pos, ref Quaternion _rot, Vector3 _vel, Vector3 _avel)
	{
		_pos += _vel * _dt;
		Vector3 vector3 = _avel;
		float num1 = vector3.magnitude;
		if (num1 <= 0.0000001f)
			return;

		Quaternion quaternion = Quaternion.AngleAxis(_dt * num1 * Mathf.Rad2Deg, vector3 * (1f / num1));
		quaternion.Normalize();
		_rot = quaternion * _rot;
		_rot.Normalize();
	}

	void UpdateMove(ref float _dt, ref Vector3 _velocity, ref Vector3 _angVelocity, List<CollisionInfo> _contacts)
	{
		surfaceVelocity = Vector3.zero;
		if (_contacts.Count > 0)
			surfaceVelocity = _contacts[0].velocity;

		// Convert world → relative
		_velocity -= surfaceVelocity;

		// Compute player input forces
		bool _isMoving = ComputeMoveForces(_angVelocity, out var _torque, out var _targetAngVel);

		// First pass: cancel velocity with bounce enabled
		VelocityCancel(_contacts, ref _velocity, ref _angVelocity, !_isMoving, false);

		// External forces (gravity, air control)
		Vector3 _externalForces = GetExternalForces(_dt, _contacts);

		// Apply contact forces (friction, jump, bounce)
		ApplyContactForces(
			_dt,
			_contacts,
			!_isMoving,
			_torque,
			_targetAngVel,
			ref _velocity,
			ref _angVelocity,
			ref _externalForces,
			out var _angAccel);

		// Integrate forces
		_velocity += _externalForces * _dt;
		_angVelocity += _angAccel * _dt;

		// Second pass: cancel velocity with bounce disabled
		VelocityCancel(_contacts, ref _velocity, ref _angVelocity, !_isMoving, true);

		// Contact time adjustment
		float _contactTime = _dt;

		if (_dt * 0.99f > _contactTime)
		{
			_velocity -= _externalForces * (_dt - _contactTime);
			_angVelocity -= _angAccel * (_dt - _contactTime);
			_dt = _contactTime;
		}

		// Extend contact time if contacts exist
		if (_contacts.Count != 0)
			_contactTime += _dt;

		_velocity += surfaceVelocity;
	}


	bool ComputeMoveForces(Vector3 _angVelocity, out Vector3 _torque, out Vector3 _targetAngVel)
	{
		_torque = Vector3.zero;
		_targetAngVel = Vector3.zero;

		// Relative gravity vector from marble center
		Vector3 _relGravity = -GravitySystem.Gravity.normalized * marbleRadius;

		// Velocity at the top of the sphere
		Vector3 _topVelocity = Vector3.Cross(_angVelocity, _relGravity);

		// Get camera-relative axes
		GetMarbleAxis(out var sideDir, out var motionDir, out Vector3 _);

		// Project top velocity onto those axes
		float _topY = Vector3.Dot(_topVelocity, motionDir);
		float _topX = Vector3.Dot(_topVelocity, sideDir);

		// Input movement (camera-relative now)
		Vector2 _move = inputMovement;
		float _moveY = maxRollVelocity * _move.y;
		float _moveX = maxRollVelocity * _move.x;

		// If no input, bail out
		if (Math.Abs(_moveY) < 0.001f && Math.Abs(_moveX) < 0.001f)
			return false;

		// Clamp input so you don’t overshoot
		if (_topY > _moveY && _moveY > 0.0f) _moveY = _topY;
		else if (_topY < _moveY && _moveY < 0.0f) _moveY = _topY;

		if (_topX > _moveX && _moveX > 0.0f) _moveX = _topX;
		else if (_topX < _moveX && _moveX < 0.0f) _moveX = _topX;

		// Desired angular velocity based on input
		_targetAngVel = Vector3.Cross(_relGravity, _moveY * motionDir + _moveX * sideDir) / _relGravity.sqrMagnitude;

		// Torque is difference between desired and current angular velocity
		_torque = _targetAngVel - _angVelocity;

		// Clamp torque to angularAcceleration
		float _targetAngAccel = _torque.magnitude;
		if (_targetAngAccel > angularAcceleration)
			_torque *= angularAcceleration / _targetAngAccel;

		return true;
	}


	void GetMarbleAxis(out Vector3 _sideDir, out Vector3 _motionDir, out Vector3 _upDir)
	{
		// Up direction is opposite of gravity
		_upDir = -GravitySystem.Gravity.normalized;

		// Use the camera's transform to get forward/right relative to the view
		Vector3 _camForward = Camera.main.transform.forward;
		Vector3 _camRight = Camera.main.transform.right;

		// Project onto the plane defined by upDir so movement stays on the ground
		_camForward = Vector3.ProjectOnPlane(_camForward, _upDir).normalized;
		_camRight = Vector3.ProjectOnPlane(_camRight, _upDir).normalized;

		// Assign motion and side directions
		_motionDir = _camForward;
		_sideDir = _camRight;
	}

	private Vector3 GetExternalForces(float _dt, List<CollisionInfo> _contacts)
	{
		Vector3 _force = GravitySystem.Gravity.normalized * gravity;
		if (_contacts.Count == 0)
		{
			GetMarbleAxis(out var _sideDir, out var _motionDir, out Vector3 _);
			_force += (_sideDir * inputMovement.x + _motionDir * inputMovement.y) * airAcceleration;
		}

		return _force;
	}

	void VelocityCancel(
	List<CollisionInfo> _contacts,
	ref Vector3 _velocity,   // RELATIVE velocity
	ref Vector3 _omega,
	bool _surfaceSlide,
	bool _noBounce)
	{
		bool firstPass = true;
		Vector3 up = -GravitySystem.Gravity.normalized;

		for (int iter = 0; iter < 6; iter++)
		{
			bool anyChange = false;

			foreach (var c in _contacts)
			{
				Vector3 n = c.normal;

				// Relative normal velocity
				float normalRelVel = Vector3.Dot(_velocity, n);

				// World normal velocity (important for MPs)
				float worldNormalVel = Vector3.Dot(_velocity + c.velocity, n);

				// --- DECIDE IF THIS CONTACT SHOULD BOUNCE ---
				bool shouldBounce =
					!_noBounce &&
					(
						normalRelVel < -minBounceVel ||   // real impact
						Mathf.Abs(worldNormalVel) > 3.0f  // MP-relative impact
					);

				// --- RESTING GROUND LOCK (ONLY IF NOT BOUNCING) ---
				if (!shouldBounce &&
					Vector3.Dot(n, up) > 0.7f &&
					normalRelVel < 0f)
				{
					// cancel tiny downward velocity
					_velocity -= n * normalRelVel;
					anyChange = true;
					continue;
				}

				// --- BOUNCE ---
				if (shouldBounce && normalRelVel < 0f)
				{
					float restitution = bounceRestitution * c.restitution;
					float bounceImpulse = -(1f + restitution) * normalRelVel;

					_velocity += n * bounceImpulse;
					anyChange = true;

					// --- Angular impulse from tangential velocity ---
					Vector3 velAtContact =
						_velocity + Vector3.Cross(_omega, -n * marbleRadius);

					Vector3 tangentVel =
						velAtContact - n * Vector3.Dot(velAtContact, n);

					float tangentMag = tangentVel.magnitude;

					if (tangentMag > 0.001f)
					{
						float penetrationSpeed = -normalRelVel;

						float inertia =
							(5f * bounceKineticFriction * c.friction * penetrationSpeed) /
							(2f * marbleRadius);

						inertia = Mathf.Min(inertia, tangentMag / marbleRadius);

						Vector3 tangentDir = tangentVel / tangentMag;
						Vector3 angularImpulse =
							inertia * Vector3.Cross(-n, -tangentDir);

						_omega += angularImpulse;
						_velocity -= Vector3.Cross(-angularImpulse, -n * marbleRadius);
					}
				}
			}

			if (!anyChange && !firstPass)
				break;

			firstPass = false;
		}
	}



	void ApplyContactForces(
		float _dt,
		List<CollisionInfo> _contacts,
		bool _isCentered,
		Vector3 _aControl,
		Vector3 _desiredOmega,
		ref Vector3 _velocity,
		ref Vector3 _omega,
		ref Vector3 _linAccel,
		out Vector3 _angAccel)
	{
		_angAccel = Vector3.zero;
		slipAmount = 0.0f;
		Vector3 _vector31 = GravitySystem.Gravity.normalized;
		int _index1 = -1;
		float _num1 = 0.0f;

		// Pick strongest contact
		for (int i = 0; i < _contacts.Count; ++i)
		{
			float _num2 = -Vector3.Dot(_contacts[i].normal, _linAccel);
			if (_num2 > _num1)
			{
				_num1 = _num2;
				_index1 = i;
			}
		}

		// Jump impulse isolated
        if (_index1 != -1 && Jump && bounce <= 0)
        {
            CollisionInfo c = _contacts[_index1];

            Vector3 n = c.normal.normalized;

            // Only allow jumping on mostly ground-like surfaces
            float upDot = Vector3.Dot(n, -GravitySystem.Gravity.normalized);
            if (upDot > 0.5f)
            {
                // Remove platform velocity along the contact normal
                float platformNormalVel = Vector3.Dot(surfaceVelocity, n);
                _velocity -= n * platformNormalVel;

                // Remove any existing velocity along the normal
                float vN = Vector3.Dot(_velocity, n);
                _velocity -= n * vN;

                // Apply jump impulse strictly along the normal
                _velocity += n * jumpImpulse;

                GameManager.instance.PlayJumpAudio();

                // Skip friction resolution this frame
                return;
            }
        }

        // Bounce impulse (optional)
        if (_index1 != -1 && bounce > 0)
		{
			CollisionInfo _collisionInfo = _contacts[_index1];
			Vector3 _direction = _collisionInfo.normal.normalized;
			float _directionalSpeed = Vector3.Dot(_velocity, _direction);

			if (_directionalSpeed < bounce)
			{
				_velocity -= _direction * _directionalSpeed;
				_velocity += _direction * bounce;
			}
		}

		// Friction resolution
		if (_index1 != -1)
		{
			CollisionInfo _collisionInfo = _contacts[_index1];

			// --- RELATIVE contact velocity (NO platform velocity here) ---
			Vector3 contactVel =
				_velocity + Vector3.Cross(_omega, -_collisionInfo.normal * marbleRadius);

			// --- Tangential motion ONLY ---
			Vector3 tangent = Vector3.ProjectOnPlane(contactVel, _collisionInfo.normal);
			float tangentSpeed = tangent.magnitude;

			if (tangentSpeed > 0.0001f)
			{
				Vector3 dir = tangent / tangentSpeed;

				float _kinetic = kineticFriction * _collisionInfo.friction;
				float _forceMag = _num1 * _kinetic;

				Vector3 force = -_forceMag * dir;
				_linAccel += force;

				float _torqueMag = (5.0f * _kinetic * _num1) / (2.0f * marbleRadius);
				Vector3 torque =
					_torqueMag * Vector3.Cross(-_collisionInfo.normal, -dir);

				_angAccel += torque;
			}

			// Static friction clamp
			Vector3 _gravVec = -_vector31 * marbleRadius;
			Vector3 _gravTorque = Vector3.Cross(_gravVec, _linAccel) / _gravVec.sqrMagnitude;

			if (_isCentered)
			{
				Vector3 _omegaNext = _omega + _angAccel * _dt;
				_aControl = _desiredOmega - _omegaNext;
				float _mag = _aControl.magnitude;
				if (_mag > brakingAcceleration)
					_aControl *= brakingAcceleration / _mag;
			}

			Vector3 _controlForce = -Vector3.Cross(_aControl, -_collisionInfo.normal * marbleRadius);
			Vector3 _totalForce = Vector3.Cross(_gravTorque, -_collisionInfo.normal * marbleRadius) + _controlForce;

			float staticLimit = staticFriction * _collisionInfo.friction * _num1;
			if (_totalForce.magnitude > staticLimit)
			{
				float _kinetic = kineticFriction * _collisionInfo.friction;
				_controlForce *= _kinetic * _num1 / _totalForce.magnitude;
			}

			_linAccel += _controlForce;
			_angAccel += _gravTorque;
		}

		_angAccel += _aControl;
	}


	static class CollisionHelpers
	{
		public static bool ClosestPtPointTriangle(
			Vector3 pt,
			float radius,
			Vector3 p0,
			Vector3 p1,
			Vector3 p2,
			Vector3 normal,
			out Vector3 closest)
		{
			closest = Vector3.zero;
			float num1 = Vector3.Dot(pt, normal);
			float num2 = Vector3.Dot(p0, normal);
			if (Mathf.Abs(num1 - num2) > radius * 1.1)
				return false;
			closest = pt + (num2 - num1) * normal;
			if (PointInTriangle(closest, p0, p1, p2))
				return true;
			float num3 = 10f;
			if (IntersectSegmentCapsule(pt, pt, p0, p1, radius, out var tSeg, out var tCap) &&
				tSeg < num3)
			{
				closest = p0 + tCap * (p1 - p0);
				num3 = tSeg;
			}

			if (IntersectSegmentCapsule(pt, pt, p1, p2, radius, out tSeg, out tCap) &&
				tSeg < num3)
			{
				closest = p1 + tCap * (p2 - p1);
				num3 = tSeg;
			}

			if (IntersectSegmentCapsule(pt, pt, p2, p0, radius, out tSeg, out tCap) &&
				tSeg < num3)
			{
				closest = p2 + tCap * (p0 - p2);
				num3 = tSeg;
			}

			return num3 < 1.0;
		}

		public static bool PointInTriangle(Vector3 pnt, Vector3 a, Vector3 b, Vector3 c)
		{
			a -= pnt;
			b -= pnt;
			c -= pnt;
			Vector3 bc = Vector3.Cross(b, c);
			Vector3 ca = Vector3.Cross(c, a);
			if (Vector3.Dot(bc, ca) < 0.0)
				return false;
			Vector3 ab = Vector3.Cross(a, b);
			return Vector3.Dot(bc, ab) >= 0.0;
		}

		public static bool IntersectSegmentCapsule(
			Vector3 segStart,
			Vector3 segEnd,
			Vector3 capStart,
			Vector3 capEnd,
			float radius,
			out float seg,
			out float cap)
		{
			return ClosestPtSegmentSegment(segStart, segEnd, capStart, capEnd, out seg, out cap,
				out Vector3 _, out Vector3 _) < radius * radius;
		}

		public static float ClosestPtSegmentSegment(
			Vector3 p1,
			Vector3 q1,
			Vector3 p2,
			Vector3 q2,
			out float s,
			out float T,
			out Vector3 c1,
			out Vector3 c2)
		{
			float num1 = 0.0001f;
			Vector3 vector31 = q1 - p1;
			Vector3 vector32 = q2 - p2;
			Vector3 vector33 = p1 - p2;
			float num2 = Vector3.Dot(vector31, vector31);
			float num3 = Vector3.Dot(vector32, vector32);
			float num4 = Vector3.Dot(vector32, vector33);
			if (num2 <= num1 && num3 <= num1)
			{
				s = T = 0.0f;
				c1 = p1;
				c2 = p2;
				return Vector3.Dot(c1 - c2, c1 - c2);
			}

			if (num2 <= num1)
			{
				s = 0.0f;
				T = num4 / num3;
				T = Mathf.Clamp(T, 0.0f, 1f);
			}
			else
			{
				float num5 = Vector3.Dot(vector31, vector33);
				if (num3 <= num1)
				{
					T = 0.0f;
					s = Mathf.Clamp(-num5 / num2, 0.0f, 1f);
				}
				else
				{
					float num6 = Vector3.Dot(vector31, vector32);
					float num7 = (float)(num2 * num3 - num6 * num6);
					s = num7 == 0.0
						? 0.0f
						: Mathf.Clamp(
							(float)(num6 * num4 - num5 * num3) / num7, 0.0f, 1f);
					T = (num6 * s + num4) / num3;
					if (T < 0.0)
					{
						T = 0.0f;
						s = Mathf.Clamp(-num5 / num2, 0.0f, 1f);
					}
					else if (T > 1.0)
					{
						T = 1f;
						s = Mathf.Clamp((num6 - num5) / num2, 0.0f, 1f);
					}
				}
			}

			c1 = p1 + vector31 * s;
			c2 = p2 + vector32 * T;
			return Vector3.Dot(c1 - c2, c1 - c2);
		}
	}
}
