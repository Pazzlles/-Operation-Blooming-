
using UnityEngine;

public class LiquidWobble : MonoBehaviour
{
	[Header("Liquid")]
	[SerializeField] private Renderer liquidRenderer;

	[Header("Wobble Settings")]
	[SerializeField] private float strength = 0.015f;
	[SerializeField] private float spring = 30f;
	[SerializeField] private float damping = 7f;
	[SerializeField] private float maxWobble = 0.4f;

	private Vector3 previousPosition;
	private Vector3 previousVelocity;

	private Vector2 wobble;
	private Vector2 wobbleVelocity;

	private MaterialPropertyBlock propertyBlock;

	private static readonly int WobbleX =
		Shader.PropertyToID("_WobbleX");

	private static readonly int WobbleZ =
		Shader.PropertyToID("_WobbleZ");

	private void Awake()
	{
		propertyBlock = new MaterialPropertyBlock();
		previousPosition = transform.position;
	}

	private void OnEnable()
	{
		previousPosition = transform.position;
		previousVelocity = Vector3.zero;
		wobble = Vector2.zero;
		wobbleVelocity = Vector2.zero;
	}

	private void LateUpdate()
	{
		if (liquidRenderer == null)
			return;

		float dt = Time.deltaTime;

		if (dt <= 0f)
			return;

		Vector3 position = transform.position;

		Vector3 velocity =
			(position - previousPosition) / dt;
		Vector3 acceleration =
			(velocity - previousVelocity) / dt;

		previousPosition = position;
		previousVelocity = velocity;

		Vector2 target = new Vector2(
			acceleration.x,
			acceleration.z
		) * strength;

		target = Vector2.ClampMagnitude(
			target, maxWobble
		);

		Vector2 force = (target - wobble) * spring;

		wobbleVelocity += force * dt;
		wobbleVelocity *= Mathf.Exp(-damping * dt);

		wobble += wobbleVelocity * dt;

		wobble = Vector2.ClampMagnitude(
			wobble, maxWobble
		);

		liquidRenderer.GetPropertyBlock(propertyBlock);

		propertyBlock.SetFloat(WobbleX, wobble.x);
		propertyBlock.SetFloat(WobbleZ, wobble.y);

		liquidRenderer.SetPropertyBlock(propertyBlock);
	}
}
