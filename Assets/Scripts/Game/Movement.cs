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

[System.Serializable]
public class CollisionInfo
{
	public Vector3 point;
	public Vector3 normal;
	public Vector3 velocity;
	public Collider collider;
	public float friction;
	public float restitution;
	public float bounce;
	public float contactDistance;
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Movement : MonoBehaviour
{
	//singleton
	public static Movement instance;
	public void Awake() => instance = this;

	//constraint
	public bool canSpin = true;
	public bool canMove = true;
	public bool canJump = true;
	public bool freezeMovement = false;
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
	[Space]
	public Vector3 marbleVelocity;
	public Vector3 marbleAngularVelocity;

	private float marbleRadius;
	private Vector2 inputMovement()
	{
		Vector2 movement = fakeInput;
		if (canSpin)
		{
			if (Input.GetKey(ControlBinding.instance.moveForward)) movement.y = 1f;
			if (Input.GetKey(ControlBinding.instance.moveBackward)) movement.y = -1f;
			if (Input.GetKey(ControlBinding.instance.moveRight)) movement.x = 1f;
			if (Input.GetKey(ControlBinding.instance.moveLeft)) movement.x = -1f;
		}
		return movement;
	}

	private Vector2 fakeInput = Vector2.zero;

	private bool Jump => Input.GetKey(ControlBinding.instance.jump);

	private Vector3 forwards = Vector3.forward;

	private bool bounceYet;
	private float bounceSpeed;
	private Vector3 bouncePos;
	private Vector3 bounceNormal;
	private float slipAmount;
	private float contactTime;
	private float rollVolume;

	private Vector3 surfaceVelocity;

	private List<MeshCollider> colTests;
	private List<CollisionInfo> contacts = new List<CollisionInfo>();

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
	private Vector3 lastNormal = Vector3.zero;

	Vector3 position;
	Vector3 oldPos;
	Vector3 newPos;
	Quaternion prevRot;

	private bool wasCanMove = true;
	private Vector2 lockedXZ;
	private float baseStaticFriction;
	private float baseKineticFriction;
	private CollisionInfo bestContact;

	private bool hasPosition = false;

	public void SetPosition(Vector3 newPos)
	{
		hasPosition = true;

		marbleVelocity = Vector3.zero;
		marbleAngularVelocity = Vector3.zero;

		position = newPos;
		oldPos = newPos;

		transform.position = newPos;
	}

	public void StopAllMovement()
	{
		marbleVelocity = Vector3.zero;
		marbleAngularVelocity = Vector3.zero;
	}

	public void StopMoving()
	{
		canMove = false;
		canSpin = false;
		canJump = false;
	}

	public void StopAllbutJumping()
	{
		canMove = false;
		canSpin = true;
		canJump = true;
	}

	public void StartMoving()
	{
		canMove = true;
		canSpin = true;
		canJump = true;
	}

	void Start()
	{
		baseStaticFriction = staticFriction;
		baseKineticFriction = kineticFriction;

		rigidBody = gameObject.GetComponent<Rigidbody>();
		rigidBody.maxAngularVelocity = Mathf.Infinity;

		sphereCollider = GetComponent<SphereCollider>();

		GravitySystem.GravityStrength = gravity;

		marbleRadius = sphereCollider.radius * Mathf.Max(
			transform.lossyScale.x,
			transform.lossyScale.y,
			transform.lossyScale.z
		);
	}

	public void GenerateMeshData()
	{
		colTests = new List<MeshCollider>();
		meshes = new List<MeshData>();

		foreach (var item in FindObjectsOfType<MeshCollider>())
		{
			if (!item.isTrigger)
				colTests.Add(item);
		}

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

	void FixedUpdate()
	{
		if (!hasPosition)
			return;

		// Detect canMove turning OFF
		if ((wasCanMove && !canMove) || !GameManager.gameStart)
		{
			lockedXZ = new Vector2(position.x, position.z);

			// Disable friction
			staticFriction = 0f;
			kineticFriction = 0f;
		}

		// Detect canMove turning ON
		if (!wasCanMove && canMove)
		{
			// Restore friction
			staticFriction = baseStaticFriction;
			kineticFriction = baseKineticFriction;
		}

		wasCanMove = canMove;

		float timeRemaining = Time.fixedDeltaTime;

		const float STEP_SIZE = 0.008f;

		oldPos = position;
		prevRot = transform.rotation;

		var it = 0;

		do
		{
			if (timeRemaining <= 0)
				break;

			var timeStep = STEP_SIZE;
			if (timeRemaining < timeStep)
				timeStep = timeRemaining;

			AdvancePhysics(ref timeStep);

			timeRemaining -= timeStep;

			it++;

			if (timeStep == 0 || it > 4)
				break;
		} while (true);

		if (!freezeMovement)
			transform.position = position;

		Vector3 vector3 = marbleAngularVelocity;
		float num1 = vector3.magnitude;
		if (num1 <= 0.0000001f)
			return;

		Quaternion quaternion = Quaternion.AngleAxis(Time.fixedDeltaTime * num1 * Mathf.Rad2Deg, vector3 * (1f / num1));
		quaternion.Normalize();
		transform.rotation = quaternion * transform.rotation;
		transform.rotation.Normalize();
	}

	List<CollisionInfo> FindContacts(Bounds bounds)
	{
		contacts.Clear();
		float _radius = marbleRadius + 0.0001f;

		for (int _index = 0; _index < colTests.Count; _index++)
		{
			MeshData _mesh = meshes[_index];
			MeshCollider _meshCollider = _mesh.collider;

			if (_mesh.mesh == null || !_meshCollider.enabled)
				continue;

			if (!_meshCollider.bounds.Intersects(bounds))
				continue;

			UpdateMeshTransform(_mesh);

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

				var closest = Vector3.zero;
				var contactNormal = Vector3.zero;
				var res = CollisionHelpers.TriangleSphereIntersection(_p0, _p1, _p2, _normal, position, _radius, out closest, out contactNormal);

				if (res)
				{
					var contactDist = (closest - position).sqrMagnitude;
					if (contactDist <= _radius * _radius)
					{
						if (Vector3.Dot(position - closest, _normal) > 0)
						{
							Vector3 colliderVelocity = Vector3.zero;

							if (_meshCollider.attachedRigidbody != null)
							{
								colliderVelocity = _meshCollider.attachedRigidbody.GetPointVelocity(closest);
							}
							else if (_mesh != null)
							{
								colliderVelocity =
									(_mesh.collider.transform.position - _mesh.lastPosition)
									/ Time.fixedDeltaTime;
							}

							CollisionInfo newCollision = new CollisionInfo
							{
								point = closest,
								normal = contactNormal.normalized,
								collider = _meshCollider,
								contactDistance = Mathf.Sqrt(contactDist),
								restitution = _meshCollider.gameObject.GetComponent<FrictionComponent>()?.restitution ?? 1.0f,
								friction = _meshCollider.gameObject.GetComponent<FrictionComponent>()?.friction ?? 1.0f,
								bounce = _meshCollider.gameObject.GetComponent<FrictionComponent>()?.bounce ?? 0.0f,
								velocity = colliderVelocity
							};

							contacts.Add(newCollision);
							lastNormal = newCollision.normal;

							if (contacts.Count >= 4)
								break;
						}
					}
				}
			}
		}

		return contacts;
	}

	private void AdvancePhysics(ref float _dt)
	{
		var searchBox = sphereCollider.bounds;
		searchBox.Expand(0.1f);

		contacts = FindContacts(searchBox);

		UpdateMove(ref _dt);
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

	void UpdateMove(ref float _dt)
	{
		// Compute player input forces
		bool isMoving = ComputeMoveForces(marbleAngularVelocity, out var aControl, out var desiredOmega);

		// First pass: cancel velocity with bounce enabled
		bool stoppedPaths = false;

		VelocityCancel(!isMoving, false, ref stoppedPaths);

		// External forces (gravity, air control)
		Vector3 A = GetExternalForces(_dt);

		// Apply contact forces (friction, jump, bounce)
		ApplyContactForces(
			_dt,
			!isMoving,
			aControl,
			desiredOmega,
			ref A,
			out Vector3 a);

		// Integrate forces
		if (canMove)
		{
			marbleVelocity += A * _dt;
			marbleAngularVelocity += a * _dt;
		}
		else
		{
			// Allow vertical physics only
			marbleVelocity.y += A.y * _dt;
			marbleAngularVelocity += a * _dt;

			// Clamp stored horizontal velocity
			Vector3 horiz = new Vector3(marbleVelocity.x, 0f, marbleVelocity.z);
			float maxStored = 1.5f; // tweak this

			if (horiz.magnitude > maxStored)
			{
				horiz = horiz.normalized * maxStored;
				marbleVelocity.x = horiz.x;
				marbleVelocity.z = horiz.z;
			}
		}

		// Second pass: cancel velocity with bounce disabled
		VelocityCancel(!isMoving, true, ref stoppedPaths);

		Vector3 moveVel = marbleVelocity;

		if (!canMove)
		{
			moveVel.x = 0f;
			moveVel.z = 0f;
		}

		var testDt = _dt;
		TestMove(ref position, moveVel, ref testDt);

		if (testDt != _dt)
		{
			var diff = _dt - testDt;
			marbleVelocity -= A * diff;
			marbleAngularVelocity -= a * diff;
			_dt = testDt;
		}

		var expectedPos = position;

		var newPos = NudgeToContacts(marbleVelocity, expectedPos);

		if (!canMove)
		{
			newPos.x = lockedXZ.x;
			newPos.z = lockedXZ.y;
		}

		if (marbleVelocity.sqrMagnitude > 1e-8f)
		{
			var posDiff = newPos - expectedPos;
			if (posDiff.sqrMagnitude > 1e-8)
			{
				var velDiffProj = marbleVelocity * Vector3.Dot(posDiff, marbleVelocity) / (marbleVelocity.sqrMagnitude);
				var expectedProjPos = expectedPos + velDiffProj;
				var updatedTimestep = (expectedProjPos - position).magnitude / marbleVelocity.magnitude;

				var tDiff = updatedTimestep - _dt;
				if (tDiff > 0)
				{
					marbleVelocity -= A * tDiff;
					marbleAngularVelocity -= a * tDiff;
					_dt = updatedTimestep;
				}
			}
		}

		if (!canMove)
		{
			Vector3 lockedPos = new Vector3(
				lockedXZ.x,
				position.y,   // allow vertical motion
				lockedXZ.y
			);

			position = lockedPos;
		}

		position = newPos;

		float contactPct = contacts.Count > 0 ? 1f : 0f;
		UpdateRollSound(contactPct, slipAmount);
	}

	Vector3 NudgeToContacts(Vector3 velocity, Vector3 position)
	{
		var it = 0;
		var prevResolved = 0;
		do
		{
			var resolved = 0;
			foreach (var contact in contacts)
			{
				// Check if we are on wrong side of the triangle
				if (Vector3.Dot(contact.normal, position) - Vector3.Dot(contact.normal, contact.point) < 0 || contact.velocity.sqrMagnitude > 0.00001f)
				{
					continue;
				}

				var planeD = -Vector3.Dot(contact.normal, contact.point);

				var t = Vector3.Dot(contact.point - position, contact.normal) / contact.normal.sqrMagnitude;
				var intersect = position + t * contact.normal;

				var planeDistance = (intersect - position).magnitude;
				if (marbleRadius - 0.005f - planeDistance > 0.0001f)
				{
					position += contact.normal * (marbleRadius - 0.005f - planeDistance);
					resolved += 1;
				}
			}
			if (resolved == 0 && prevResolved == 0)
				break;
			prevResolved = resolved;
			it++;
		} while (it < 4);
		return position;
	}

	void TestMove(ref Vector3 position, Vector3 velocity, ref float dt)
	{
		if (velocity.magnitude > 0.001)
		{
			if (Physics.SphereCast(position, marbleRadius, velocity.normalized, out var _hit, velocity.magnitude * dt + marbleRadius))
			{
				float _travelTime = _hit.distance / velocity.magnitude;
				dt = Mathf.Max(Mathf.Min(dt, _travelTime), 0.00001f);
			}
		}
		position += velocity * dt;
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
		Vector2 _move = inputMovement();
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

	private Vector3 GetExternalForces(float _dt)
	{
		Vector3 _force = GravitySystem.Gravity.normalized * gravity;
		if (contacts.Count == 0)
		{
			GetMarbleAxis(out var _sideDir, out var _motionDir, out Vector3 _);
			_force += (_sideDir * inputMovement().x + _motionDir * inputMovement().y) * airAcceleration;
		}

		return _force;
	}

	bool VelocityCancel(
		bool _surfaceSlide,
		bool _noBounce,
		ref bool stoppedPaths)
	{
		var SurfaceDotThreshold = 0.0001;
		var looped = false;
		var itersIn = 0;
		var done = false;
		do
		{
			done = true;
			itersIn++;
			for (var i = 0; i < contacts.Count; i++)
			{
				var sVel = marbleVelocity - contacts[i].velocity;
				var surfaceDot = Vector3.Dot(contacts[i].normal, sVel);

				if ((!looped && surfaceDot < 0.0) || surfaceDot < -SurfaceDotThreshold)
				{
					var velLen = marbleVelocity.magnitude;
					var surfaceVel = contacts[i].normal * surfaceDot;

					if (_noBounce)
					{
						marbleVelocity -= surfaceVel;
					}
					else
					{
						if (contacts[i].velocity.magnitude < 0.0001f && !_surfaceSlide && surfaceDot > -maxDotSlide * velLen)
						{
							marbleVelocity -= surfaceVel;
							marbleVelocity.Normalize();
							marbleVelocity *= velLen;
							_surfaceSlide = true;
						}
						else if (surfaceDot >= -minBounceVel)
						{
							marbleVelocity -= surfaceVel;
						}
						else
						{
							var restitution = bounceRestitution;

							if (GameManager.instance.superBounceIsActive)
								restitution = 0.9f;
							if (GameManager.instance.shockAbsorberIsActive)
								restitution = 0.01f;

							restitution *= contacts[i].restitution;

							// impact velocity = velocity INTO surface
							float impactVelocity = -surfaceDot;

							// MBG volume curve
							float volume = Mathf.Pow(impactVelocity / 12f, 1.5f);
							volume = Mathf.Clamp01(volume);

							if (impactVelocity > 1f)
								Marble.instance.PlayBounceSound(volume);

							var velocityAdd = surfaceVel * -(1 + restitution);
							var vAtC = sVel + Vector3.Cross(marbleAngularVelocity, contacts[i].normal * -marbleRadius);
							var normalVel = -Vector3.Dot(contacts[i].normal, sVel);

							Marble.instance.BounceEmitter(sVel.magnitude * restitution, contacts[i]);

							vAtC -= contacts[i].normal * Vector3.Dot(contacts[i].normal, sVel);

							var vAtCMag = vAtC.magnitude;

							if (vAtCMag > 0.00001)
							{
								var friction = bounceKineticFriction * contacts[i].friction;

								var angVMagnitude = 5 * friction * normalVel / (2 * marbleRadius);
								if (vAtCMag / marbleRadius < angVMagnitude)
									angVMagnitude = vAtCMag / marbleRadius;

								var vAtCDir = vAtC * (1 / vAtCMag);

								var deltaOmega = Vector3.Cross(contacts[i].normal, vAtCDir) * angVMagnitude;
								marbleAngularVelocity += deltaOmega;

								marbleVelocity -= Vector3.Cross(deltaOmega, contacts[i].normal * marbleRadius);
							}
							marbleVelocity += velocityAdd;

							slipAmount = Mathf.Clamp(vAtCMag / maxRollVelocity, 0f, 1.5f);
						}
					}

					done = false;
				}
			}
			looped = true;
			if (itersIn > 6 && !stoppedPaths)
			{
				stoppedPaths = true;
				if (_noBounce)
					done = true;

				foreach (var contact in contacts)
					contact.velocity = Vector3.zero;
			}
		} while (!done && itersIn < 8); // Maximum limit pls
		if (marbleVelocity.magnitude < 625.0)
		{
			var gotOne = false;
			var dir = Vector3.zero;
			for (var j = 0; j < contacts.Count; j++)
			{
				var dir2 = dir + contacts[j].normal;
				if (dir2.sqrMagnitude < 0.01)
				{
					dir2 = dir2 + contacts[j].normal;
				}
				dir = dir2;
				dir.Normalize();
				gotOne = true;
			}
			if (gotOne)
			{
				dir.Normalize();
				var soFar = 0.0;
				for (var k = 0; k < contacts.Count; k++)
				{
					var dist = marbleRadius - contacts[k].contactDistance;
					var timeToSeparate = 0.1;
					var vel = marbleVelocity - contacts[k].velocity;
					var outVel = Vector3.Dot(vel + dir * (float)soFar, contacts[k].normal);
					if (dist > timeToSeparate * outVel)
					{
						soFar += (dist - outVel * timeToSeparate) / timeToSeparate / Vector3.Dot(contacts[k].normal, dir);
					}
				}
				if (soFar < -25.0)
					soFar = -25.0;
				if (soFar > 25.0)
					soFar = 25.0;
				marbleVelocity += dir * (float)soFar;
			}
		}

		return stoppedPaths;
	}

	void ApplyContactForces(
		float _dt,
		bool _isCentered,
		Vector3 _aControl,
		Vector3 _desiredOmega,
		ref Vector3 A,
		out Vector3 a)
	{
		a = Vector3.zero;
		Vector3 gWorkGravityDir = GravitySystem.Gravity.normalized;
		int bestSurface = -1;
		float bestNormalForce = 0f;
		for (int i = 0; i < contacts.Count; i++)
		{
			float normalForce = -Vector3.Dot(contacts[i].normal, A);
			if (normalForce > bestNormalForce)
			{
				bestNormalForce = normalForce;
				bestSurface = i;
			}
		}

		bestContact = (bestSurface != -1) ? contacts[bestSurface] : null;

		if (bestSurface == -1)
		{
			slipAmount = 0f;
		}

		//bouncy floor
		if (contacts.Count > 0 && contacts[0].bounce > 0)
		{
			Vector3 n = contacts[0].normal.normalized;

			// component of velocity along the normal
			float normalComponent = Vector3.Dot(marbleVelocity, n);

			// remove it
			marbleVelocity -= normalComponent * n;
			marbleVelocity += n * contacts[0].bounce;
			return;
		}

		bool _canJump = bestSurface != -1;
		if (_canJump && Jump && canJump)
		{
			float upDot = Vector3.Dot(bestContact.normal.normalized, -GravitySystem.GravityDir.normalized);

			// Wall or ceiling → no jump
			if (upDot >= 0.1f)
			{
				Vector3 velDifference = marbleVelocity - bestContact.velocity;
				float sv = Vector3.Dot(bestContact.normal, velDifference);

				if (sv < 0f)
					sv = 0f;

				if (sv < jumpImpulse)
				{
					marbleVelocity += bestContact.normal * (jumpImpulse - sv);
					GameManager.instance.PlayJumpAudio();
				}
			}
		}
		for (int j = 0; j < contacts.Count; j++)
		{
			float normalForce2 = -Vector3.Dot(contacts[j].normal, A);
			if (normalForce2 > 0f && Vector3.Dot(contacts[j].normal, marbleVelocity - contacts[j].velocity) <= 0.0001f)
			{
				A += contacts[j].normal * normalForce2;
			}
		}
		if (bestSurface != -1)
		{
			Vector3 vAtC = marbleVelocity + (Vector3.Cross(marbleAngularVelocity, -bestContact.normal * marbleRadius)) - bestContact.velocity;

			float rawSlip = vAtC.magnitude / maxRollVelocity;
			// Accumulate peak slip (important!)
			slipAmount = Mathf.Max(slipAmount, rawSlip);

			float vAtCMag = vAtC.magnitude;
			bool slipping = false;
			Vector3 aFriction = Vector3.zero;
			Vector3 AFriction = Vector3.zero;
			if (vAtCMag != 0f)
			{
				slipping = true;
				float friction = 0.0f;
				// if (this.mode != MarbleMode.Start)
				friction = kineticFriction * bestContact.friction;
				float angAMagnitude = 5 * friction * bestNormalForce / (2 * marbleRadius); // https://math.stackexchange.com/questions/565333/moment-of-inertia-of-a-n-dimensional-sphere
				float AMagnitude = bestNormalForce * friction;
				float totalDeltaV = (angAMagnitude * marbleRadius + AMagnitude) * _dt;
				if (totalDeltaV > vAtCMag)
				{
					float fraction = vAtCMag / totalDeltaV;
					angAMagnitude *= fraction;
					AMagnitude *= fraction;
					slipping = false;
				}
				Vector3 vAtCDir = vAtC / vAtCMag;

				aFriction = Vector3.Cross(-bestContact.normal, -vAtCDir) * angAMagnitude;
				AFriction = -AMagnitude * vAtCDir;
			}
			if (!slipping)
			{
				Vector3 R = -gWorkGravityDir * marbleRadius;
				Vector3 aadd = Vector3.Cross(R, A) / R.sqrMagnitude;
				if (_isCentered)
				{
					Vector3 nextOmega = marbleAngularVelocity + a * _dt;
					_aControl = _desiredOmega - nextOmega;
					float aScalar = _aControl.magnitude;
					if (aScalar > brakingAcceleration)
					{
						_aControl *= brakingAcceleration / aScalar;
					}
				}
				Vector3 Aadd = -Vector3.Cross(_aControl, (-bestContact.normal * marbleRadius));

				float aAtCMag = (Vector3.Cross(aadd, (-bestContact.normal * marbleRadius)) + Aadd).magnitude;
				var friction2 = 0.0f;
				// if (mode != MarbleMode.Start)
				friction2 = staticFriction * bestContact.friction;
				if (aAtCMag > friction2 * bestNormalForce)
				{
					friction2 = 0.0f;
					// if (mode != MarbleMode.Start)
					friction2 = this.kineticFriction * bestContact.friction;
					Aadd *= friction2 * bestNormalForce / aAtCMag;
				}
				A += Aadd;
				a += aadd;
			}
			A += AFriction;
			a += aFriction;
		}
		a += _aControl;

		slipAmount = Mathf.MoveTowards(slipAmount, 0f, _dt * 2.5f);
	}

	public void ApplySurfaceBoost(float strength = 24.7f)
	{
		// Camera-relative movement direction projected onto gravity plane
		Vector3 up = -GravitySystem.Gravity.normalized;

		Vector3 camForward = Camera.main.transform.forward;
		Vector3 movementVector = Vector3.ProjectOnPlane(camForward, up);

		// Fallback if camera is looking straight along gravity
		if (movementVector.sqrMagnitude < 1e-6f)
			movementVector = Vector3.ProjectOnPlane(Camera.main.transform.forward, up);

		movementVector.Normalize();

		// Remove component pushing into the surface (MBG behavior)
		Vector3 n = lastNormal.normalized;
		float dot = Vector3.Dot(movementVector, n);
		movementVector -= n * dot;

		if (movementVector.sqrMagnitude > 1e-6f)
			movementVector.Normalize();

		marbleVelocity += movementVector * strength;
	}

	void UpdateRollSound(float contactPct, float _slipAmount)
	{
		Vector3 pos = transform.position;

		AudioSource rollSound = Marble.instance.rollingSound;
		AudioSource slipSound = Marble.instance.slidingSound;

		if (contacts.Count == 0 || GameManager.gameFinish)
		{
			rollSound.volume = 0f;
			slipSound.volume = 0f;
			return;
		}

		// Relative rolling velocity
		Vector3 rollVel = bestContact != null
			? marbleVelocity - bestContact.velocity
			: marbleVelocity;

		float scale = rollVel.magnitude / maxRollVelocity;

		// Roll volume
		float rollVolume = 2f * scale;
		if (rollVolume > 1f)
			rollVolume = 1f;

		if (contactPct < 0.05f && rollSound != null)
			rollVolume = rollSound.volume / 5f;

		// Slip volume
		float slipVolume = 0f;
		if (_slipAmount > 1e-4f)
		{
			slipVolume = _slipAmount / 2.5f;
			if (slipVolume > 1f)
				slipVolume = 1f;

			rollVolume = 0f;
		}

		rollVolume = Mathf.Max(0f, rollVolume);
		slipVolume = Mathf.Max(0f, slipVolume);

		if (rollSound != null)
			rollSound.volume = rollVolume;

		if (slipSound != null)
			slipSound.volume = slipVolume;

		// Pitch calculation
		float pitch =
			Mathf.Clamp(rollVel.magnitude / 15f, 0f, 1f) * 0.75f + 0.75f;

		// JS safeguard equivalent (Unity is fine, but we keep it safe)
		if (pitch < 0.2f)
			pitch = 0.2f;

		if (rollSound != null)
			rollSound.pitch = pitch;

		rollSound.volume = rollSound.volume * PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);
		slipSound.volume = slipSound.volume * PlayerPrefs.GetFloat("Audio_SoundVolume", 0.5f);
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

		public static void ClosestPtPointTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c, out Vector3 outP)
		{
			// Check if P in vertex region outside A
			var ab = b - a;
			var ac = c - a;
			var ap = p - a;
			var d1 = Vector3.Dot(ab, ap);
			var d2 = Vector3.Dot(ac, ap);
			if (d1 <= 0.0 && d2 <= 0.0)
			{ // barycentric coordinates (1,0,0)
				outP = a;
				return;
			}
			// Check if P in vertex region outside B
			var bp = p - b;
			var d3 = Vector3.Dot(ab, bp);
			var d4 = Vector3.Dot(ac, bp);
			if (d3 >= 0.0 && d4 <= d3)
			{ // barycentric coordinates (0,1,0
				outP = b;
				return;
			}

			// Check if P in edge region of AB, if so return projection of P onto AB
			var vc = d1 * d4 - d3 * d2;
			if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
			{
				var v2 = d1 / (d1 - d3);
				outP = a + ab * v2;
				return;
			}

			// Check if P in vertex region outside C
			var cp = p - c;
			var d5 = Vector3.Dot(ab, cp);
			var d6 = Vector3.Dot(ac, cp);
			if (d6 >= 0.0 && d5 <= d6)
			{ // barycentric coordinates (0,0,1)
				outP = c;
				return;
			}

			// Check if P in edge region of AC, if so return projection of P onto AC
			var vb = d5 * d2 - d1 * d6;
			if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
			{
				var w2 = d2 / (d2 - d6);
				outP = a + ac * w2;
				return;
			}

			// Check if P in edge region of BC, if so return projection of P onto BC
			var va = d3 * d6 - d5 * d4;
			if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
			{
				var w3 = (d4 - d3) / ((d4 - d3) + (d5 - d6));
				outP = b + (c - b) * w3;
				return;
			}
			// P inside face region. Compute Q through its barycentric coordinates (u,v,w)

			var denom = 1.0f / (va + vb + vc);
			var v = vb * denom;
			var w = vc * denom;
			outP = a + ab * v + ac * w;
			return;
		}

		public static bool TriangleSphereIntersection(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 N, Vector3 P, float r, out Vector3 point, out Vector3 normal)
		{
			ClosestPtPointTriangle(P, v0, v1, v2, out point);
			var v = point - P;
			if (v.sqrMagnitude <= r * r)
			{
				normal = P - point;
				normal.Normalize();
				return true;
			}
			else
			{
				normal = Vector3.zero;
				return false;
			}
		}
	}
}
